using Dasboard_Prueba;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Requiere una carpeta "RostrosRegistrados" con subcarpetas por persona
    /// (ej: RostrosRegistrados/Juan/foto1.jpg, foto2.jpg...) usadas para entrenar
    /// el reconocedor al iniciar la ventana.
    ///
    /// CAMBIOS EN ESTA VERSIÓN (corrección de falsos positivos):
    /// 1. Umbral LBPH ahora debe recalibrarse con datos reales (ver diagnóstico en consola/log).
    /// 2. Se aplica CLAHE (ecualización adaptativa) tanto al entrenar como al reconocer,
    ///    para reducir el efecto de diferencias de iluminación entre fotos y cámara en vivo.
    /// 3. La decisión de acceso ya NO se toma con un solo frame: se exige consenso de varios
    ///    frames consecutivos con la misma etiqueta antes de conceder o denegar acceso.
    /// 4. Se eliminaron los MessageBox de diagnóstico temporal (bloqueaban el hilo de UI);
    ///    ahora el detalle de cada predicción se escribe en Debug.WriteLine y en el log.
    /// </summary>
    public partial class ReconocimientoFacial : Window
    {
        // ---------------------- Configuración general ----------------------
        private const int MAX_INTENTOS_FALLIDOS = 3;
        private const int MINUTOS_BLOQUEO = 5;

        // Umbral de distancia LBPH: cuanto menor, más estricta la coincidencia.
        // IMPORTANTE: este valor DEBE recalibrarse con datos reales de tu instalación.
        // Usa el log/Debug.WriteLine de VerificarIdentidad para recolectar distancias de:
        //   a) personas SÍ registradas -> anota el rango típico de distancias
        //   b) personas NO registradas -> anota el rango típico de distancias
        // El umbral correcto es el punto medio entre ambos rangos. Con CLAHE aplicado,
        // valores típicos suelen bajar respecto a los que tenías sin ecualizar (65.0 era
        // demasiado permisivo). Empieza probando con 50.0 y ajusta según tus mediciones.
        private const double UMBRAL_CONFIANZA_LBPH = 50.0;

        // Ventana de frames (a ~30 fps) dentro de la cual se debe superar la prueba de vida.
        private const int FRAMES_PRUEBA_VIDA = 90;
        private const int UMBRAL_MOVIMIENTO_PX = 8;

        // Cantidad de frames consecutivos con predicción que se exigen antes de decidir
        // el acceso. Esto evita que un solo frame con ruido/mala luz decida todo.
        private const int FRAMES_CONSENSO_REQUERIDOS = 5;

        // Tolerancia de frames "ruidosos" dentro de la ventana de consenso (frames que caen
        // en una etiqueta distinta a la mayoritaria, pero no invalidan el consenso).
        private const int TOLERANCIA_FRAMES_RUIDOSOS = 1;

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

        // ---------------------- Estado de prueba de vida ----------------------
        private bool _ojosVisiblesFramePrevio = false;
        private bool _esperandoReaperturaOjos = false;
        private int _parpadeosDetectados = 0;

        private System.Drawing.Point? _centroRostroPrevio = null;
        private double _movimientoAcumulado = 0;

        private int _framesProcesados = 0;
        private bool _pruebaVidaSuperada = false;
        private bool _verificando = false; // true solo mientras se evalúa la decisión final de acceso

        // ---------------------- Periodo de gracia al iniciar la cámara ----------------------
        // Le da tiempo a la persona de acomodarse frente a la cámara antes de que se
        // empiece a evaluar la prueba de vida o el reconocimiento. Durante este periodo
        // solo se muestra el video en vivo con una cuenta regresiva, sin contar como intento.
        private const int SEGUNDOS_GRACIA = 3;
        private DateTime? _inicioGracia = null;

        // ---------------------- Estado de consenso multi-frame ----------------------
        private readonly List<(int Label, double Distance)> _historialPredicciones = new();

        private RegistroIntentos _registro;

        public ReconocimientoFacial()
        {
            InitializeComponent();

            // ---------------------------------------------------------------------
            // TEMPORAL: importa las fotos de la carpeta local a la base de datos.
            // Ejecutar una sola vez y luego BORRAR esta línea (y el comentario)
            // para que no se vuelva a correr y duplique las fotos en la tabla.
            // ---------------------------------------------------------------------
            // Login.Clases.ImportadorRostros.ImportarDesdeCarpeta(@"C:\Users\Valeria Perdomo\Desktop\PersonasRegistradas");

            CargarRegistroIntentos();
            InicializarClasificadores();
            EntrenarReconocedor();
            ActualizarUIBloqueo();
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
                if (File.Exists(archivoIntentos))
                {
                    string json = File.ReadAllText(archivoIntentos);
                    _registro = JsonSerializer.Deserialize<RegistroIntentos>(json) ?? new RegistroIntentos();
                }
                else
                {
                    _registro = new RegistroIntentos();
                }
            }
            catch
            {
                // Si el archivo está corrupto, se reinicia el registro por seguridad.
                _registro = new RegistroIntentos();
            }
        }

        private void GuardarRegistroIntentos()
        {
            try
            {
                string json = JsonSerializer.Serialize(_registro);
                File.WriteAllText(archivoIntentos, json);
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
                string linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mensaje}{Environment.NewLine}";
                File.AppendAllText(archivoLog, linea);
            }
            catch
            {
                // El registro de log no debe interrumpir el flujo de autenticación.
            }
        }

        private void ActualizarUIBloqueo()
        {
            bool estaBloqueado = _registro.BloqueadoHasta.HasValue &&
                                  _registro.BloqueadoHasta.Value > DateTime.Now;

            if (!estaBloqueado)
            {
                btnIniciarCamara.IsEnabled = true;
                return;
            }

            btnIniciarCamara.IsEnabled = false;
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x7b, 0x1f, 0x1f));

            _timerBloqueo = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timerBloqueo.Tick += (s, e) =>
            {
                var restante = _registro.BloqueadoHasta.Value - DateTime.Now;

                if (restante <= TimeSpan.Zero)
                {
                    _timerBloqueo.Stop();
                    _registro.BloqueadoHasta = null;
                    _registro.IntentosFallidos = 0;
                    GuardarRegistroIntentos();

                    btnIniciarCamara.IsEnabled = true;
                    txtEstado.Text = "En espera...";
                    elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2d, 0x30, 0x50));
                }
                else
                {
                    txtEstado.Text = $"Bloqueado. Intenta de nuevo en {restante.Minutes:D2}:{restante.Seconds:D2}";
                }
            };
            _timerBloqueo.Start();

            txtEstado.Text = "Bloqueado temporalmente";
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
        /// grises. Se usa SIEMPRE en el mismo punto del pipeline (después de recortar el
        /// rostro, antes de redimensionar) tanto al entrenar como al reconocer, para que
        /// el modelo y las predicciones en vivo estén en las mismas condiciones de contraste
        /// y no se vean afectados por diferencias de iluminación entre las fotos de la base
        /// de datos y el video de la cámara.
        /// </summary>
        private Image<Gray, byte> NormalizarIluminacion(Image<Gray, byte> imgGris)
        {
            // Se usa el método estático CvInvoke.CLAHE en vez de instanciar la clase CLAHE
            // directamente, ya que en algunas versiones de Emgu.CV el constructor público
            // no está disponible y da error CS0246 ("no se encontró el tipo CLAHE").
            // CvInvoke sí forma parte del núcleo de Emgu.CV y no requiere referencias extra.
            var salida = new Image<Gray, byte>(imgGris.Size);
            // El cuarto parámetro (256) es la cantidad de bins del histograma; 256 es el
            // valor estándar para imágenes de 8 bits por canal (escala de grises 0-255).
            CvInvoke.CLAHE(imgGris, 2.0, new System.Drawing.Size(8, 8), 256, salida);
            return salida;
        }

        private void EntrenarReconocedor()
        {
            var imagenes = new List<Image<Gray, byte>>();
            var etiquetas = new List<int>();
            _etiquetasNombres.Clear();

            if (_clasificadorRostro == null)
                return;

            // Se agrupan las fotos por nombre de persona, tal como llegan de la tabla.
            var fotosPorPersona = new Dictionary<string, List<byte[]>>();

            // NOTA: se usa una cadena de conexión propia (en vez de clsConexion) para no
            // modificar esa clase compartida por el equipo. Se agrega "Authentication=SqlPassword;"
            // porque las versiones recientes de Microsoft.Data.SqlClient, al detectar un servidor
            // "*.database.windows.net", intentan autenticar con Azure Active Directory por defecto
            // si no se especifica el tipo de autenticación.
            const string cadenaConexion =
                "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;" +
                "User ID=DayanaSosa;Password=Serv2026;Authentication=SqlPassword;";

            using var conexion = new Microsoft.Data.SqlClient.SqlConnection(cadenaConexion);
            try
            {
                conexion.Open();

                using var cmd = new Microsoft.Data.SqlClient.SqlCommand(
                    "SELECT Nombre, Foto FROM RostrosRegistrados", conexion);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string nombre = reader.GetString(reader.GetOrdinal("Nombre"));
                    byte[] foto = (byte[])reader["Foto"];

                    if (!fotosPorPersona.ContainsKey(nombre))
                        fotosPorPersona[nombre] = new List<byte[]>();

                    fotosPorPersona[nombre].Add(foto);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los rostros registrados desde la base de datos: " + ex.Message,
                    "Error de base de datos", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // No se necesita "finally" con Cerrar(): el "using" en la declaración
            // de "conexion" ya cierra y libera la conexión automáticamente.

            int etiquetaActual = 0;
            var conteoValidasPorPersona = new Dictionary<string, int>();

            foreach (var persona in fotosPorPersona)
            {
                bool tieneImagenesValidas = false;
                int validasDeEstaPersona = 0;

                foreach (var datosFoto in persona.Value)
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

                conteoValidasPorPersona[persona.Key] = validasDeEstaPersona;

                if (tieneImagenesValidas)
                {
                    _etiquetasNombres[etiquetaActual] = persona.Key;
                    etiquetaActual++;
                }
            }

            // Diagnóstico de entrenamiento: cuántas fotos de cada persona pasaron la
            // detección de rostro y se usaron realmente para entrenar. Si alguna persona
            // queda con muy pocas fotos válidas (menos de 5-8), el modelo tendrá poca
            // variación de esa cara y será más fácil que otro rostro caiga "cerca" de ella.
            var resumen = string.Join(" | ", conteoValidasPorPersona.Select(kv =>
                $"{kv.Key}: {kv.Value}/{fotosPorPersona[kv.Key].Count}"));
            RegistrarEnLog($"ENTRENAMIENTO - Fotos válidas por persona -> {resumen}");
            System.Diagnostics.Debug.WriteLine($"[ENTRENAMIENTO] {resumen}");

            if (imagenes.Count == 0)
            {
                txtEstado.Text = "Sin rostros registrados en la base de datos";
                return;
            }

            // NOTA: la firma exacta del constructor/Create de LBPHFaceRecognizer puede variar
            // según la versión de Emgu.CV instalada. Si el constructor no compila, usar:
            // _reconocedor = LBPHFaceRecognizer.Create(1, 8, 8, 8, UMBRAL_CONFIANZA_LBPH);
            // IMPORTANTE: aquí se usa double.MaxValue (sin límite) en vez de UMBRAL_CONFIANZA_LBPH.
            // Si le ponemos un límite aquí, Emgu.CV oculta la distancia real (la reemplaza por
            // "infinito" y Label=-1) cada vez que alguien no coincide dentro de ese límite,
            // lo cual hace imposible calibrar el umbral con datos reales. Dejamos que el motor
            // siempre calcule y devuelva la distancia real, y decidimos nosotros en VerificarIdentidad
            // comparando esa distancia contra UMBRAL_CONFIANZA_LBPH.
            _reconocedor = new LBPHFaceRecognizer(1, 8, 8, 8, double.MaxValue);

            using var vectorImagenes = new Emgu.CV.Util.VectorOfMat();
            foreach (var img in imagenes)
                vectorImagenes.Push(img.Mat);

            using var vectorEtiquetas = new Emgu.CV.Util.VectorOfInt(etiquetas.ToArray());

            _reconocedor.Train(vectorImagenes, vectorEtiquetas);

            foreach (var img in imagenes) img.Dispose();
        }

        // ======================================================================
        //  CONTROL DE CÁMARA
        // ======================================================================

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (_registro.BloqueadoHasta.HasValue && _registro.BloqueadoHasta.Value > DateTime.Now)
            {
                MessageBox.Show("El acceso está temporalmente bloqueado por intentos fallidos.",
                    "Bloqueado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_clasificadorRostro == null || _reconocedor == null)
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

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
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

            bool estaBloqueado = _registro.BloqueadoHasta.HasValue &&
                                  _registro.BloqueadoHasta.Value > DateTime.Now;
            if (!estaBloqueado)
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

            // ---- Periodo de gracia: le da tiempo a la persona de acomodarse frente a la
            // cámara antes de empezar a evaluar prueba de vida o identidad. Durante este
            // lapso solo se muestra el video con una cuenta regresiva; nada de esto cuenta
            // como intento fallido. ----
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

            // Ya se superó la prueba de vida: seguimos alimentando el historial de
            // predicciones frame a frame hasta juntar el consenso necesario. La bandera
            // _verificando evita que VerificarIdentidad se reevalúe mientras se procesa
            // la decisión final (AccesoConcedido/AccesoDenegado detienen la cámara).
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

            // ---- Detección de parpadeo (se busca en la mitad superior del rostro) ----
            using var rostroROI = imgGris.Copy(rostroRect);
            var zonaSuperior = new System.Drawing.Rectangle(0, 0, rostroROI.Width, Math.Max(1, rostroROI.Height / 2));
            using var zonaOjos = rostroROI.Copy(zonaSuperior);

            var ojos = _clasificadorOjos.DetectMultiScale(zonaOjos, 1.1, 6, System.Drawing.Size.Empty);
            bool ojosVisiblesAhora = ojos.Length >= 1;

            // Un parpadeo = transición de "ojos visibles" a "no visibles" y de vuelta a "visibles".
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

            // ---- Detección de movimiento de cabeza (centroide del rostro entre frames) ----
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

            // ---- Evaluación conjunta ----
            bool huboParpadeo = _parpadeosDetectados >= 1;
            bool huboMovimiento = _movimientoAcumulado >= UMBRAL_MOVIMIENTO_PX;

            if (huboParpadeo && huboMovimiento)
            {
                _pruebaVidaSuperada = true;
                return;
            }

            if (_framesProcesados >= FRAMES_PRUEBA_VIDA)
            {
                // Se agotó la ventana de tiempo sin cumplir ambas condiciones.
                // No se cuenta como intento fallido de identidad: solo se reinicia la prueba de vida.
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

            // Ya tenemos suficientes frames: se evalúa el consenso y se decide el acceso.
            // Se bloquean nuevas llamadas a VerificarIdentidad mientras se resuelve esta decisión.
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
                            _etiquetasNombres.ContainsKey(etiquetaMasComun.Key);

            RegistrarEnLog(
                $"CONSENSO - Etiqueta mayoritaria: {etiquetaMasComun.Key} " +
                $"({etiquetaMasComun.Count()}/{FRAMES_CONSENSO_REQUERIDOS} frames) " +
                $"| Distancia promedio: {distanciaPromedio:F2} | Coincide: {coincide}");

            _historialPredicciones.Clear();

            if (coincide)
            {
                string nombre = _etiquetasNombres[etiquetaMasComun.Key];
                AccesoConcedido(nombre);
            }
            else
            {
                AccesoDenegado("Rostro no coincide con ningún registro (o consenso insuficiente)");
            }
        }

        private void AccesoConcedido(string nombre)
        {
            _registro.IntentosFallidos = 0;
            _registro.BloqueadoHasta = null;
            GuardarRegistroIntentos();

            RegistrarEnLog($"ACCESO CONCEDIDO - Usuario: {nombre}");

            DetenerCamaraInterno();

            txtEstado.Text = "Identidad verificada";
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71));
            txtNombreReconocido.Text = nombre;
            bdNombreReconocido.Visibility = Visibility.Visible;

            // Pequeña pausa visual para que el usuario vea el check verde y su nombre
            // antes de pasar al menú principal.
            var temporizadorTransicion = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
            temporizadorTransicion.Tick += (s, e) =>
            {
                temporizadorTransicion.Stop();

                var menu = new MenuPrincipal();
                // Si MenuPrincipal necesita saber quién inició sesión, agrega un
                // constructor que reciba el nombre, ej: new MenuPrincipal(nombre);
                menu.Show();
                this.Close();
            };
            temporizadorTransicion.Start();
        }

        private void AccesoDenegado(string motivo)
        {
            _registro.IntentosFallidos++;
            RegistrarEnLog($"ACCESO DENEGADO - Motivo: {motivo} - Intento N.{_registro.IntentosFallidos}");

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
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(0x7b, 0x1f, 0x1f));

            int intentosRestantes = MAX_INTENTOS_FALLIDOS - _registro.IntentosFallidos;
            string plural = intentosRestantes == 1 ? "intento" : "intentos";

            var respuesta = MessageBox.Show(
                $"Intento fallido. ¿Desea intentar de nuevo? (quedan {intentosRestantes} {plural})",
                "Acceso denegado", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (respuesta == MessageBoxResult.Yes)
            {
                // Vuelve a abrir la cámara automáticamente para el siguiente intento
                // (btnIniciarCamara_Click ya valida que no esté bloqueado y reinicia el
                // periodo de gracia de SEGUNDOS_GRACIA para que la persona se acomode).
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

        private BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
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