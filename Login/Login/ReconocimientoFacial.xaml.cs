using Dasboard_Prueba;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Login.Clases;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Login
{
    /// <summary>
    /// Ventana de verificación facial.
    /// Requiere paquetes NuGet: Emgu.CV, Emgu.CV.runtime.windows (o windows.x64), Emgu.CV.Bitmap
    /// Requiere los clasificadores Haar Cascade (haarcascade_frontalface_default.xml y
    /// haarcascade_eye.xml, descargables del repositorio oficial de OpenCV) dentro de
    /// una carpeta "Recursos" junto al ejecutable.
    ///
    /// La conexión a base de datos se maneja exclusivamente a través de ClsConexion
    /// (App.config), sin cadenas de conexión hardcodeadas en esta clase.
    /// </summary>
    public partial class ReconocimientoFacial : Window
    {
        // ---------------------- Colores de estado (brushes congelados) ----------------------

        private static readonly SolidColorBrush BrushEspera = CrearBrush("#2d3050");
        private static readonly SolidColorBrush BrushAlerta = CrearBrush("#f5a623");
        private static readonly SolidColorBrush BrushError = CrearBrush("#7b1f1f");
        private static readonly SolidColorBrush BrushInfo = CrearBrush("#4f6ef7");
        private static readonly SolidColorBrush BrushExito = CrearBrush("#2ECC71");

        private static SolidColorBrush CrearBrush(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
            brush.Freeze();
            return brush;
        }

        // ---------------------- Configuración general ----------------------

        private readonly RepositorioSql _db = new();
        private const int MAX_INTENTOS_FALLIDOS = 5;
        private const int MINUTOS_BLOQUEO = 3;

        // Umbral de distancia LBPH: cuanto menor, más estricta la coincidencia.
        // IMPORTANTE: este valor DEBE recalibrarse con datos reales de tu instalación.
        private const double UMBRAL_CONFIANZA_LBPH = 50.0;

        // Ventana de frames (a ~30 fps) dentro de la cual se debe superar la prueba de vida.
        private const int FRAMES_PRUEBA_VIDA = 90;
        private const int UMBRAL_MOVIMIENTO_PX = 8;

        // Cantidad de frames consecutivos con predicción que se exigen antes de decidir el acceso.
        private const int FRAMES_CONSENSO_REQUERIDOS = 5;

        // Tolerancia de frames "ruidosos" dentro de la ventana de consenso.
        private const int TOLERANCIA_FRAMES_RUIDOSOS = 1;

        private const int SEGUNDOS_GRACIA = 3;

        private readonly string archivoIntentos =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seguridad_intentos.json");
        private readonly string archivoLog =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log_accesos.txt");

        // ---------------------- Cámara y clasificadores ----------------------
        private VideoCapture _camara;
        private DispatcherTimer _timer;
        private DispatcherTimer _timerBloqueo;

        private CascadeClassifier _clasificadorRostro;
        private CascadeClassifier _clasificadorOjos;

        private LBPHFaceRecognizer _reconocedor;
        private readonly Dictionary<int, string> _etiquetasNombres = new();
        private readonly Dictionary<int, string> _etiquetasEmails = new();

        // Evita iniciar la cámara mientras el modelo todavía se está entrenando en segundo plano.
        private bool _modeloListo = false;

        // ---------------------- Estado de prueba de vida ----------------------
        private bool _ojosVisiblesFramePrevio = false;
        private bool _esperandoReaperturaOjos = false;
        private int _parpadeosDetectados = 0;

        private System.Drawing.Point? _centroRostroPrevio = null;
        private double _movimientoAcumulado = 0;

        private int _framesProcesados = 0;
        private bool _pruebaVidaSuperada = false;
        private bool _verificando = false;

        private DateTime? _inicioGracia = null;

        // ---------------------- Estado de consenso multi-frame ----------------------
        private readonly List<(int Label, double Distance)> _historialPredicciones = new();

        private RegistroIntentos _registro;

        public ReconocimientoFacial()
        {
            InitializeComponent();

            CargarRegistroIntentos();
            InicializarClasificadores();
            ActualizarUIBloqueo();

            btnIniciarCamara.IsEnabled = false;
            txtEstado.Text = "Cargando rostros registrados...";
            _ = CargarYEntrenarAsync();
        }

        private bool EstaBloqueado => _registro.BloqueadoHasta.HasValue &&
                                       _registro.BloqueadoHasta.Value > DateTime.Now;

        private async Task CargarYEntrenarAsync()
        {
            try
            {
                var (imagenes, etiquetas, nombres, emails) = await Task.Run(EntrenarReconocedor);

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

                if (!EstaBloqueado)
                {
                    btnIniciarCamara.IsEnabled = true;
                    txtEstado.Text = "En espera...";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al preparar el reconocimiento facial: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtEstado.Text = "Error al cargar rostros registrados";
            }
        }

        // ======================================================================
        //  PERSISTENCIA DE INTENTOS FALLIDOS / BLOQUEO
        // ======================================================================

        private class RegistroIntentos
        {
            public int IntentosFallidos { get; set; } = 0;
            public DateTime? BloqueadoHasta { get; set; } = null;
        }

        private void CargarRegistroIntentos()
        {
            try
            {
                _registro = File.Exists(archivoIntentos)
                    ? JsonSerializer.Deserialize<RegistroIntentos>(File.ReadAllText(archivoIntentos)) ?? new RegistroIntentos()
                    : new RegistroIntentos();
            }
            catch
            {
                _registro = new RegistroIntentos();
            }
        }

        private void GuardarRegistroIntentos()
        {
            try
            {
                File.WriteAllText(archivoIntentos, JsonSerializer.Serialize(_registro));
            }
            catch
            {
                // El fallo al guardar no debe interrumpir el flujo de autenticación.
            }
        }

        private void RegistrarEnLog(string mensaje)
        {
            try
            {
                File.AppendAllText(archivoLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}{Environment.NewLine}");
            }
            catch
            {
                // El registro de log no debe interrumpir el flujo de autenticación.
            }
        }

        private void ActualizarUIBloqueo()
        {
            if (!EstaBloqueado)
            {
                if (_modeloListo) btnIniciarCamara.IsEnabled = true;
                return;
            }

            btnIniciarCamara.IsEnabled = false;
            elipseEstado.Fill = BrushError;

            _timerBloqueo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerBloqueo.Tick += TimerBloqueo_Tick;
            _timerBloqueo.Start();

            txtEstado.Text = "Bloqueado temporalmente";
        }

        private void TimerBloqueo_Tick(object sender, EventArgs e)
        {
            var restante = _registro.BloqueadoHasta.Value - DateTime.Now;

            if (restante <= TimeSpan.Zero)
            {
                _timerBloqueo.Stop();
                _registro.BloqueadoHasta = null;
                _registro.IntentosFallidos = 0;
                GuardarRegistroIntentos();

                if (_modeloListo) btnIniciarCamara.IsEnabled = true;
                txtEstado.Text = "En espera...";
                elipseEstado.Fill = BrushEspera;
            }
            else
            {
                txtEstado.Text = $"Bloqueado. Intenta de nuevo en {restante.Minutes:D2}:{restante.Seconds:D2}";
            }
        }

        // ======================================================================
        //  INICIALIZACIÓN DE CLASIFICADORES Y ENTRENAMIENTO
        // ======================================================================

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
        /// Aplica ecualización de histograma adaptativa (CLAHE) a un rostro en escala de
        /// grises. Se usa SIEMPRE en el mismo punto del pipeline tanto al entrenar como al
        /// reconocer, para reducir el efecto de diferencias de iluminación.
        /// </summary>
        private static Image<Gray, byte> NormalizarIluminacion(Image<Gray, byte> imgGris)
        {
            var salida = new Image<Gray, byte>(imgGris.Size);
            CvInvoke.CLAHE(imgGris, 2.0, new System.Drawing.Size(8, 8), 256, salida);
            return salida;
        }

        /// <summary>
        /// Se ejecuta en un hilo de fondo (Task.Run): se conecta a la base de datos vía
        /// ClsConexion, recorta y normaliza cada rostro, y devuelve las listas ya listas
        /// para entrenar. No toca controles de UI directamente.
        /// </summary>
        private (List<Image<Gray, byte>> Imagenes, List<int> Etiquetas,
                 Dictionary<int, string> Nombres, Dictionary<int, string> Emails) EntrenarReconocedor()
        {
            var imagenes = new List<Image<Gray, byte>>();
            var etiquetas = new List<int>();
            var etiquetasNombres = new Dictionary<int, string>();
            var etiquetasEmails = new Dictionary<int, string>();

            if (_clasificadorRostro == null)
                return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);

            // Se agrupan las fotos por Usuario_Email (identificador único real), no por
            // Nombre, para evitar mezclar a dos personas con el mismo nombre.
            var fotosPorPersona = new Dictionary<string, (string Nombre, List<byte[]> Fotos)>();

            using var conexion = new ClsConexion();
            try
            {
                if (!conexion.Abrir())
                    return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);

                using var cmd = new SqlCommand(
                    "SELECT Usuario_Email, Nombre, Foto FROM ReconocimientoFacial WHERE Usuario_Email IS NOT NULL",
                    conexion.SqlC);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string email = reader.GetString(reader.GetOrdinal("Usuario_Email"));
                    string nombre = reader.GetString(reader.GetOrdinal("Nombre"));
                    byte[] foto = (byte[])reader["Foto"];

                    if (!fotosPorPersona.TryGetValue(email, out var entrada))
                    {
                        entrada = (nombre, new List<byte[]>());
                        fotosPorPersona[email] = entrada;
                    }
                    entrada.Fotos.Add(foto);
                }
            }
            catch (Exception ex)
            {
                RegistrarEnLog("ERROR AL CARGAR ROSTROS: " + ex.Message);
                throw;
            }
            finally
            {
                conexion.Cerrar();
            }

            int etiquetaActual = 0;
            var conteoValidasPorPersona = new Dictionary<string, int>();

            foreach (var persona in fotosPorPersona)
            {
                string email = persona.Key;
                string nombre = persona.Value.Nombre;
                bool tieneImagenesValidas = false;
                int validasDeEstaPersona = 0;

                foreach (var datosFoto in persona.Value.Fotos)
                {
                    try
                    {
                        using var ms = new MemoryStream(datosFoto);
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
                        etiquetas.Add(etiquetaActual);
                        tieneImagenesValidas = true;
                        validasDeEstaPersona++;
                    }
                    catch
                    {
                        // Se ignora una foto corrupta o no procesable y se continúa con el resto.
                    }
                }

                conteoValidasPorPersona[email] = validasDeEstaPersona;

                if (tieneImagenesValidas)
                {
                    etiquetasNombres[etiquetaActual] = nombre;
                    etiquetasEmails[etiquetaActual] = email;
                    etiquetaActual++;
                }
            }

            var resumen = string.Join(" | ", conteoValidasPorPersona.Select(kv =>
                $"{kv.Key}: {kv.Value}/{fotosPorPersona[kv.Key].Fotos.Count}"));
            RegistrarEnLog($"ENTRENAMIENTO - Fotos válidas por persona -> {resumen}");
            System.Diagnostics.Debug.WriteLine($"[ENTRENAMIENTO] {resumen}");

            return (imagenes, etiquetas, etiquetasNombres, etiquetasEmails);
        }

        // ======================================================================
        //  CONTROL DE CÁMARA
        // ======================================================================

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (EstaBloqueado)
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

            txtEstado.Text = $"Prepárate, colócate frente a la cámara... {SEGUNDOS_GRACIA}";
            elipseEstado.Fill = BrushAlerta;

            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamaraInterno();
            txtEstado.Text = "En espera...";
            elipseEstado.Fill = BrushEspera;
            bdNombreReconocido.Visibility = Visibility.Collapsed;
        }

        private void DetenerCamaraInterno()
        {
            _timer?.Stop();
            _timer = null;

            _camara?.Dispose();
            _camara = null;
            _verificando = false;
            _inicioGracia = null;
            _historialPredicciones.Clear();

            btnDetenerCamara.IsEnabled = false;

            if (!EstaBloqueado && _modeloListo)
                btnIniciarCamara.IsEnabled = true;

            pnlSinCamara.Visibility = Visibility.Visible;
            imgCamara.Source = null;
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

        // ======================================================================
        //  PROCESAMIENTO DE CADA FRAME
        // ======================================================================

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
                txtEstado.Text = "No se detecta ningún rostro...";
                elipseEstado.Fill = BrushEspera;
                MostrarFrame(imgColor);
                return;
            }

            var rostroRect = rostros.OrderByDescending(r => r.Width * r.Height).First();
            imgColor.Draw(rostroRect, new Bgr(System.Drawing.Color.LimeGreen), 2);

            if (!_pruebaVidaSuperada)
            {
                ProcesarPruebaDeVida(imgGris, rostroRect);
                MostrarFrame(imgColor);

                if (_pruebaVidaSuperada)
                {
                    txtEstado.Text = "Prueba de vida superada. Verificando identidad...";
                    elipseEstado.Fill = BrushInfo;
                }
                return;
            }

            if (!_verificando)
            {
                VerificarIdentidad(imgGris, rostroRect);
            }

            MostrarFrame(imgColor);
        }

        // ======================================================================
        //  PRUEBA DE VIDA: PARPADEO + MOVIMIENTO DE CABEZA
        // ======================================================================

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

        // ======================================================================
        //  COMPARACIÓN DE ROSTRO Y DECISIÓN DE ACCESO (con consenso multi-frame)
        // ======================================================================

        private void VerificarIdentidad(Image<Gray, byte> imgGris, System.Drawing.Rectangle rostroRect)
        {
            using var rostroRecortado = imgGris.Copy(rostroRect);
            using var rostroEcualizado = NormalizarIluminacion(rostroRecortado);
            using var rostroNormalizado = rostroEcualizado.Resize(200, 200, Inter.Cubic);

            var resultado = _reconocedor.Predict(rostroNormalizado);
            _historialPredicciones.Add((resultado.Label, resultado.Distance));

            System.Diagnostics.Debug.WriteLine(
                $"[VERIFICACION] Frame {_historialPredicciones.Count}/{FRAMES_CONSENSO_REQUERIDOS} " +
                $"| Label: {resultado.Label} | Distancia: {resultado.Distance:F2} | Umbral: {UMBRAL_CONFIANZA_LBPH}");

            if (_historialPredicciones.Count < FRAMES_CONSENSO_REQUERIDOS)
            {
                txtEstado.Text = $"Confirmando identidad ({_historialPredicciones.Count}/{FRAMES_CONSENSO_REQUERIDOS})...";
                return;
            }

            _verificando = true;

            var etiquetaMasComun = _historialPredicciones
                .GroupBy(p => p.Label)
                .OrderByDescending(g => g.Count())
                .First();

            bool consensoSuficiente = etiquetaMasComun.Count() >= FRAMES_CONSENSO_REQUERIDOS - TOLERANCIA_FRAMES_RUIDOSOS;
            double distanciaPromedio = etiquetaMasComun.Average(p => p.Distance);

            bool coincide = consensoSuficiente &&
                            etiquetaMasComun.Key >= 0 &&
                            distanciaPromedio <= UMBRAL_CONFIANZA_LBPH &&
                            _etiquetasNombres.ContainsKey(etiquetaMasComun.Key) &&
                            _etiquetasEmails.ContainsKey(etiquetaMasComun.Key);

            RegistrarEnLog(
                $"CONSENSO - Etiqueta mayoritaria: {etiquetaMasComun.Key} " +
                $"({etiquetaMasComun.Count()}/{FRAMES_CONSENSO_REQUERIDOS} frames) " +
                $"| Distancia promedio: {distanciaPromedio:F2} | Coincide: {coincide}");

            _historialPredicciones.Clear();

            if (coincide)
            {
                AccesoConcedido(_etiquetasNombres[etiquetaMasComun.Key], _etiquetasEmails[etiquetaMasComun.Key]);
            }
            else
            {
                AccesoDenegado("Rostro no coincide con ningún registro (o consenso insuficiente)",
                    etiquetaMasComun.Key, distanciaPromedio);
            }
        }

        private void AccesoConcedido(string nombre, string email)
        {
            _registro.IntentosFallidos = 0;
            _registro.BloqueadoHasta = null;
            GuardarRegistroIntentos();

            RegistrarEnLog($"ACCESO CONCEDIDO - Usuario: {nombre} ({email})");

            DetenerCamaraInterno();

            txtEstado.Text = "Identidad verificada";
            elipseEstado.Fill = BrushExito;
            txtNombreReconocido.Text = nombre;
            bdNombreReconocido.Visibility = Visibility.Visible;

            var temporizadorTransicion = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            temporizadorTransicion.Tick += (s, e) =>
            {
                temporizadorTransicion.Stop();

                try
                {
                    IniciarSesionCompleta(email);

                    var menu = new MenuPrincipal();
                    menu.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo abrir el menú principal: " + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            temporizadorTransicion.Start();
        }

        /// <summary>
        /// Consulta los datos completos del usuario reconocido (nombre, apellido, rol)
        /// y llena SesionActual exactamente igual que lo haría el login normal, para que
        /// el resto de la app funcione igual sin importar el método de inicio de sesión.
        /// </summary>
        private void IniciarSesionCompleta(string email)
        {
            var fila = _db.ObtenerUsuarioPorEmail(email);

            if (fila == null)
            {
                RegistrarEnLog($"ADVERTENCIA - Se reconoció el rostro de {email} pero no se encontró su registro en Usuario.");
                MessageBox.Show("Se verificó tu rostro, pero no se encontró tu usuario en el sistema.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nombreCompleto = fila["Usuario_Nombre"].ToString();
            string apellido = fila["Usuario_Apellido"].ToString();
            string rol = fila["Usuario_Rol"].ToString();

            SesionActual.IniciarSesion(email, nombreCompleto, apellido, rol);
        }

        private void AccesoDenegado(string motivo, int labelDetectado, double distancia)
        {
            _registro.IntentosFallidos++;
            RegistrarEnLog($"ACCESO DENEGADO - Motivo: {motivo} - Label: {labelDetectado} " +
                            $"- Distancia: {distancia:F2} - Intento N.{_registro.IntentosFallidos}");

            if (_registro.IntentosFallidos >= MAX_INTENTOS_FALLIDOS)
            {
                _registro.BloqueadoHasta = DateTime.Now.AddMinutes(MINUTOS_BLOQUEO);
                RegistrarEnLog($"CUENTA BLOQUEADA TEMPORALMENTE hasta {_registro.BloqueadoHasta:yyyy-MM-dd HH:mm:ss}");
                GuardarRegistroIntentos();

                DetenerCamaraInterno();

                MessageBox.Show(
                    $"Se superó el número máximo de intentos fallidos ({MAX_INTENTOS_FALLIDOS}).\n" +
                    $"El acceso ha sido bloqueado por {MINUTOS_BLOQUEO} minutos.",
                    "Acceso bloqueado", MessageBoxButton.OK, MessageBoxImage.Error);

                ActualizarUIBloqueo();
                return;
            }

            GuardarRegistroIntentos();
            DetenerCamaraInterno();

            txtEstado.Text = $"Rostro no reconocido ({_registro.IntentosFallidos}/{MAX_INTENTOS_FALLIDOS} intentos)";
            elipseEstado.Fill = BrushError;

            int intentosRestantes = MAX_INTENTOS_FALLIDOS - _registro.IntentosFallidos;
            string plural = intentosRestantes == 1 ? "intento" : "intentos";

            var respuesta = MessageBox.Show(
                $"Intento fallido. ¿Desea intentar de nuevo? (quedan {intentosRestantes} {plural})",
                "Acceso denegado", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (respuesta == MessageBoxResult.Yes)
            {
                btnIniciarCamara_Click(this, new RoutedEventArgs());
            }
            else
            {
                txtEstado.Text = "En espera...";
                elipseEstado.Fill = BrushEspera;
            }
        }

        // ======================================================================
        //  UTILIDADES: RENDER DE FRAME, ARRASTRE DE VENTANA, NAVEGACIÓN
        // ======================================================================

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
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();

            // Punto de integración: cerrar esta ventana o navegar a la anterior, por ejemplo:
            // this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();
            base.OnClosed(e);
        }
    }
}