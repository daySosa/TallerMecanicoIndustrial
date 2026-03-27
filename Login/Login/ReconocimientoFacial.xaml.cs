using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Login
{
    /// <summary>
    /// Ventana encargada del reconocimiento facial.
    /// Permite capturar video desde la cámara, detectar rostros,
    /// realizar reconocimiento facial, registrar nuevas personas
    /// y gestionar el acceso al sistema.
    /// </summary>
    public partial class ReconocimientoFacial : Window
    {
        /// <summary>
        /// Colección de cámaras disponibles en el sistema.
        /// </summary>
        private FilterInfoCollection? _camarasDisponibles;

        /// <summary>
        /// Cámara actualmente activa.
        /// </summary>
        private VideoCaptureDevice? _camaraActiva;

        /// <summary>
        /// Clasificador Haar utilizado para la detección de rostros.
        /// </summary>
        private CascadeClassifier? _detectorRostros;

        /// <summary>
        /// Servicio encargado del entrenamiento y reconocimiento facial.
        /// </summary>
        private readonly clsServicioReconocimiento _servicioReconocimiento = new();

        /// <summary>
        /// Indica si se ha detectado un rostro en el frame actual.
        /// </summary>
        private bool _rostroDetectado = false;

        /// <summary>
        /// Indica si el acceso ha sido concedido al usuario.
        /// </summary>
        private bool _accesoOtorgado = false;

        /// <summary>
        /// Indica si la aplicación está en modo registro de personas.
        /// </summary>
        private bool _esModoRegistro = false;

        /// <summary>
        /// Lista de personas registradas en el sistema.
        /// Contiene Id, Nombre y la fotografía asociada.
        /// </summary>
        private readonly List<(int Id, string Nombre, Drawing.Bitmap Foto)> _personas = new();

        /// <summary>
        /// Fotografía capturada para registro de una nueva persona.
        /// </summary>
        private Drawing.Bitmap? _fotoCapturada = null;

        /// <summary>
        /// Correo electrónico del usuario en sesión.
        /// </summary>
        private readonly string _correoUsuario;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="ReconocimientoFacial"/>.
        /// </summary>
        /// <param name="correo">Correo del usuario en sesión.</param>
        public ReconocimientoFacial(string correo = "")
        {
            InitializeComponent();
            _correoUsuario = correo;

            txtNombrePersona.TextChanged += (_, _) =>
                txtContadorChars.Text = $"{txtNombrePersona.Text.Length} / 100";

            CargarDetector();
            CargarPersonasDesdeBD();
        }

        /// <summary>
        /// Carga el modelo de detección de rostros desde un archivo XML.
        /// </summary>
        private void CargarDetector()
        {
            const string ruta = "haarcascade_frontalface_default.xml";
            if (File.Exists(ruta))
                _detectorRostros = new CascadeClassifier(ruta);
            else
                MostrarEstado("⚠ No se encontró el XML de detección de rostros.", esError: true);
        }

        /// <summary>
        /// Carga las personas registradas desde la base de datos y entrena el modelo.
        /// </summary>
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

        /// <summary>
        /// Guarda una nueva persona en la base de datos.
        /// </summary>
        /// <param name="nombre">Nombre de la persona.</param>
        /// <param name="foto">Fotografía asociada.</param>
        private void GuardarPersonaEnBD(string nombre, Drawing.Bitmap foto)
        {
            try
            {
                using Drawing.Bitmap copia = new(foto);
                using MemoryStream ms = new();
                copia.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bytes = ms.ToArray();

                clsConexion conexion = new();
                conexion.Abrir();
                const string query = "INSERT INTO ReconocimientoFacial (Nombre, Foto) VALUES (@nombre, @foto)";
                SqlCommand cmd = new(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@foto", bytes);
                cmd.ExecuteNonQuery();
                conexion.Cerrar();

                MessageBox.Show($"Persona '{nombre}' registrada correctamente.",
                    "Registro exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón para iniciar la cámara.
        /// </summary>
        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var (ok, msg) = clsValidacionReconocimiento.ValidarCamaraDisponible(_camarasDisponibles.Count);
            if (!ok)
            {
                MessageBox.Show(msg, "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _accesoOtorgado = false;
            _esModoRegistro = false;

            _camaraActiva = new VideoCaptureDevice(_camarasDisponibles[0].MonikerString);
            _camaraActiva.NewFrame += CamaraActiva_NewFrame;
            _camaraActiva.Start();

            pnlSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            btnCapturarFoto.IsEnabled = true;
            btnModoRegistro.IsEnabled = true;
            btnModoLogin.IsEnabled = false;

            MostrarEstado("Cámara iniciada — selecciona un modo.");
        }

        /// <summary>
        /// Maneja el evento Click del botón para detener la cámara.
        /// </summary>
        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            btnIniciarCamara.IsEnabled = true;
            btnDetenerCamara.IsEnabled = false;
            btnCapturarFoto.IsEnabled = false;
            btnModoRegistro.IsEnabled = false;
            btnModoLogin.IsEnabled = false;
            pnlSinCamara.Visibility = Visibility.Visible;
            MostrarEstado("Cámara detenida.");
        }

        /// <summary>
        /// Detiene la cámara activa y libera recursos.
        /// </summary>
        private void DetenerCamara()
        {
            if (_camaraActiva is { IsRunning: true })
            {
                _camaraActiva.SignalToStop();
                _camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                _camaraActiva = null;
            }
        }

        /// <summary>
        /// Maneja cada frame capturado por la cámara.
        /// Realiza detección de rostros y reconocimiento facial.
        /// </summary>
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
                var color = _esModoRegistro ? Drawing.Color.Orange : Drawing.Color.LimeGreen;
                g.DrawRectangle(new Drawing.Pen(color, 2), rostro);

                if (!_esModoRegistro && _servicioReconocimiento.Entrenado)
                {
                    using var rostroGris = imgGris.Copy(rostro);
                    var (label, distance) = _servicioReconocimiento.Predecir(rostroGris);
                    bool valido = clsValidacionReconocimiento
                        .EsReconocimientoValido(label, distance, _personas.Count);

                    if (valido)
                    {
                        string nombre = _personas[label].Nombre;
                        Dispatcher.Invoke(() =>
                            txtNombreReconocido.Text =
                                $"✔ Identificado: {nombre}  (dist: {distance:F1})");
                        FinalizarAcceso(nombre);
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                            txtNombreReconocido.Text =
                                $"Desconocido  (dist: {distance:F1})");
                    }
                }
            }

            if (rostros.Length == 0)
                Dispatcher.Invoke(() => txtNombreReconocido.Text = "");

            ActualizarVisor(frame);
        }

        /// <summary>
        /// Actualiza la imagen mostrada en la interfaz con el frame actual.
        /// </summary>
        private void ActualizarVisor(Drawing.Bitmap frame)
        {
            var src = BitmapToImageSource(frame);
            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = src;
                if (!_esModoRegistro)
                {
                    MostrarEstado(_rostroDetectado
                        ? "Modo Login — Rostro detectado, analizando..."
                        : "Modo Login — Buscando rostro...");

                    elipseEstado.Fill = _rostroDetectado
                        ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                        : new SolidColorBrush(Color.FromRgb(100, 100, 100));
                }
            });
        }

        // ── Acceso concedido ──────────────────────────────────────────────────────

        /// <summary>
        /// Finaliza el proceso de autenticación y concede acceso al sistema.
        /// </summary>
        private async void FinalizarAcceso(string nombre)
        {
            if (_accesoOtorgado) return;
            _accesoOtorgado = true;

            Dispatcher.Invoke(() =>
            {
                txtNombreReconocido.Text = $"✔ ¡Bienvenido, {nombre}!";
                MostrarEstado("Acceso concedido. Entrando en 5 segundos...");
                elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            });

            await Task.Delay(5000);

            Dispatcher.Invoke(() =>
            {
                DetenerCamara();
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            });
        }

        /// <summary>
        /// Activa el modo de registro de nuevas personas.
        /// </summary>
        private void btnModoRegistro_Click(object sender, RoutedEventArgs e)
        {
            _esModoRegistro = true;
            txtNombreReconocido.Text = "";
            btnModoRegistro.IsEnabled = false;
            btnModoLogin.IsEnabled = true;
            MostrarEstado("Modo Registro activo — el reconocimiento está desactivado.");
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(230, 126, 34));
        }

        /// <summary>
        /// Activa el modo de inicio de sesión mediante reconocimiento facial.
        /// </summary>
        private void btnModoLogin_Click(object sender, RoutedEventArgs e)
        {
            _esModoRegistro = false;
            _accesoOtorgado = false;
            _fotoCapturada = null;
            bdFotoCapturada.Visibility = Visibility.Collapsed;
            txtNombreReconocido.Text = "";
            btnModoLogin.IsEnabled = false;
            btnModoRegistro.IsEnabled = true;
            MostrarEstado("Modo Login activo — coloca tu cara frente a la cámara.");
        }

        /// <summary>
        /// Captura una fotografía del rostro detectado para su registro.
        /// </summary>
        private void btnCapturarFoto_Click(object sender, RoutedEventArgs e)
        {
            var (modoOk, msgModo) =
                clsValidacionReconocimiento.ValidarModoRegistroActivo(_esModoRegistro);
            if (!modoOk)
            {
                MessageBox.Show(msgModo, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (rostroOk, msgRostro) =
                clsValidacionReconocimiento.ValidarRostroDetectado(_rostroDetectado);
            if (!rostroOk)
            {
                MessageBox.Show(msgRostro, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Drawing.Bitmap? frame = ObtenerFrameActual();
            if (frame == null)
            {
                MessageBox.Show("No se pudo obtener el fotograma actual.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var imgEmgu = frame.ToImage<Bgr, byte>();
            using var imgGris = imgEmgu.Convert<Gray, byte>();
            var rostros = _detectorRostros!.DetectMultiScale(
                imgGris, 1.1, 6, new Drawing.Size(90, 90));

            if (rostros.Length > 0)
            {
                _fotoCapturada?.Dispose();
                using var rostroGris = imgGris.Copy(rostros[0]);
                _fotoCapturada = clsServicioReconocimiento
                    .PrepararRostro(rostroGris.ToBitmap()).ToBitmap();

                bdFotoCapturada.Visibility = Visibility.Visible;
                MostrarEstado("Rostro capturado. Ingresa el nombre y pulsa Registrar.");
            }
            else
            {
                MessageBox.Show("No se detectó un rostro claro. Intenta de nuevo.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            frame.Dispose();
        }

        /// <summary>
        /// Registra una nueva persona en la base de datos.
        /// </summary>
        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombrePersona.Text.Trim();

            var (nombreOk, msgNombre) = clsValidacionReconocimiento.ValidarNombre(nombre);
            if (!nombreOk)
            {
                MessageBox.Show(msgNombre, "Nombre inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (fotoOk, msgFoto) = clsValidacionReconocimiento.ValidarFotoCapturada(_fotoCapturada);
            if (!fotoOk)
            {
                MessageBox.Show(msgFoto, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GuardarPersonaEnBD(nombre, _fotoCapturada!);
            CargarPersonasDesdeBD();

            txtNombrePersona.Text = "";
            bdFotoCapturada.Visibility = Visibility.Collapsed;
            _fotoCapturada?.Dispose();
            _fotoCapturada = null;
        }

        /// <summary>
        /// Regresa a la ventana de opciones de sesión.
        /// </summary>
        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            new OpcionSesion(_correoUsuario).Show();
            this.Close();
        }

        /// <summary>
        /// Muestra un mensaje de estado en la interfaz.
        /// </summary>
        private void MostrarEstado(string mensaje, bool esError = false)
        {
            txtEstado.Text = mensaje;
            txtEstado.Foreground = esError
                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                : new SolidColorBrush(Color.FromRgb(170, 170, 170));
        }

        /// <summary>
        /// Obtiene el frame actual mostrado en la interfaz como Bitmap.
        /// </summary>
        private Drawing.Bitmap? ObtenerFrameActual()
        {
            if (imgCamara.Source is not BitmapSource src) return null;

            var bmp = new Drawing.Bitmap(
                src.PixelWidth,
                src.PixelHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            var data = bmp.LockBits(
                new Drawing.Rectangle(Drawing.Point.Empty, bmp.Size),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            src.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        /// <summary>
        /// Convierte un Bitmap de System.Drawing a BitmapImage para WPF.
        /// </summary>
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

        /// <summary>
        /// Libera recursos cuando la ventana se cierra.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            DetenerCamara();
            _servicioReconocimiento.Dispose();
            foreach (var p in _personas) p.Foto?.Dispose();
            _fotoCapturada?.Dispose();
            base.OnClosed(e);
        }
    }
}

