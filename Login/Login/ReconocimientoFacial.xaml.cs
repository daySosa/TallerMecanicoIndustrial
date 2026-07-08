using Dasboard_Prueba;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Login.Clases;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Login
{
    /// <summary>
    /// Ventana de verificación facial 1:1: compara el rostro capturado en vivo
    /// únicamente contra las fotos registradas de la cuenta que se está autenticando
    /// (no hace identificación abierta contra toda la base de datos).
    /// Incluye prueba de vida (parpadeo + movimiento de cabeza) y bloqueo temporal
    /// tras varios intentos fallidos, respaldado en base de datos.
    /// </summary>
    public partial class ReconocimientoFacial : Window
    {
        #region Configuración general

        private const int MAX_INTENTOS_FALLIDOS = 5;
        private const int MINUTOS_BLOQUEO = 3;
        private const int ANCHO_MINIMO_ROSTRO_PX = 250;

        private const double UMBRAL_CONFIANZA_LBPH = 44.5;

        private const int FRAMES_PRUEBA_VIDA = 90;
        private const int UMBRAL_MOVIMIENTO_PX = 8;

        private const int FRAMES_CONSENSO_REQUERIDOS = 5;
        private const int TOLERANCIA_FRAMES_RUIDOSOS = 1;

        private const int SEGUNDOS_GRACIA = 3;

        /// <summary>Varianza mínima del Laplaciano para considerar un frame "nítido" (no borroso).
        /// Cámaras de baja calidad producen más motion blur/ruido, por lo que frames por debajo
        /// de este umbral se descartan del consenso de verificación.</summary>
        private const double UMBRAL_NITIDEZ_MINIMA = 40;

        /// <summary>Duración de las transiciones de fade al navegar entre ventanas.</summary>
        private static readonly Duration DuracionFade = new(TimeSpan.FromMilliseconds(220));

        private readonly string archivoLog =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log_accesos.txt");

        private readonly RepositorioSql _db = new();

        #endregion

        #region Brushes congelados (recursos de UI reutilizables)

        private static readonly SolidColorBrush BrushEspera = CrearBrushCongelado(0x2d, 0x30, 0x50);
        private static readonly SolidColorBrush BrushAlerta = CrearBrushCongelado(0xf5, 0xa6, 0x23);
        private static readonly SolidColorBrush BrushExito = CrearBrushCongelado(0x2E, 0xCC, 0x71);
        private static readonly SolidColorBrush BrushError = CrearBrushCongelado(0x7b, 0x1f, 0x1f);
        private static readonly SolidColorBrush BrushInfo = CrearBrushCongelado(0x4f, 0x6e, 0xf7);

        /// <summary>
        /// Crea un <see cref="SolidColorBrush"/> ya congelado (Freeze) para evitar
        /// el costo de hacerlo en cada uso y permitir su acceso seguro entre hilos.
        /// </summary>
        private static SolidColorBrush CrearBrushCongelado(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        #endregion

        #region Cámara y clasificadores

        private VideoCapture _camara;
        private DispatcherTimer _timer;
        private DispatcherTimer _timerBloqueo;

        private CascadeClassifier _clasificadorRostro;
        private CascadeClassifier _clasificadorOjos;

        private LBPHFaceRecognizer _reconocedor;
        private readonly Dictionary<int, string> _etiquetasNombres = new();
        private readonly Dictionary<int, string> _etiquetasEmails = new();
        private readonly string _correoEsperado;

        private bool _modeloListo = false;

        #endregion

        #region Estado de bloqueo (respaldado en tabla IntentosAccesoFallidos)

        private DateTime? _bloqueadoHasta = null;

        #endregion

        #region Estado de prueba de vida

        private bool _ojosVisiblesFramePrevio = false;
        private bool _esperandoReaperturaOjos = false;
        private int _parpadeosDetectados = 0;

        private System.Drawing.Point? _centroRostroPrevio = null;
        private double _movimientoAcumulado = 0;

        private int _framesProcesados = 0;
        private bool _pruebaVidaSuperada = false;
        private bool _verificando = false;

        private DateTime? _inicioGracia = null;

        private readonly List<(int Label, double Distance)> _historialPredicciones = new();

        #endregion

        #region Precarga del menú principal (transición fluida tras acceso concedido)

        /// <summary>
        /// Instancia de <see cref="MenuPrincipal"/> precargada oculta (Opacity = 0)
        /// mientras se muestra la insignia de éxito, para que la construcción de la
        /// ventana (constructor, carga inicial de datos) no se note como un freeze
        /// justo al final de la transición.
        /// </summary>
        private MenuPrincipal _menuPrincipalPrecargado;

        #endregion

        #region Constructor

        /// <param name="correoEsperado">
        /// Correo de la cuenta que se está verificando. La comparación es 1:1
        /// contra las fotos de este correo, no una identificación abierta.
        /// </param>
        public ReconocimientoFacial(string correoEsperado)
        {
            InitializeComponent();

            _correoEsperado = string.IsNullOrWhiteSpace(correoEsperado) ? string.Empty : correoEsperado.Trim();

            if (_correoEsperado.Length == 0)
            {
                MessageBox.Show("No se recibió una cuenta válida para verificar.",
                    "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            txtEstado.Text = $"Verificando: {_correoEsperado}";

            InicializarClasificadores();

            btnIniciarCamara.IsEnabled = false;
            txtEstado.Text = "Cargando rostros registrados...";
            _ = CargarYEntrenarAsync();
        }

        #endregion

        #region Transiciones y navegación entre ventanas

        /// <summary>Aplica un fade-in suave cuando la ventana termina de cargar.</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionFade)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Navega hacia la ventana de selección de sesión (<c>OpcionSesion</c>)
        /// con un fade-out fluido, pasándole el correo que se estaba verificando.
        /// </summary>
        private void IrAOpcionSesion()
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionFade)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            fadeOut.Completed += (s, e) =>
            {
                try
                {
                    var opcionSesion = new OpcionSesion(_correoEsperado);
                    opcionSesion.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo abrir la ventana de sesión: " + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Close();
                }
            };

            BeginAnimation(OpacityProperty, fadeOut);
        }

        #endregion

        #region Carga y entrenamiento del modelo LBPH

        private async Task CargarYEntrenarAsync()
        {
            try
            {
                var (imagenes, etiquetas, nombres, emails) = await Task.Run(EntrenarReconocedor);

                await ActualizarEstadoBloqueoAsync();

                if (imagenes.Count == 0)
                {
                    txtEstado.Text = "Sin rostros registrados en la base de datos";
                    return;
                }

                _reconocedor = new LBPHFaceRecognizer(1, 8, 8, 8, double.MaxValue);

                using (var vectorImagenes = new Emgu.CV.Util.VectorOfMat())
                using (var vectorEtiquetas = new Emgu.CV.Util.VectorOfInt(etiquetas.ToArray()))
                {
                    foreach (var img in imagenes)
                        vectorImagenes.Push(img.Mat);

                    await Task.Run(() => _reconocedor.Train(vectorImagenes, vectorEtiquetas));
                }

                foreach (var img in imagenes) img.Dispose();

                _etiquetasNombres.Clear();
                _etiquetasEmails.Clear();
                foreach (var kv in nombres) _etiquetasNombres[kv.Key] = kv.Value;
                foreach (var kv in emails) _etiquetasEmails[kv.Key] = kv.Value;

                _modeloListo = true;

                if (_bloqueadoHasta is null)
                {
                    btnIniciarCamara.IsEnabled = true;
                    ActualizarEstadoUI("En espera...", BrushEspera);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al preparar el reconocimiento facial: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtEstado.Text = "Error al cargar rostros registrados";
            }
        }

        #endregion

        #region Bloqueo por intentos fallidos (respaldado por IntentosAccesoFallidos)

        private async Task ActualizarEstadoBloqueoAsync()
        {
            RepositorioSql.EstadoIntentosFallidos estado;
            try
            {
                estado = await Task.Run(() =>
                    _db.ContarIntentosFallidosRecientes(_correoEsperado, MINUTOS_BLOQUEO));
            }
            catch (Exception ex)
            {
                RegistrarEnLog("ERROR AL CONSULTAR INTENTOS FALLIDOS: " + ex.Message);
                return;
            }

            bool bloqueado = estado.TotalIntentos >= MAX_INTENTOS_FALLIDOS && estado.UltimoIntento.HasValue;

            if (!bloqueado)
            {
                _bloqueadoHasta = null;
                _timerBloqueo?.Stop();
                if (_modeloListo) btnIniciarCamara.IsEnabled = true;
                return;
            }

            _bloqueadoHasta = estado.UltimoIntento!.Value.ToLocalTime().AddMinutes(MINUTOS_BLOQUEO);

            if (_bloqueadoHasta.Value <= DateTime.Now)
            {
                _bloqueadoHasta = null;
                if (_modeloListo) btnIniciarCamara.IsEnabled = true;
                return;
            }

            IniciarContadorBloqueoUI();
        }

        private void IniciarContadorBloqueoUI()
        {
            btnIniciarCamara.IsEnabled = false;
            ActualizarEstadoUI("Bloqueado temporalmente", BrushError);

            _timerBloqueo?.Stop();
            _timerBloqueo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerBloqueo.Tick += (s, e) =>
            {
                if (_bloqueadoHasta is null) { _timerBloqueo.Stop(); return; }

                var restante = _bloqueadoHasta.Value - DateTime.Now;

                if (restante <= TimeSpan.Zero)
                {
                    _timerBloqueo.Stop();
                    _bloqueadoHasta = null;

                    if (_modeloListo) btnIniciarCamara.IsEnabled = true;
                    ActualizarEstadoUI("En espera...", BrushEspera);
                }
                else
                {
                    txtEstado.Text = $"Bloqueado. Intenta de nuevo en {restante.Minutes:D2}:{restante.Seconds:D2}";
                }
            };
            _timerBloqueo.Start();
        }

        private void RegistrarEnLog(string mensaje)
        {
            try
            {
                string linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}{Environment.NewLine}";
                File.AppendAllText(archivoLog, linea);
            }
            catch
            {
            }
        }

        private void ActualizarEstadoUI(string texto, SolidColorBrush color)
        {
            txtEstado.Text = texto;
            elipseEstado.Fill = color;
        }

        #endregion

        #region Inicialización de clasificadores y entrenamiento

        private void InicializarClasificadores()
        {
            string rutaRecursos = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recursos");
            string rutaRostro = Path.Combine(rutaRecursos, "haarcascade_frontalface_default.xml");
            string rutaOjos = Path.Combine(rutaRecursos, "haarcascade_eye.xml");

            if (!File.Exists(rutaRostro) || !File.Exists(rutaOjos))
            {
                MessageBox.Show(
                    "Faltan los archivos de clasificación Haar Cascade en la carpeta 'Recursos'.\n" +
                    "Descárgalos desde el repositorio oficial de OpenCV (opencv/data/haarcascades) " +
                    "y colócalos junto al ejecutable.",
                    "Recursos faltantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _clasificadorRostro = new CascadeClassifier(rutaRostro);
            _clasificadorOjos = new CascadeClassifier(rutaOjos);
        }

        /// <summary>
        /// Normaliza la iluminación del rostro para hacerlo más robusto ante cambios
        /// de luz y ruido de cámaras de baja calidad. Pipeline:
        /// 1) Filtro bilateral: reduce ruido/grano de cámara conservando bordes.
        /// 2) CLAHE: ecualización adaptativa de contraste local (ilumina sombras
        ///    sin "quemar" zonas claras).
        /// </summary>
        private static Image<Gray, byte> NormalizarIluminacion(Image<Gray, byte> imgGris)
        {
            var denoised = new Image<Gray, byte>(imgGris.Size);
            CvInvoke.BilateralFilter(imgGris, denoised, 5, 50, 50);

            var salida = new Image<Gray, byte>(denoised.Size);
            CvInvoke.CLAHE(denoised, 2.0, new System.Drawing.Size(8, 8), 256, salida);

            denoised.Dispose();
            return salida;
        }

        /// <summary>
        /// Calcula la nitidez de una imagen usando la varianza del Laplaciano.
        /// Valores bajos indican una imagen borrosa (motion blur, desenfoque de
        /// autoenfoque, etc.), algo común en cámaras de baja calidad. Los frames
        /// por debajo de <see cref="UMBRAL_NITIDEZ_MINIMA"/> se descartan del
        /// consenso de verificación para no contaminar el promedio de distancia.
        /// </summary>
        private static double CalcularNitidez(Image<Gray, byte> img)
        {
            using var laplaciano = new Mat();
            CvInvoke.Laplacian(img, laplaciano, DepthType.Cv64F);

            using var mean = new Mat();
            using var stdDev = new Mat();
            CvInvoke.MeanStdDev(laplaciano, mean, stdDev);

            double[,] valores = (double[,])stdDev.GetData();
            double desviacion = valores[0, 0];
            return desviacion * desviacion;
        }


        private (List<Image<Gray, byte>> Imagenes, List<int> Etiquetas,
            Dictionary<int, string> Nombres, Dictionary<int, string> Emails) EntrenarReconocedor()
        {
            var imagenes = new List<Image<Gray, byte>>();
            var etiquetas = new List<int>();
            var etiquetasNombres = new Dictionary<int, string>();
            var etiquetasEmails = new Dictionary<int, string>();

            if (_clasificadorRostro == null)
                return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);

            List<RepositorioSql.PersonaReconocimiento> personas;
            try
            {
                personas = _db.ObtenerPersonasReconocimiento();
            }
            catch (Exception ex)
            {
                RegistrarEnLog("ERROR AL CARGAR ROSTROS: " + ex.Message);
                throw;
            }

            var fotosDeLaPersona = personas
                .Where(p => !string.IsNullOrWhiteSpace(p.Email)
                         && p.Email.Equals(_correoEsperado, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (fotosDeLaPersona.Count == 0)
            {
                RegistrarEnLog($"ERROR - No hay fotos registradas para {_correoEsperado}");
                return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);
            }

            string nombre = fotosDeLaPersona[0].Nombre;
            int validas = 0;

            foreach (var persona in fotosDeLaPersona)
            {
                try
                {
                    using var ms = new MemoryStream(persona.Foto);
                    using var bmp = new System.Drawing.Bitmap(ms);
                    using var imgColor = bmp.ToImage<Bgr, byte>();
                    using var imgGris = imgColor.Convert<Gray, byte>();

                    var rostros = _clasificadorRostro.DetectMultiScale(
                        imgGris, 1.1, 6, System.Drawing.Size.Empty);

                    if (rostros.Length == 0) continue;

                    var rostroRect = rostros.OrderByDescending(r => r.Width * r.Height).First();
                    using var rostroRecortado = imgGris.Copy(rostroRect);
                    using var rostroEcualizado = NormalizarIluminacion(rostroRecortado);
                    var rostroNormalizado = rostroEcualizado.Resize(200, 200, Inter.Cubic);

                    imagenes.Add(rostroNormalizado);
                    etiquetas.Add(0);
                    validas++;
                }
                catch
                {
                }
            }

            if (validas > 0)
            {
                etiquetasNombres[0] = nombre;
                etiquetasEmails[0] = _correoEsperado;
            }

            RegistrarEnLog($"ENTRENAMIENTO (1:1) - {_correoEsperado}: {validas}/{fotosDeLaPersona.Count} fotos válidas");

            return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);
        }

        #endregion

        #region Control de cámara

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (_bloqueadoHasta.HasValue && _bloqueadoHasta.Value > DateTime.Now)
            {
                MessageBox.Show("El acceso está temporalmente bloqueado por intentos fallidos.",
                    "Bloqueado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_clasificadorRostro == null || !_modeloListo || _reconocedor == null)
            {
                MessageBox.Show(
                    "No es posible iniciar la verificación: faltan recursos (clasificadores Haar) " +
                    "o no hay rostros registrados para comparar.",
                    "Error de configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _camara = new VideoCapture(0);

                try
                {
                    _camara.Set(CapProp.FrameWidth, 1280);
                    _camara.Set(CapProp.FrameHeight, 720);
                    _camara.Set(CapProp.Autofocus, 0);
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo acceder a la cámara: {ex.Message}",
                    "Error de cámara", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ReiniciarEstadoPruebaVida();
            _inicioGracia = DateTime.Now;

            pnlSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            bdNombreReconocido.Visibility = Visibility.Collapsed;

            ActualizarEstadoUI($"Prepárate, colócate frente a la cámara... {SEGUNDOS_GRACIA}", BrushAlerta);

            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamaraInterno();
            ActualizarEstadoUI("En espera...", BrushEspera);
            bdNombreReconocido.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Detiene el timer y libera la referencia a la cámara.
        /// IMPORTANTE: <see cref="VideoCapture.Dispose"/> puede tardar (el driver
        /// necesita liberar el dispositivo físico) y si se llama en el hilo de UI
        /// congela la ventana durante ese lapso — que es justo el efecto de
        /// "cámara pegada" al aceptar el acceso. Por eso el Dispose real se hace
        /// en un hilo de fondo (fire-and-forget) mientras la UI sigue respondiendo
        /// de inmediato (oculta el feed, habilita botones, navega, etc.).
        /// </summary>
        /// <param name="mostrarPanelSinCamara">
        /// Si es <c>true</c> (valor por defecto), vuelve a mostrar el panel
        /// "Presiona Iniciar cámara..." y limpia el último frame. Se pasa
        /// <c>false</c> desde <see cref="AccesoConcedido"/> para mantener visible
        /// el último frame congelado (con el rostro reconocido) detrás de la
        /// insignia de éxito durante la transición hacia el menú principal.
        /// </param>
        private void DetenerCamaraInterno(bool mostrarPanelSinCamara = true)
        {
            _timer?.Stop();
            _timer = null;

            var camaraALiberar = _camara;
            _camara = null;

            if (camaraALiberar != null)
            {
                Task.Run(() =>
                {
                    try
                    {
                        camaraALiberar.Dispose();
                    }
                    catch (Exception ex)
                    {
                        RegistrarEnLog("ERROR AL LIBERAR LA CÁMARA: " + ex.Message);
                    }
                });
            }

            _verificando = false;
            _inicioGracia = null;
            _historialPredicciones.Clear();

            btnDetenerCamara.IsEnabled = false;

            bool estaBloqueado = _bloqueadoHasta.HasValue && _bloqueadoHasta.Value > DateTime.Now;
            if (!estaBloqueado && _modeloListo)
                btnIniciarCamara.IsEnabled = true;

            if (mostrarPanelSinCamara)
            {
                pnlSinCamara.Visibility = Visibility.Visible;
                imgCamara.Source = null;
            }
        }

        private void ReiniciarEstadoPruebaVida()
        {
            _ojosVisiblesFramePrevio = false;
            _esperandoReaperturaOjos = false;
            _parpadeosDetectados = 0;
            _centroRostroPrevio = null;
            _movimientoAcumulado = 0;
            _framesProcesados = 0;
            _pruebaVidaSuperada = false;
            _verificando = false;
            _inicioGracia = null;
            _historialPredicciones.Clear();
        }

        #endregion

        #region Procesamiento de cada frame

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_camara == null) return;

            using var frame = _camara.QueryFrame();
            if (frame == null) return;

            using var imgColor = frame.ToImage<Bgr, byte>();
            using var imgGris = imgColor.Convert<Gray, byte>();

            if (_inicioGracia.HasValue)
            {
                double restante = SEGUNDOS_GRACIA - (DateTime.Now - _inicioGracia.Value).TotalSeconds;

                if (restante > 0)
                {
                    txtEstado.Text = $"Prepárate, colócate frente a la cámara... {Math.Ceiling(restante):F0}";
                    MostrarFrame(imgColor);
                    return;
                }

                _inicioGracia = null;
                txtEstado.Text = "Analizando prueba de vida (parpadea y mueve la cabeza)...";
            }

            var rostros = _clasificadorRostro.DetectMultiScale(
                imgGris, 1.1, 5, new System.Drawing.Size(90, 90));

            if (rostros.Length == 0)
            {
                ActualizarEstadoUI("No se detecta ningún rostro...", BrushEspera);
                MostrarFrame(imgColor);
                return;
            }

            var rostroRect = rostros.OrderByDescending(r => r.Width * r.Height).First();

            if (rostroRect.Width < ANCHO_MINIMO_ROSTRO_PX)
            {
                imgColor.Draw(rostroRect, new Bgr(System.Drawing.Color.Orange), 2);
                ActualizarEstadoUI("Acércate un poco más a la cámara...", BrushAlerta);
                MostrarFrame(imgColor);
                return;
            }

            imgColor.Draw(rostroRect, new Bgr(System.Drawing.Color.LimeGreen), 2);

            if (!_pruebaVidaSuperada)
            {
                ProcesarPruebaDeVida(imgGris, rostroRect);
                MostrarFrame(imgColor);

                if (_pruebaVidaSuperada)
                {
                    ActualizarEstadoUI("Prueba de vida superada. Verificando identidad...", BrushInfo);
                }
                return;
            }

            if (!_verificando)
            {
                VerificarIdentidad(imgGris, rostroRect);
            }

            MostrarFrame(imgColor);
        }

        #endregion

        #region Prueba de vida: parpadeo + movimiento de cabeza

        private void ProcesarPruebaDeVida(Image<Gray, byte> imgGris, System.Drawing.Rectangle rostroRect)
        {
            _framesProcesados++;

            using var rostroROI = imgGris.Copy(rostroRect);
            var zonaSuperior = new System.Drawing.Rectangle(0, 0, rostroROI.Width, Math.Max(1, rostroROI.Height / 2));
            using var zonaOjos = rostroROI.Copy(zonaSuperior);

            var ojos = _clasificadorOjos.DetectMultiScale(zonaOjos, 1.1, 6, System.Drawing.Size.Empty);
            bool ojosVisiblesAhora = ojos.Length >= 1;

            if (_ojosVisiblesFramePrevio && !ojosVisiblesAhora)
            {
                _esperandoReaperturaOjos = true;
            }
            else if (_esperandoReaperturaOjos && ojosVisiblesAhora)
            {
                _parpadeosDetectados++;
                _esperandoReaperturaOjos = false;
            }
            _ojosVisiblesFramePrevio = ojosVisiblesAhora;

            var centroActual = new System.Drawing.Point(
                rostroRect.X + rostroRect.Width / 2,
                rostroRect.Y + rostroRect.Height / 2);

            if (_centroRostroPrevio.HasValue)
            {
                double dx = centroActual.X - _centroRostroPrevio.Value.X;
                double dy = centroActual.Y - _centroRostroPrevio.Value.Y;
                _movimientoAcumulado += Math.Sqrt(dx * dx + dy * dy);
            }
            _centroRostroPrevio = centroActual;

            bool huboParpadeo = _parpadeosDetectados >= 1;
            bool huboMovimiento = _movimientoAcumulado >= UMBRAL_MOVIMIENTO_PX;

            if (huboParpadeo && huboMovimiento)
            {
                _pruebaVidaSuperada = true;
                return;
            }

            if (_framesProcesados >= FRAMES_PRUEBA_VIDA)
            {
                _framesProcesados = 0;
                _parpadeosDetectados = 0;
                _movimientoAcumulado = 0;
                txtEstado.Text = "Parpadea y mueve levemente la cabeza para continuar...";
            }
        }

        #endregion

        #region Comparación de rostro y decisión de acceso (consenso multi-frame)

        private void VerificarIdentidad(Image<Gray, byte> imgGris, System.Drawing.Rectangle rostroRect)
        {
            using var rostroRecortado = imgGris.Copy(rostroRect);
            using var rostroEcualizado = NormalizarIluminacion(rostroRecortado);
            using var rostroNormalizado = rostroEcualizado.Resize(200, 200, Inter.Cubic);

            double nitidez = CalcularNitidez(rostroNormalizado);
            if (nitidez < UMBRAL_NITIDEZ_MINIMA)
            {
                System.Diagnostics.Debug.WriteLine($"[VERIFICACION] Frame descartado por baja nitidez ({nitidez:F1})");
                return;
            }

            var resultado = _reconocedor.Predict(rostroNormalizado);
            _historialPredicciones.Add((resultado.Label, resultado.Distance));

            System.Diagnostics.Debug.WriteLine(
                $"[VERIFICACION] Frame {_historialPredicciones.Count}/{FRAMES_CONSENSO_REQUERIDOS} " +
                $"| Distancia: {resultado.Distance:F2} | Umbral: {UMBRAL_CONFIANZA_LBPH} | Nitidez: {nitidez:F1}");

            if (_historialPredicciones.Count < FRAMES_CONSENSO_REQUERIDOS)
            {
                txtEstado.Text = $"Confirmando identidad ({_historialPredicciones.Count}/{FRAMES_CONSENSO_REQUERIDOS})...";
                return;
            }

            _verificando = true;

            double distanciaPromedio = _historialPredicciones.Average(p => p.Distance);
            int framesDentroDelUmbral = _historialPredicciones.Count(p => p.Distance <= UMBRAL_CONFIANZA_LBPH);

            bool coincide = framesDentroDelUmbral >= FRAMES_CONSENSO_REQUERIDOS - TOLERANCIA_FRAMES_RUIDOSOS;

            RegistrarEnLog(
                $"VERIFICACION 1:1 - {_correoEsperado} | Distancia promedio: {distanciaPromedio:F2} " +
                $"| Frames dentro del umbral: {framesDentroDelUmbral}/{FRAMES_CONSENSO_REQUERIDOS} | Coincide: {coincide}");

            _historialPredicciones.Clear();

            if (coincide && _etiquetasNombres.TryGetValue(0, out string nombre))
            {
                AccesoConcedido(nombre, _correoEsperado);
            }
            else
            {
                _ = AccesoDenegadoAsync("Rostro no coincide con la cuenta que intentas verificar",
                    0, distanciaPromedio);
            }
        }

        /// <summary>
        /// Maneja el flujo de éxito: inicia la sesión, detiene la cámara sin limpiar el
        /// último frame (para que la transición muestre el rostro reconocido junto a la
        /// insignia de éxito), y precarga <see cref="MenuPrincipal"/> mientras se muestra
        /// dicha insignia, para que la transición hacia el menú sea fluida (crossfade).
        /// </summary>
        private void AccesoConcedido(string nombre, string email)
        {
            RegistrarEnLog($"ACCESO CONCEDIDO - Usuario: {nombre} ({email})");

            DetenerCamaraInterno(mostrarPanelSinCamara: false);

            if (!IniciarSesionCompleta(email))
            {
                ActualizarEstadoUI("En espera...", BrushEspera);
                bdNombreReconocido.Visibility = Visibility.Collapsed;
                return;
            }

            ActualizarEstadoUI("Identidad verificada", BrushExito);
            txtNombreReconocido.Text = nombre;
            bdNombreReconocido.Visibility = Visibility.Visible;

            PrecargarMenuPrincipal();

            var temporizadorTransicion = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            temporizadorTransicion.Tick += (s, e) =>
            {
                temporizadorTransicion.Stop();
                AbrirMenuPrincipalConFade();
            };
            temporizadorTransicion.Start();
        }

        /// <summary>
        /// Crea la ventana del menú principal por adelantado, oculta (Opacity = 0),
        /// marcando que el origen del ingreso fue reconocimiento facial para que
        /// <see cref="MenuPrincipal"/> no aplique su propio fade-in de entrada
        /// (el crossfade lo controla esta ventana).
        /// </summary>
        private void PrecargarMenuPrincipal()
        {
            try
            {
                _menuPrincipalPrecargado = new MenuPrincipal(OrigenIngreso.ReconocimientoFacial) { Opacity = 0 };
            }
            catch (Exception ex)
            {
                RegistrarEnLog("ERROR AL PRECARGAR MENU PRINCIPAL: " + ex.Message);
                _menuPrincipalPrecargado = null;
            }
        }

        /// <summary>
        /// Realiza un crossfade real entre esta ventana y <see cref="MenuPrincipal"/>:
        /// ambas animaciones corren en paralelo (una se apaga mientras la otra se
        /// enciende), en vez de esperar a que termine el fade-out para recién crear
        /// la ventana nueva. Si la precarga falló, se crea aquí como respaldo.
        /// </summary>
        private void AbrirMenuPrincipalConFade()
        {
            var menu = _menuPrincipalPrecargado;

            try
            {
                menu ??= new MenuPrincipal(OrigenIngreso.ReconocimientoFacial);
                menu.Opacity = 0;
                menu.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir el menú principal: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            var fadeIn = new DoubleAnimation(0d, 1d, DuracionFade)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            menu.BeginAnimation(OpacityProperty, fadeIn);

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionFade)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (s, e) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>
        /// Inicia la sesión del usuario verificado. Devuelve <c>false</c> si no se
        /// encontró su registro en la tabla Usuario (caso poco común: rostro
        /// reconocido pero cuenta inconsistente en base de datos).
        /// </summary>
        private bool IniciarSesionCompleta(string email)
        {
            var fila = _db.ObtenerUsuarioPorEmail(email);

            if (fila == null)
            {
                RegistrarEnLog($"ADVERTENCIA - Se reconoció el rostro de {email} pero no se encontró su registro en Usuario.");
                MessageBox.Show("Se verificó tu rostro, pero no se encontró tu usuario en el sistema.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string nombreCompleto = fila["Usuario_Nombre"].ToString();
            string apellido = fila["Usuario_Apellido"].ToString();
            string rol = fila["Usuario_Rol"].ToString();

            SesionActual.IniciarSesion(email, nombreCompleto, apellido, rol);
            return true;
        }

        private async Task AccesoDenegadoAsync(string motivo, int labelDetectado, double distancia)
        {
            RegistrarEnLog($"ACCESO DENEGADO - Motivo: {motivo} - Label: {labelDetectado} - Distancia: {distancia:F2}");

            try
            {
                await Task.Run(() =>
                    _db.RegistrarIntentoFallidoReconocimiento(_correoEsperado, labelDetectado, distancia));
            }
            catch (Exception ex)
            {
                RegistrarEnLog("ERROR AL REGISTRAR INTENTO FALLIDO EN BD: " + ex.Message);
            }

            await ActualizarEstadoBloqueoAsync();

            DetenerCamaraInterno();

            if (_bloqueadoHasta.HasValue)
            {
                MessageBox.Show(
                    $"Se superó el número máximo de intentos fallidos ({MAX_INTENTOS_FALLIDOS}).\n" +
                    $"El acceso ha sido bloqueado por {MINUTOS_BLOQUEO} minutos.",
                    "Acceso bloqueado", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ActualizarEstadoUI("Rostro no reconocido", BrushError);

            var respuesta = MessageBox.Show(
                "Intento fallido. ¿Desea intentar de nuevo?",
                "Acceso denegado", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (respuesta == MessageBoxResult.Yes)
            {
                btnIniciarCamara_Click(this, new RoutedEventArgs());
            }
            else
            {
                ActualizarEstadoUI("En espera...", BrushEspera);
            }
        }

        #endregion

        #region Utilidades: render de frame, arrastre de ventana, navegación

        private void MostrarFrame(Image<Bgr, byte> imagen)
        {
            using var bitmap = imagen.ToBitmap();
            imgCamara.Source = BitmapToBitmapSource(bitmap);
        }

        private static BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            try
            {
                var bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                bs.Freeze();
                return bs;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }

        [LibraryImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DeleteObject(IntPtr hObject);

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            IrAOpcionSesion();
        }

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();
            base.OnClosed(e);
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            IrAOpcionSesion();
        }

        #endregion
    }
}