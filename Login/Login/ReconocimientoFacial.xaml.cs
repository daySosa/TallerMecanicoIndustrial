using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Login
{
    /// <summary>
    /// Ventana de reconocimiento facial.
    /// Gestiona captura de video, detección de rostros, autenticación biométrica
    /// vinculada al usuario en sesión, registro de nuevas personas y bloqueo
    /// temporal por intentos fallidos.
    /// Toda la comunicación con la base de datos se realiza mediante
    /// procedimientos almacenados.
    /// </summary>
    public partial class ReconocimientoFacial : Window
    {
        // ── Cámara y detección ────────────────────────────────────────────────────

        /// <summary>Cámaras disponibles detectadas en el sistema.</summary>
        private FilterInfoCollection? _camarasDisponibles;

        /// <summary>Cámara actualmente activa.</summary>
        private VideoCaptureDevice? _camaraActiva;

        /// <summary>Clasificador Haar para detección de rostros frontales.</summary>
        private CascadeClassifier? _detectorRostros;

        // ── Reconocimiento ────────────────────────────────────────────────────────

        /// <summary>Servicio LBPH de entrenamiento y predicción.</summary>
        private readonly clsServicioReconocimiento _servicioReconocimiento = new();

        /// <summary>
        /// Personas cargadas desde BD.
        /// Cada entrada contiene (Id de fila, Nombre, Foto bitmap).
        /// </summary>
        private readonly List<(int Id, string Nombre, Drawing.Bitmap Foto)> _personas = new();

        /// <summary>
        /// Índice dentro de <see cref="_personas"/> correspondiente al usuario en sesión.
        /// -1 mientras no esté vinculado o el usuario no tenga rostro registrado.
        /// </summary>
        private int _labelUsuarioSesion = -1;

        /// <summary>Nombre del usuario en sesión resuelto desde la base de datos.</summary>
        private string _nombreUsuarioSesion = string.Empty;

        // ── Estado de la UI ───────────────────────────────────────────────────────

        /// <summary>Indica si hay un rostro detectado en el frame actual.</summary>
        private bool _rostroDetectado = false;

        /// <summary>Indica si el acceso fue concedido (evita procesamiento adicional).</summary>
        private bool _accesoOtorgado = false;

        /// <summary>Indica si la ventana está en modo Registro (vs modo Login).</summary>
        private bool _esModoRegistro = false;

        // ── Registro ──────────────────────────────────────────────────────────────

        /// <summary>Fotografía capturada para registrar una nueva persona.</summary>
        private Drawing.Bitmap? _fotoCapturada = null;

        // ── Sesión ────────────────────────────────────────────────────────────────

        /// <summary>Correo del usuario actualmente en sesión.</summary>
        private readonly string _correoUsuario;

        // ═══════════════════════════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa la ventana, suscribe el contador de caracteres,
        /// carga el detector Haar y precarga las personas desde BD.
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Inicialización
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Carga el modelo Haar desde disco.
        /// Muestra error en la barra de estado si el archivo no existe.
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
        /// Llama a <c>PA_RF_ObtenerTodasLasPersonas</c>, entrena el modelo LBPH
        /// y vincula el índice del usuario en sesión.
        /// </summary>
        private void CargarPersonasDesdeBD()
        {
            try
            {
                foreach (var p in _personas) p.Foto?.Dispose();
                _personas.Clear();
                _labelUsuarioSesion = -1;

                clsConexion conexion = new();
                conexion.Abrir();

                SqlCommand cmd = new("PA_RF_ObtenerTodasLasPersonas", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using SqlDataReader reader = cmd.ExecuteReader();
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

                VincularUsuarioSesion();

                MostrarEstado($"{_personas.Count} persona(s) cargada(s). Sistema listo.");
            }
            catch (Exception ex)
            {
                MostrarEstado("Error al cargar datos: " + ex.Message, esError: true);
            }
        }

        /// <summary>
        /// Llama a <c>PA_RF_ObtenerNombrePorCorreo</c> para resolver el nombre
        /// del usuario en sesión y buscarlo dentro de <see cref="_personas"/>.
        /// Sin este vínculo el sistema denegará todo intento de login.
        /// </summary>
        private void VincularUsuarioSesion()
        {
            if (string.IsNullOrEmpty(_correoUsuario)) return;

            try
            {
                clsConexion conexion = new();
                conexion.Abrir();

                SqlCommand cmd = new("PA_RF_ObtenerNombrePorCorreo", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Correo", _correoUsuario);

                object? resultado = cmd.ExecuteScalar();
                conexion.Cerrar();

                if (resultado is string nombreBD)
                {
                    _nombreUsuarioSesion = nombreBD;
                    _labelUsuarioSesion = _personas.FindIndex(p =>
                        string.Equals(p.Nombre, nombreBD, StringComparison.OrdinalIgnoreCase));
                }

                if (_labelUsuarioSesion < 0)
                    MostrarEstado(
                        "⚠ El usuario en sesión no tiene rostro registrado. " +
                        "Solo podrás usar el modo Registro.", esError: true);
                else
                    MostrarEstado(
                        $"Usuario vinculado: {_nombreUsuarioSesion}. Listo para autenticar.");
            }
            catch (Exception ex)
            {
                MostrarEstado("Error al vincular usuario: " + ex.Message, esError: true);
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // Control de cámara
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Inicia la primera cámara disponible y activa los botones de modo.
        /// </summary>
        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            var (ok, msg) =
                clsValidacionReconocimiento.ValidarCamaraDisponible(_camarasDisponibles.Count);
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
        /// Detiene la cámara y restablece el estado de la UI.
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
        /// Señala a la cámara que deje de capturar y libera el objeto.
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Procesamiento de frames
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Callback de AForge para cada frame capturado.
        /// Detecta rostros y, en modo Login, valida que el rostro pertenezca
        /// exclusivamente al usuario en sesión.
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
                var color = _esModoRegistro
                    ? Drawing.Color.Orange
                    : Drawing.Color.LimeGreen;
                g.DrawRectangle(new Drawing.Pen(color, 2), rostro);

                if (!_esModoRegistro && _servicioReconocimiento.Entrenado)
                    ProcesarReconocimiento(imgGris, rostro);
            }

            if (rostros.Length == 0)
                Dispatcher.Invoke(() => txtNombreReconocido.Text = "");

            ActualizarVisor(frame);
        }

        /// <summary>
        /// Ejecuta la predicción LBPH y aplica las validaciones de identidad,
        /// bloqueo y registro de intentos fallidos.
        /// </summary>
        private void ProcesarReconocimiento(Image<Gray, byte> imgGris, Drawing.Rectangle rostro)
        {
            // 1. Verificar bloqueo antes de analizar
            var (bloqueada, msgBloqueo) =
                clsValidacionReconocimiento.VerificarBloqueo(_correoUsuario);
            if (bloqueada)
            {
                Dispatcher.Invoke(() =>
                {
                    txtNombreReconocido.Text = msgBloqueo;
                    elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(231, 76, 60));
                });
                return;
            }

            // 2. Predicción
            using var rostroGris = imgGris.Copy(rostro);
            var (label, distance) = _servicioReconocimiento.Predecir(rostroGris);

            // 3. Validación: distancia aceptable Y label == usuario en sesión
            bool valido = _labelUsuarioSesion >= 0 &&
                          clsValidacionReconocimiento.EsReconocimientoValido(
                              label, distance, _labelUsuarioSesion);

            if (valido)
            {
                clsValidacionReconocimiento.LimpiarIntentosFallidos(_correoUsuario);
                FinalizarAcceso(_nombreUsuarioSesion);
                return;
            }

            // 4. ¿Fue OTRA persona conocida (suplantación) o simplemente no reconocido?
            bool otraPersonaDetectada =
                label >= 0 &&
                label < _personas.Count &&
                label != _labelUsuarioSesion &&
                distance <= clsValidacionReconocimiento.UmbralReconocimiento;

            if (otraPersonaDetectada)
            {
                clsValidacionReconocimiento.RegistrarIntentoFallido(_correoUsuario);
                int intentos =
                    clsValidacionReconocimiento.ObtenerIntentosFallidos(_correoUsuario);

                RegistrarIntentoEnBD(_correoUsuario, label, distance);

                Dispatcher.Invoke(() =>
                    txtNombreReconocido.Text =
                        $"⛔ Acceso denegado — persona no autorizada " +
                        $"(intento {intentos} / 5)");
            }
            else
            {
                Dispatcher.Invoke(() =>
                    txtNombreReconocido.Text = $"Analizando... (dist: {distance:F1})");
            }
        }

        /// <summary>
        /// Actualiza el visor con el frame procesado y refleja el estado
        /// de detección en la barra de estado y la elipse indicadora.
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Acceso concedido
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Concede acceso: actualiza la UI, espera 5 s, detiene la cámara
        /// y abre el menú principal.
        /// </summary>
        private async void FinalizarAcceso(string nombre)
        {
            if (_accesoOtorgado) return;
            _accesoOtorgado = true;

            Dispatcher.Invoke(() =>
            {
                txtNombreReconocido.Text = $"✔ ¡Bienvenido, {nombre}! Acceso concedido.";
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Modos de operación
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>Activa el modo Registro: el reconocimiento queda deshabilitado.</summary>
        private void btnModoRegistro_Click(object sender, RoutedEventArgs e)
        {
            _esModoRegistro = true;
            txtNombreReconocido.Text = "";
            btnModoRegistro.IsEnabled = false;
            btnModoLogin.IsEnabled = true;
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(230, 126, 34));
            MostrarEstado("Modo Registro activo — el reconocimiento está desactivado.");
        }

        /// <summary>
        /// Activa el modo Login: habilita el reconocimiento y restablece el estado.
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Captura y registro de personas
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Captura el rostro del frame actual para su posterior registro.
        /// Solo funciona en modo Registro y cuando hay un rostro detectado.
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
        /// Valida el formulario y llama a <c>PA_RF_InsertarPersona</c>
        /// para guardar la nueva persona en BD.
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

            var (fotoOk, msgFoto) =
                clsValidacionReconocimiento.ValidarFotoCapturada(_fotoCapturada);
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
        /// Llama a <c>PA_RF_InsertarPersona</c> para insertar nombre y foto PNG en BD.
        /// </summary>
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

                SqlCommand cmd = new("PA_RF_InsertarPersona", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Foto", bytes);
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
        /// Llama a <c>PA_RF_RegistrarIntentoFallido</c> para auditar cada intento
        /// con persona no autorizada. Los errores se silencian para no interrumpir
        /// el flujo de autenticación.
        /// </summary>
        private void RegistrarIntentoEnBD(string correo, int labelDetectado, double distancia)
        {
            try
            {
                clsConexion conexion = new();
                conexion.Abrir();

                SqlCommand cmd = new("PA_RF_RegistrarIntentoFallido", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@LabelDetectado", labelDetectado);
                cmd.Parameters.AddWithValue("@Distancia", distancia);
                cmd.ExecuteNonQuery();

                conexion.Cerrar();
            }
            catch
            {
                // El log no debe interrumpir el flujo de autenticación
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // Navegación
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>Detiene la cámara y regresa a la ventana de opciones de sesión.</summary>
        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            new OpcionSesion(_correoUsuario).Show();
            this.Close();
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // Utilidades de imagen
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el frame actual mostrado en <c>imgCamara</c> como Bitmap GDI+.
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

            src.CopyPixels(
                Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

        /// <summary>
        /// Convierte un <see cref="Drawing.Bitmap"/> GDI+ a <see cref="BitmapImage"/> WPF.
        /// El resultado se congela para usarse desde hilos secundarios.
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

        // ═══════════════════════════════════════════════════════════════════════════
        // Barra de estado
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>Muestra un mensaje en la barra de estado inferior.</summary>
        /// <param name="esError">Si true, el texto se muestra en rojo.</param>
        private void MostrarEstado(string mensaje, bool esError = false)
        {
            txtEstado.Text = mensaje;
            txtEstado.Foreground = esError
                ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                : new SolidColorBrush(Color.FromRgb(170, 170, 170));
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // Ciclo de vida
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>Libera todos los recursos gestionados al cerrar la ventana.</summary>
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