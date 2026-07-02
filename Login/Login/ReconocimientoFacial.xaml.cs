using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Login
{
    public partial class ReconocimientoFacial : Window
    {
        private FilterInfoCollection? _camarasDisponibles;
        private VideoCaptureDevice? _camaraActiva;
        private CascadeClassifier? _detectorRostros;

        private readonly clsServicioReconocimiento _servicioReconocimiento = new();

        private bool _rostroDetectado = false;
        private bool _accesoOtorgado = false;

        private readonly List<(int Id, string Nombre, Drawing.Bitmap Foto)> _personas = new();

        private readonly string _correoUsuario;

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public ReconocimientoFacial(string correo = "")
        {
            InitializeComponent();
            _correoUsuario = correo;
            CargarDetector();
            CargarPersonasDesdeBD();
        }

        // ════════════════════════════════════════════════════════════
        // DRAG
        // ════════════════════════════════════════════════════════════

        private void Window_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        // ════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ════════════════════════════════════════════════════════════

        private void CargarDetector()
        {
            const string ruta = "haarcascade_frontalface_default.xml";
            if (File.Exists(ruta))
                _detectorRostros = new CascadeClassifier(ruta);
            else
                MostrarEstado("⚠ No se encontró el XML de detección.", esError: true);
        }

        private void CargarPersonasDesdeBD()
        {
            try
            {
                foreach (var p in _personas) p.Foto?.Dispose();
                _personas.Clear();

                clsConexion conexion = new();
                conexion.Abrir();

                const string query = "SELECT Id, Nombre, Foto FROM ReconocimientoFacial";
                using SqlDataReader reader = new SqlCommand(query, conexion.SqlC).ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    byte[] bytes = (byte[])reader[2];
                    using MemoryStream ms = new(bytes);
                    Drawing.Bitmap foto = new(new Drawing.Bitmap(ms));
                    _personas.Add((id, nombre, foto));
                }

                conexion.Cerrar();

                if (_personas.Count > 0)
                    _servicioReconocimiento.Entrenar(_personas);

                MostrarEstado($"{_personas.Count} persona(s) cargada(s). Sistema listo.");
            }
            catch (Exception ex)
            {
                MostrarEstado("Error al cargar datos: " + ex.Message, esError: true);
            }
        }

        // ════════════════════════════════════════════════════════════
        // CÁMARA
        // ════════════════════════════════════════════════════════════

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var (ok, msg) = clsValidacionReconocimiento
                .ValidarCamaraDisponible(_camarasDisponibles.Count);

            if (!ok)
            {
                MessageBox.Show(msg, "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _accesoOtorgado = false;

            _camaraActiva = new VideoCaptureDevice(_camarasDisponibles[0].MonikerString);
            _camaraActiva.NewFrame += CamaraActiva_NewFrame;
            _camaraActiva.Start();

            pnlSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;

            MostrarEstado("Cámara iniciada — buscando rostro...");
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            btnIniciarCamara.IsEnabled = true;
            btnDetenerCamara.IsEnabled = false;
            pnlSinCamara.Visibility = Visibility.Visible;
            MostrarEstado("Cámara detenida.");
        }

        private void DetenerCamara()
        {
            if (_camaraActiva is { IsRunning: true })
            {
                _camaraActiva.SignalToStop();
                _camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                _camaraActiva = null;
            }
        }

        // ════════════════════════════════════════════════════════════
        // PROCESAMIENTO DE FRAMES
        // ════════════════════════════════════════════════════════════

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs args)
        {
            if (_accesoOtorgado) return;

            using Drawing.Bitmap frame = (Drawing.Bitmap)args.Frame.Clone();

            if (_detectorRostros == null)
            {
                ActualizarVisor(frame);
                return;
            }

            using var imgEmgu = frame.ToImage<Bgr, byte>();
            using var imgGris = imgEmgu.Convert<Gray, byte>();

            var rostros = _detectorRostros.DetectMultiScale(
                imgGris, scaleFactor: 1.1, minNeighbors: 6,
                minSize: new Drawing.Size(90, 90));

            _rostroDetectado = rostros.Length > 0;

            using Drawing.Graphics g = Drawing.Graphics.FromImage(frame);

            foreach (var rostro in rostros)
            {
                g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 2), rostro);

                if (_servicioReconocimiento.Entrenado)
                {
                    using var rostroGris = imgGris.Copy(rostro);
                    var (label, distance) = _servicioReconocimiento.Predecir(rostroGris);
                    bool valido = clsValidacionReconocimiento
                        .EsReconocimientoValido(label, distance, _personas.Count);

                    if (valido)
                        FinalizarAcceso(_personas[label].Nombre);
                    else
                        Dispatcher.Invoke(() => txtNombreReconocido.Text =
                            $"Desconocido  (dist: {distance:F1})");
                }
            }

            if (rostros.Length == 0)
                Dispatcher.Invoke(() => txtNombreReconocido.Text = "");

            ActualizarVisor(frame);
        }

        private void ActualizarVisor(Drawing.Bitmap frame)
        {
            var src = BitmapToImageSource(frame);
            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = src;

                MostrarEstado(_rostroDetectado
                    ? "Rostro detectado, analizando..."
                    : "Buscando rostro...");

                elipseEstado.Fill = _rostroDetectado
                    ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                    : new SolidColorBrush(Color.FromRgb(100, 100, 100));
            });
        }

        // ════════════════════════════════════════════════════════════
        // ACCESO CONCEDIDO
        // ════════════════════════════════════════════════════════════

        private async void FinalizarAcceso(string nombre)
        {
            if (_accesoOtorgado) return;
            _accesoOtorgado = true;

            Dispatcher.Invoke(() =>
            {
                bdNombreReconocido.Visibility = Visibility.Visible;
                txtNombreReconocido.Text = $"¡Bienvenido, {nombre}!";
                MostrarEstado("Acceso concedido. Entrando en 3 segundos...");
                elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            });

            // ── NUEVO: cargar sesión con el rol del usuario logueado ──
            try
            {
                var db = new clsConsultasBD();
                var datosUsuario = db.ObtenerUsuarioPorEmail(_correoUsuario);
                if (datosUsuario != null)
                {
                    SesionActual.IniciarSesion(
                        datosUsuario["Usuario_Email"].ToString(),
                        datosUsuario["Usuario_Nombre"].ToString(),
                        datosUsuario["Usuario_Apellido"].ToString(),
                        datosUsuario["Usuario_Rol"].ToString()
                    );
                }
            }
            catch { /* si falla, sesión queda vacía y los permisos bloquean todo por defecto */ }

            await Task.Delay(3000);

            Dispatcher.Invoke(() =>
            {
                DetenerCamara();
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            });
        }
        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ════════════════════════════════════════════════════════════

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            new OpcionSesion(_correoUsuario).Show();
            this.Close();
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════

        private void MostrarEstado(string mensaje, bool esError = false)
        {
            txtEstado.Text = mensaje;
            txtEstado.Foreground = esError
                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                : new SolidColorBrush(Color.FromRgb(170, 170, 170));
        }

        private static BitmapImage BitmapToImageSource(Drawing.Bitmap bitmap)
        {
            using MemoryStream ms = new();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Position = 0;
            BitmapImage img = new();
            img.BeginInit();
            img.StreamSource = ms;
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            img.Freeze();
            return img;
        }

        // ════════════════════════════════════════════════════════════
        // CLEANUP
        // ════════════════════════════════════════════════════════════

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamara();
            _servicioReconocimiento.Dispose();
            foreach (var p in _personas) p.Foto?.Dispose();
            base.OnClosed(e);
        }
    }
}