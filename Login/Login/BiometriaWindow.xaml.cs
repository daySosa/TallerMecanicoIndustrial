using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace InterfazClientes
{
    public partial class VentanaBiometria : Window
    {
        private readonly RepositorioSql _db = new();
        private DataTable _usuariosCache = new();
        private string _emailSeleccionado = null;
        private byte[]? _rostroCapturado = null;

        // ── CÁMARA ───────────────────────────────────────────────────
        private FilterInfoCollection? _camarasDisponibles;
        private VideoCaptureDevice? _camaraActiva;
        private CascadeClassifier? _detectorRostros;
        private bool _rostroDetectadoEnVivo = false;

        public VentanaBiometria()
        {
            InitializeComponent();
            CargarDetector();
            CargarUsuarios();
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── DETECTOR DE ROSTROS ──────────────────────────────────────

        private void CargarDetector()
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "haarcascade_frontalface_default.xml");

            if (File.Exists(ruta))
            {
                _detectorRostros = new CascadeClassifier(ruta);
            }
            else
            {
                MessageBox.Show("No se encontró el archivo del detector de rostros (haarcascade_frontalface_default.xml). " +
                    "Verifica que esté en la carpeta de salida del proyecto y que su propiedad 'Copiar en el directorio de salida' esté activada.",
                    "Detector no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // ── USUARIOS ─────────────────────────────────────────────────

        private void CargarUsuarios()
        {
            try
            {
                _usuariosCache = _db.ObtenerUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message);
            }
        }

        private void txtBuscarUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscarUsuario.Text.Trim().Replace("'", "''");

            if (_usuariosCache == null) return;

            _usuariosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : $"Usuario_Nombre LIKE '%{texto}%' OR Usuario_Apellido LIKE '%{texto}%' " +
                  $"OR Usuario_Email LIKE '%{texto}%'";

            if (_usuariosCache.DefaultView.Count == 1)
                SeleccionarUsuario(_usuariosCache.DefaultView[0].Row);
            else
                LimpiarSeleccion();
        }

        private void SeleccionarUsuario(DataRow row)
        {
            _emailSeleccionado = row["Usuario_Email"].ToString();
            string nombre = $"{row["Usuario_Nombre"]} {row["Usuario_Apellido"]}";
            txtUsuarioSeleccionado.Text = nombre;
            txtUsuarioSeleccionado.Foreground = Pincel("#FFFFFF");
            ActualizarBotonesCaptura();
        }

        private void LimpiarSeleccion()
        {
            _emailSeleccionado = null;
            txtUsuarioSeleccionado.Text = "Sin usuario seleccionado";
            txtUsuarioSeleccionado.Foreground = Pincel("#353a58");
            ActualizarBotonesCaptura();
        }

        // ── CÁMARA ───────────────────────────────────────────────────

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                MessageBox.Show("Selecciona un usuario válido de la lista antes de activar la cámara.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                if (_camarasDisponibles.Count == 0)
                {
                    MessageBox.Show("No se detectó ninguna cámara conectada.",
                        "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _camaraActiva = new VideoCaptureDevice(_camarasDisponibles[0].MonikerString);
                _camaraActiva.NewFrame += CamaraActiva_NewFrame;
                _camaraActiva.Start();

                panelSinCamara.Visibility = Visibility.Collapsed;
                btnCapturar.IsEnabled = true;
                btnIniciarCamara.IsEnabled = false;

                txtEstadoDeteccion.Text = "Cámara iniciada — buscando rostro...";
                txtEstadoDeteccion.Foreground = Pincel("#8890b5");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar cámara: " + ex.Message);
            }
        }

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs args)
        {
            using Drawing.Bitmap frame = (Drawing.Bitmap)args.Frame.Clone();

            if (_detectorRostros != null)
            {
                using var imgEmgu = frame.ToImage<Bgr, byte>();
                using var imgGris = imgEmgu.Convert<Gray, byte>();

                CvInvoke.EqualizeHist(imgGris, imgGris);

                // ROI central: solo buscamos rostros en el área donde normalmente
                // se posiciona la persona, ignorando bordes/fondo
                int roiWidth = (int)(frame.Width * 0.6);
                int roiHeight = (int)(frame.Height * 0.75);
                int roiX = (frame.Width - roiWidth) / 2;
                int roiY = (frame.Height - roiHeight) / 2;
                var roiRect = new Drawing.Rectangle(roiX, roiY, roiWidth, roiHeight);

                imgGris.ROI = roiRect;

                var rostrosEnRoi = _detectorRostros.DetectMultiScale(
                    imgGris,
                    scaleFactor: 1.1,
                    minNeighbors: 10,
                    minSize: new Drawing.Size(100, 100),
                    maxSize: new Drawing.Size(350, 350));

                imgGris.ROI = Drawing.Rectangle.Empty; // resetear ROI

                // Convertimos las coordenadas de vuelta al frame completo (sumamos el offset del ROI)
                var rostros = rostrosEnRoi
                    .Select(r => new Drawing.Rectangle(r.X + roiX, r.Y + roiY, r.Width, r.Height))
                    .ToArray();

                // Nos quedamos solo con el rostro más grande (más cercano/confiable)
                Drawing.Rectangle[] rostroPrincipal = rostros.Length > 0
                    ? new[] { rostros.OrderByDescending(r => r.Width * r.Height).First() }
                    : Array.Empty<Drawing.Rectangle>();

                _rostroDetectadoEnVivo = rostroPrincipal.Length > 0;

                using Drawing.Graphics g = Drawing.Graphics.FromImage(frame);
                foreach (var rostro in rostroPrincipal)
                    g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 2), rostro);
            }

            var src = BitmapToImageSource(frame);
            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = src;

                if (_rostroDetectadoEnVivo)
                {
                    txtEstadoDeteccion.Text = "Rostro detectado — listo para capturar";
                    txtEstadoDeteccion.Foreground = Pincel("#4CAF50");
                }
                else
                {
                    txtEstadoDeteccion.Text = "No se detecta rostro";
                    txtEstadoDeteccion.Foreground = Pincel("#E74C3C");
                }
            });
        }

        private void btnCapturar_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                MessageBox.Show("Selecciona un usuario válido de la lista antes de capturar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_rostroDetectadoEnVivo)
            {
                MessageBox.Show("No se detecta ningún rostro. Ubícate frente a la cámara.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Convierte el frame actual que se ve en pantalla a bytes (JPEG) para guardar
                var bitmapSource = (BitmapSource)imgCamara.Source;
                using MemoryStream ms = new();
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(ms);
                _rostroCapturado = ms.ToArray();

                txtEstadoRegistro.Text = "✔ Rostro capturado";
                txtEstadoDeteccion.Text = "Rostro capturado correctamente";
                txtEstadoDeteccion.Foreground = Pincel("#4CAF50");
                iconDeteccion.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
                iconDeteccion.Foreground = Pincel("#4CAF50");

                ActualizarBotonesCaptura();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al capturar: " + ex.Message);
            }
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

        // ── GUARDAR / ELIMINAR ───────────────────────────────────────

        private void btnGuardarBiometria_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                MessageBox.Show("Debes seleccionar un usuario ya registrado en el sistema.",
                    "Usuario no seleccionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_rostroCapturado == null || _rostroCapturado.Length == 0)
            {
                MessageBox.Show("Captura el rostro antes de guardar.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _db.GuardarBiometria(_emailSeleccionado, _rostroCapturado);
                MessageBox.Show("✅ Biometría guardada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DetenerCamara();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        private void btnEliminarBiometria_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                MessageBox.Show("Debes seleccionar un usuario ya registrado en el sistema.",
                    "Usuario no seleccionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                "¿Estás seguro de eliminar el registro biométrico de este usuario?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _db.EliminarBiometria(_emailSeleccionado);
                MessageBox.Show("✅ Registro biométrico eliminado.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarSeleccion();
                _rostroCapturado = null;
                ActualizarBotonesCaptura();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message);
            }
        }

        // ── CERRAR ───────────────────────────────────────────────────

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamara();
            base.OnClosed(e);
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private void ActualizarBotonesCaptura()
        {
            bool hayUsuario = _emailSeleccionado != null;
            bool hayCaptura = _rostroCapturado != null && _rostroCapturado.Length > 0;

            btnGuardarBiometria.IsEnabled = hayUsuario && hayCaptura;
            btnEliminarBiometria.IsEnabled = hayUsuario;
        }

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}