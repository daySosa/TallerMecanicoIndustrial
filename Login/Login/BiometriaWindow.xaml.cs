#nullable enable
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana para registrar la biometría facial de un usuario: activa la cámara,
    /// detecta rostros en vivo con un clasificador Haar Cascade (Emgu CV), permite
    /// capturar varias fotos con distintas poses y las guarda asociadas al usuario
    /// seleccionado. Incluye una transición de fade-in al abrirse y fade-out al cerrarse.
    /// </summary>
    public partial class VentanaBiometria : Window
    {
        #region Constantes

        /// <summary>Cantidad mínima de fotos requeridas para poder guardar el registro.</summary>
        private const int MIN_FOTOS = 8;

        /// <summary>Cantidad máxima de fotos permitidas por sesión de captura.</summary>
        private const int MAX_FOTOS = 12;

        /// <summary>Duración de las transiciones de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        private const string RutaDetector = "haarcascade_frontalface_default.xml";

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private DataTable _usuariosCache = new();
        private string? _emailSeleccionado;

        /// <summary>Fotos capturadas en la sesión actual, aún sin persistir.</summary>
        private readonly List<byte[]> _fotosCapturadas = new();

        private FilterInfoCollection? _camarasDisponibles;
        private VideoCaptureDevice? _camaraActiva;
        private CascadeClassifier? _detectorRostros;
        private volatile bool _rostroDetectadoEnVivo;

        /// <summary>Último frame SIN el rectángulo de detección dibujado, para capturar sin ruido visual.</summary>
        private Drawing.Bitmap? _ultimoFrameLimpio;

        /// <summary>
        /// Indica si la ventana ya fue cerrada (o está cerrándose). El callback de la
        /// cámara corre en un hilo secundario y puede seguir disparándose durante el
        /// fade-out; esta bandera evita que intente tocar controles de UI ya inválidos.
        /// </summary>
        private volatile bool _ventanaCerrada;

        /// <summary>Evita el reingreso a Window_Closing mientras se reproduce el fade-out.</summary>
        private bool _cerrandoConAnimacion;

        #endregion

        public VentanaBiometria()
        {
            InitializeComponent();
            CargarDetector();
            CargarUsuarios();
        }

        #region Ciclo de vida y transición de entrada/salida

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>Aplica un fade-in suave al mostrar la ventana (entra con Opacity="0" desde XAML).</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Intercepta el cierre para detener la cámara de inmediato (evita que siga
        /// entregando frames durante la animación) y reproducir un fade-out antes de
        /// cerrar de verdad. La primera vez cancela el cierre; al completarse la
        /// animación se vuelve a llamar Close() con la bandera activada.
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_cerrandoConAnimacion) return;

            _ventanaCerrada = true;
            DetenerCamara();

            e.Cancel = true;
            _cerrandoConAnimacion = true;

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        protected override void OnClosed(EventArgs e)
        {
            _ventanaCerrada = true;
            DetenerCamara();
            base.OnClosed(e);
        }

        #endregion

        #region Detector de rostros

        /// <summary>Carga el clasificador Haar Cascade usado para detectar rostros en cada frame.</summary>
        private void CargarDetector()
        {
            string ruta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RutaDetector);

            if (File.Exists(ruta))
            {
                _detectorRostros = new CascadeClassifier(ruta);
            }
            else
            {
                MessageBox.Show(
                    $"No se encontró el archivo del detector de rostros ({RutaDetector}). " +
                    "Verifica que esté en la carpeta de salida del proyecto y que su propiedad " +
                    "'Copiar en el directorio de salida' esté activada.",
                    "Detector no encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Usuarios: búsqueda y selección

        private void CargarUsuarios()
        {
            try
            {
                _usuariosCache = _db.ObtenerUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscarUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_usuariosCache == null) return;

            string texto = EscaparParaFiltro(txtBuscarUsuario.Text.Trim());

            _usuariosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : $"Usuario_Nombre LIKE '%{texto}%' OR Usuario_Apellido LIKE '%{texto}%' " +
                  $"OR Usuario_Email LIKE '%{texto}%'";

            if (_usuariosCache.DefaultView.Count == 1)
                SeleccionarUsuario(_usuariosCache.DefaultView[0].Row);
            else
                LimpiarSeleccion();
        }

        /// <summary>Escapa comillas y caracteres especiales de LIKE (% * [ ]) usados por DataView.RowFilter.</summary>
        private static string EscaparParaFiltro(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            return texto
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        private void SeleccionarUsuario(DataRow row)
        {
            _emailSeleccionado = row["Usuario_Email"].ToString();
            string nombre = $"{row["Usuario_Nombre"]} {row["Usuario_Apellido"]}";
            txtUsuarioSeleccionado.Text = nombre;
            txtUsuarioSeleccionado.Foreground = Pincel("#FFFFFF");

            // Al cambiar de usuario se descartan capturas pendientes del usuario anterior.
            _fotosCapturadas.Clear();
            ActualizarContadorCapturas();
            ActualizarBotonesCaptura();
        }

        private void LimpiarSeleccion()
        {
            _emailSeleccionado = null;
            txtUsuarioSeleccionado.Text = "Sin usuario seleccionado";
            txtUsuarioSeleccionado.Foreground = Pincel("#353a58");
            _fotosCapturadas.Clear();
            ActualizarContadorCapturas();
            ActualizarBotonesCaptura();
        }

        #endregion

        #region Cámara

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                Aviso("Selecciona un usuario válido de la lista antes de activar la cámara.");
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
                btnIniciarCamara.IsEnabled = false;
                ActualizarBotonesCaptura();

                txtEstadoDeteccion.Text = "Cámara iniciada — buscando rostro...";
                txtEstadoDeteccion.Foreground = Pincel("#8890b5");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al iniciar cámara: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Se ejecuta en el hilo de captura de AForge por cada frame nuevo. Detecta el
        /// rostro principal con Emgu CV, dibuja un rectángulo indicativo solo en la copia
        /// que se muestra en pantalla, y actualiza el estado de detección en el hilo de UI.
        /// </summary>
        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs args)
        {
            if (_ventanaCerrada) return;

            using Drawing.Bitmap frame = (Drawing.Bitmap)args.Frame.Clone();

            // Guardamos una copia limpia ANTES de dibujar el rectángulo de detección encima,
            // para no quemar esa línea verde sobre los píxeles del rostro al capturar.
            _ultimoFrameLimpio?.Dispose();
            _ultimoFrameLimpio = (Drawing.Bitmap)frame.Clone();

            if (_detectorRostros != null)
                DetectarYMarcarRostro(frame);

            if (_ventanaCerrada) return;

            var src = BitmapToImageSource(frame); // esta copia SÍ lleva el rectángulo, solo para mostrar en pantalla
            Dispatcher.Invoke(() =>
            {
                if (_ventanaCerrada) return;

                imgCamara.Source = src;
                ActualizarEstadoDeteccion(_rostroDetectadoEnVivo);
            });
        }

        /// <summary>Detecta el rostro de mayor tamaño dentro de una región central del frame y lo marca con un rectángulo.</summary>
        private void DetectarYMarcarRostro(Drawing.Bitmap frame)
        {
            using var imgEmgu = frame.ToImage<Bgr, byte>();
            using var imgGris = imgEmgu.Convert<Gray, byte>();

            CvInvoke.EqualizeHist(imgGris, imgGris);

            // ROI central: solo buscamos rostros en el área donde normalmente
            // se posiciona la persona, ignorando bordes/fondo.
            int roiWidth = (int)(frame.Width * 0.6);
            int roiHeight = (int)(frame.Height * 0.75);
            int roiX = (frame.Width - roiWidth) / 2;
            int roiY = (frame.Height - roiHeight) / 2;
            var roiRect = new Drawing.Rectangle(roiX, roiY, roiWidth, roiHeight);

            imgGris.ROI = roiRect;

            var rostrosEnRoi = _detectorRostros!.DetectMultiScale(
                imgGris,
                scaleFactor: 1.1,
                minNeighbors: 10,
                minSize: new Drawing.Size(100, 100),
                maxSize: new Drawing.Size(350, 350));

            imgGris.ROI = Drawing.Rectangle.Empty; // resetear ROI

            var rostros = rostrosEnRoi
                .Select(r => new Drawing.Rectangle(r.X + roiX, r.Y + roiY, r.Width, r.Height))
                .ToArray();

            var rostroPrincipal = rostros.Length > 0
                ? new[] { rostros.OrderByDescending(r => r.Width * r.Height).First() }
                : Array.Empty<Drawing.Rectangle>();

            _rostroDetectadoEnVivo = rostroPrincipal.Length > 0;

            using Drawing.Graphics g = Drawing.Graphics.FromImage(frame);
            foreach (var rostro in rostroPrincipal)
                g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 2), rostro);
        }

        /// <summary>Sincroniza texto, color e ícono del panel de estado con si hay o no rostro detectado.</summary>
        private void ActualizarEstadoDeteccion(bool rostroDetectado)
        {
            if (rostroDetectado)
            {
                txtEstadoDeteccion.Text = "Rostro detectado — listo para capturar";
                txtEstadoDeteccion.Foreground = Pincel("#4CAF50");
                iconDeteccion.Kind = PackIconKind.CheckCircleOutline;
                iconDeteccion.Foreground = Pincel("#4CAF50");
            }
            else
            {
                txtEstadoDeteccion.Text = "No se detecta rostro";
                txtEstadoDeteccion.Foreground = Pincel("#E74C3C");
                iconDeteccion.Kind = PackIconKind.CloseCircleOutline;
                iconDeteccion.Foreground = Pincel("#E74C3C");
            }
        }

        /// <summary>Detiene la captura de cámara (si está activa) y libera el último frame en memoria.</summary>
        private void DetenerCamara()
        {
            if (_camaraActiva is { IsRunning: true })
            {
                _camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                _camaraActiva.SignalToStop();
                _camaraActiva = null;
            }

            _ultimoFrameLimpio?.Dispose();
            _ultimoFrameLimpio = null;
        }

        /// <summary>Convierte un <see cref="Drawing.Bitmap"/> a un <see cref="BitmapImage"/> congelado, apto para enlazar en WPF.</summary>
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

        #endregion

        #region Captura de fotos

        private void btnCapturar_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                Aviso("Selecciona un usuario válido de la lista antes de capturar.");
                return;
            }

            if (!_rostroDetectadoEnVivo)
            {
                Aviso("No se detecta ningún rostro. Ubícate frente a la cámara.");
                return;
            }

            if (_fotosCapturadas.Count >= MAX_FOTOS)
            {
                MessageBox.Show(
                    $"Ya capturaste el máximo de {MAX_FOTOS} fotos. Guarda el registro o descarta las capturas para empezar de nuevo.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_ultimoFrameLimpio == null)
            {
                Aviso("No hay un frame disponible todavía para capturar. Espera un momento e intenta de nuevo.");
                return;
            }

            try
            {
                // Capturamos del frame LIMPIO (sin el rectángulo verde quemado en los píxeles),
                // no del que se muestra en pantalla (imgCamara.Source), que sí lo tiene dibujado.
                using MemoryStream ms = new();
                _ultimoFrameLimpio.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                _fotosCapturadas.Add(ms.ToArray());

                txtEstadoDeteccion.Text = "Rostro capturado correctamente";
                txtEstadoDeteccion.Foreground = Pincel("#4CAF50");
                iconDeteccion.Kind = PackIconKind.CheckCircleOutline;
                iconDeteccion.Foreground = Pincel("#4CAF50");

                ActualizarContadorCapturas();
                ActualizarBotonesCaptura();

                if (_fotosCapturadas.Count < MAX_FOTOS)
                {
                    MessageBox.Show(
                        $"Foto {_fotosCapturadas.Count} de {MAX_FOTOS} capturada.\n\n" +
                        "Cambia ligeramente tu pose (gira un poco la cabeza, sonríe o no) " +
                        "antes de la siguiente captura para mejorar el reconocimiento.",
                        "Captura registrada", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al capturar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDescartarCapturas_Click(object sender, RoutedEventArgs e)
        {
            if (_fotosCapturadas.Count == 0) return;

            var confirm = MessageBox.Show(
                $"¿Descartar las {_fotosCapturadas.Count} foto(s) capturada(s) en esta sesión?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            _fotosCapturadas.Clear();
            ActualizarContadorCapturas();
            ActualizarBotonesCaptura();
        }

        #endregion

        #region Guardar / Eliminar registro

        private void btnGuardarBiometria_Click(object sender, RoutedEventArgs e)
        {
            if (_emailSeleccionado == null)
            {
                MessageBox.Show("Debes seleccionar un usuario ya registrado en el sistema.",
                    "Usuario no seleccionado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_fotosCapturadas.Count < MIN_FOTOS)
            {
                MessageBox.Show(
                    $"Captura al menos {MIN_FOTOS} fotos (llevas {_fotosCapturadas.Count}) antes de guardar, " +
                    "para que el reconocimiento sea más confiable.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _db.EliminarBiometria(_emailSeleccionado);

                foreach (var foto in _fotosCapturadas)
                    _db.GuardarBiometria(_emailSeleccionado, foto);

                MessageBox.Show($"✅ Biometría guardada correctamente ({_fotosCapturadas.Count} fotos).",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                _fotosCapturadas.Clear();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                "¿Estás seguro de eliminar TODAS las fotos biométricas de este usuario?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                _db.EliminarBiometria(_emailSeleccionado);
                MessageBox.Show("✅ Registro biométrico eliminado.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarSeleccion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Helpers

        private void ActualizarContadorCapturas()
        {
            txtEstadoRegistro.Text = _fotosCapturadas.Count == 0
                ? "Sin capturas"
                : $"{_fotosCapturadas.Count} de {MAX_FOTOS} fotos capturadas";
        }

        private void ActualizarBotonesCaptura()
        {
            bool hayUsuario = _emailSeleccionado != null;
            bool camaraActiva = _camaraActiva is { IsRunning: true };
            bool alcanzoMinimo = _fotosCapturadas.Count >= MIN_FOTOS;
            bool alcanzoMaximo = _fotosCapturadas.Count >= MAX_FOTOS;

            btnCapturar.IsEnabled = hayUsuario && camaraActiva && !alcanzoMaximo;
            btnGuardarBiometria.IsEnabled = hayUsuario && alcanzoMinimo;
            btnEliminarBiometria.IsEnabled = hayUsuario;
        }

        private static void Aviso(string mensaje) =>
            MessageBox.Show(mensaje, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        #endregion
    }
}