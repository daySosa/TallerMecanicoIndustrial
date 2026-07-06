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
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Login
{

    public partial class ReconocimientoFacial : Window
    {
        // ---------------------- Configuración general ----------------------
        private const int MAX_INTENTOS_FALLIDOS = 5;
        private const int MINUTOS_BLOQUEO = 3;
        private readonly RepositorioSql _db = new();

        private const double UMBRAL_CONFIANZA_LBPH = 90.0;

        private const int FRAMES_PRUEBA_VIDA = 90;
        private const int UMBRAL_MOVIMIENTO_PX = 8;

        private const int FRAMES_CONSENSO_REQUERIDOS = 5;
        private const int TOLERANCIA_FRAMES_RUIDOSOS = 1;

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
        private readonly string _correoEsperado;

        private bool _modeloListo = false;

        // ---------------------- Estado de bloqueo (ahora respaldado por BD) ----------------------
        private DateTime? _bloqueadoHasta = null;

        // ---------------------- Estado de prueba de vida ----------------------
        private bool _ojosVisiblesFramePrevio = false;
        private bool _esperandoReaperturaOjos = false;
        private int _parpadeosDetectados = 0;

        private System.Drawing.Point? _centroRostroPrevio = null;
        private double _movimientoAcumulado = 0;

        private int _framesProcesados = 0;
        private bool _pruebaVidaSuperada = false;
        private bool _verificando = false;

        private const int SEGUNDOS_GRACIA = 3;
        private DateTime? _inicioGracia = null;

        private readonly List<(int Label, double Distance)> _historialPredicciones = new();

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

        private async Task CargarYEntrenarAsync()
        {
            try
            {
                var (imagenes, etiquetas, nombres, emails) = await Task.Run(() => EntrenarReconocedor());

                await ActualizarEstadoBloqueoAsync();

                if (imagenes.Count == 0)
                {
                    txtEstado.Text = "Sin rostros registrados en la base de datos";
                    return;
                }

                _reconocedor = new LBPHFaceRecognizer(1, 8, 8, 8, double.MaxValue);

                using var vectorImagenes = new Emgu.CV.Util.VectorOfMat();
                foreach (var img in imagenes)
                    vectorImagenes.Push(img.Mat);

                using var vectorEtiquetas = new Emgu.CV.Util.VectorOfInt(etiquetas.ToArray());

                await Task.Run(() => _reconocedor.Train(vectorImagenes, vectorEtiquetas));

                foreach (var img in imagenes) img.Dispose();

                _etiquetasNombres.Clear();
                _etiquetasEmails.Clear();
                foreach (var kv in nombres) _etiquetasNombres[kv.Key] = kv.Value;
                foreach (var kv in emails) _etiquetasEmails[kv.Key] = kv.Value;

                _modeloListo = true;

                if (_bloqueadoHasta is null)
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
        //  BLOQUEO POR INTENTOS FALLIDOS (respaldado por IntentosAccesoFallidos)
        // ======================================================================


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


            _bloqueadoHasta = estado.UltimoIntento.Value.ToLocalTime().AddMinutes(MINUTOS_BLOQUEO);

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
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x7b, 0x1f, 0x1f));
            txtEstado.Text = "Bloqueado temporalmente";

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
                    txtEstado.Text = "En espera...";
                    elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2d, 0x30, 0x50));
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

        private static Image<Gray, byte> NormalizarIluminacion(Image<Gray, byte> imgGris)
        {
            var salida = new Image<Gray, byte>(imgGris.Size);
            CvInvoke.CLAHE(imgGris, 2.0, new System.Drawing.Size(8, 8), 256, salida);
            return salida;
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

            var fotosPorPersona = new Dictionary<string, (string Nombre, List<byte[]> Fotos)>();
            foreach (var persona in personas)
            {
                if (string.IsNullOrWhiteSpace(persona.Email)) continue;

                if (!fotosPorPersona.ContainsKey(persona.Email))
                    fotosPorPersona[persona.Email] = (persona.Nombre, new List<byte[]>());

                fotosPorPersona[persona.Email].Fotos.Add(persona.Foto);
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
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));

            _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(33) };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamaraInterno();
            txtEstado.Text = "En espera...";
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2d, 0x30, 0x50));
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

            bool estaBloqueado = _bloqueadoHasta.HasValue && _bloqueadoHasta.Value > DateTime.Now;
            if (!estaBloqueado && _modeloListo)
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
                elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2d, 0x30, 0x50));
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
                    elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x4f, 0x6e, 0xf7));
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

            string emailDetectado = string.Empty;
            bool seEncontroEmail = coincide && _etiquetasEmails.TryGetValue(etiquetaMasComun.Key, out emailDetectado);

            if (seEncontroEmail && emailDetectado.Equals(_correoEsperado, StringComparison.OrdinalIgnoreCase))
            {
                string nombre = _etiquetasNombres[etiquetaMasComun.Key];
                AccesoConcedido(nombre, emailDetectado);
            }
            else if (seEncontroEmail)
            {
                RegistrarEnLog($"RECHAZADO - Rostro pertenece a {emailDetectado}, pero se esperaba {_correoEsperado}");
                _ = AccesoDenegadoAsync("El rostro no corresponde a la cuenta que intentas verificar",
                    etiquetaMasComun.Key, distanciaPromedio);
            }
            else
            {
                _ = AccesoDenegadoAsync("Rostro no coincide con ningún registro (o consenso insuficiente)",
                    etiquetaMasComun.Key, distanciaPromedio);
            }
        }

        private void AccesoConcedido(string nombre, string email)
        {

            RegistrarEnLog($"ACCESO CONCEDIDO - Usuario: {nombre} ({email})");

            DetenerCamaraInterno();

            txtEstado.Text = "Identidad verificada";
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
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

            txtEstado.Text = "Rostro no reconocido";
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x7b, 0x1f, 0x1f));

            var respuesta = MessageBox.Show(
                "Intento fallido. ¿Desea intentar de nuevo?",
                "Acceso denegado", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (respuesta == MessageBoxResult.Yes)
            {
                btnIniciarCamara_Click(this, new RoutedEventArgs());
            }
            else
            {
                txtEstado.Text = "En espera...";
                elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2d, 0x30, 0x50));
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

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();
        }

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();
            base.OnClosed(e);
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamaraInterno();
            _timerBloqueo?.Stop();
            this.Close();
        }
    }
}