using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
using Login.Clases;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Login
{
    public partial class ReconocimientoFacial : Window
    {
        private FilterInfoCollection _camarasDisponibles;
        private VideoCaptureDevice _camaraActiva;
        private CascadeClassifier _detectorRostros;

        private readonly RepositorioSql _db = new();
        private readonly MotorReconocimientoFacialLBPH _motor = new();
        private readonly CancellationTokenSource _cts = new();

        private volatile bool _rostroDetectado;
        private volatile bool _accesoOtorgado;
        private volatile bool _procesandoFrame; // evita acumulación de frames (fluidez)
        private bool _camaraEnUso;
        private bool _cargandoPersonas;
        private bool _navegando;

        // Cada persona registrada, junto con el correo de la cuenta a la que
        // pertenece su rostro (columna Usuario_Email en la BD).
        private readonly List<(int Id, string Nombre, string Email, Drawing.Bitmap Foto)> _personas = new();

        /// <summary>
        /// Índice dentro de _personas que corresponde al rostro registrado
        /// para _correoUsuario. -1 si esta cuenta no tiene rostro registrado.
        /// Este es el valor que debe compararse contra el label predicho —
        /// NUNCA _personas.Count (eso es la etiqueta interna de "ruido").
        /// </summary>
        private int _labelEsperado = -1;

        private DateTime _ultimoIntentoRegistrado = DateTime.MinValue;
        private static readonly TimeSpan CooldownIntentoFallido = TimeSpan.FromSeconds(3);

        private readonly string _correoUsuario;

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public ReconocimientoFacial(string correo = "")
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            CargarDetector();

            btnIniciarCamara.IsEnabled = false; // se habilita cuando termine de cargar personas
            _ = CargarPersonasDesdeBDAsync();

            Closed += (_, _) => LiberarRecursos();
        }

        // ════════════════════════════════════════════════════════════
        // DRAG
        // ════════════════════════════════════════════════════════════

        private void Window_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                    DragMove();
            }
            catch (InvalidOperationException)
            {
                // El botón del mouse cambió de estado a mitad del gesto; se ignora.
            }
        }

        // ════════════════════════════════════════════════════════════
        // CARGA INICIAL
        // ════════════════════════════════════════════════════════════

        private void CargarDetector()
        {
            const string ruta = "haarcascade_frontalface_default.xml";
            try
            {
                if (File.Exists(ruta))
                {
                    _detectorRostros = new CascadeClassifier(ruta);
                }
                else
                {
                    MostrarEstado("⚠ No se encontró el archivo de detección facial.", esError: true);
                }
            }
            catch (Exception ex)
            {
                _detectorRostros = null;
                MostrarEstado("⚠ Error al cargar el detector de rostros: " + ex.Message, esError: true);
            }
        }

        private async Task CargarPersonasDesdeBDAsync()
        {
            if (_cargandoPersonas) return;
            _cargandoPersonas = true;
            MostrarEstado("Cargando datos de reconocimiento...");

            try
            {
                var personasCargadas = await Task.Run(CargarPersonasDesdeBDInterno, _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                foreach (var p in _personas) p.Foto?.Dispose();
                _personas.Clear();
                _personas.AddRange(personasCargadas);

                // Identidad esperada para esta sesión: el índice del rostro
                // vinculado al correo con el que se abrió esta ventana.
                _labelEsperado = string.IsNullOrEmpty(_correoUsuario)
                    ? -1
                    : _personas.FindIndex(p =>
                        string.Equals(p.Email, _correoUsuario, StringComparison.OrdinalIgnoreCase));

                if (_personas.Count == 0)
                {
                    MostrarEstado("No hay personas registradas para reconocimiento.", esError: true);
                }
                else
                {
                    _motor.Entrenar(_personas
                        .Select(p => (p.Id, p.Nombre, p.Foto))
                        .ToList());

                    if (_labelEsperado < 0)
                    {
                        MostrarEstado(
                            "⚠ Esta cuenta no tiene un rostro registrado. Usa el código de verificación.",
                            esError: true);
                    }
                    else
                    {
                        MostrarEstado($"{_personas.Count} persona(s) cargada(s). Sistema listo.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ventana cerrada durante la carga; no es un error real.
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MostrarEstado("Error al cargar datos: " + ex.Message, esError: true);
            }
            finally
            {
                _cargandoPersonas = false;
                if (IsLoaded)
                    btnIniciarCamara.IsEnabled = PuedeIniciarCamara();
            }
        }

        private bool PuedeIniciarCamara()
            => _detectorRostros != null && !_cargandoPersonas && _labelEsperado >= 0;

        /// <summary>
        /// Convierte lo leído por RepositorioSql.ObtenerPersonasReconocimiento
        /// (bytes crudos) en Bitmaps independientes. Corre en un hilo de fondo.
        /// Cada fila se procesa de forma aislada: una foto corrupta se descarta
        /// sin tumbar la carga completa.
        /// </summary>
        private List<(int Id, string Nombre, string Email, Drawing.Bitmap Foto)> CargarPersonasDesdeBDInterno()
        {
            var resultado = new List<(int, string, string, Drawing.Bitmap)>();

            foreach (var p in _db.ObtenerPersonasReconocimiento())
            {
                try
                {
                    using var ms = new MemoryStream(p.Foto);
                    using var fotoTemporal = new Drawing.Bitmap(ms);
                    var fotoIndependiente = new Drawing.Bitmap(fotoTemporal);

                    resultado.Add((p.Id, p.Nombre, p.Email, fotoIndependiente));
                }
                catch (Exception exFila)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "Persona de reconocimiento facial descartada por error: " + exFila.Message);
                }
            }

            return resultado;
        }

        // ════════════════════════════════════════════════════════════
        // CÁMARA
        // ════════════════════════════════════════════════════════════

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            if (_camaraEnUso) return; // evita doble inicio por doble clic

            if (_detectorRostros == null)
            {
                MessageBox.Show(
                    "No se puede iniciar: falta el archivo de detección facial.",
                    "Detector no disponible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_labelEsperado < 0)
            {
                MessageBox.Show(
                    "Esta cuenta no tiene un rostro registrado. Usa el código de verificación por correo.",
                    "Sin rostro registrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var (bloqueada, msgBloqueo) = ValidacionesReconocimientoFacial.VerificarBloqueo(_correoUsuario);
            if (bloqueada)
            {
                MessageBox.Show(msgBloqueo, "Cuenta bloqueada", MessageBoxButton.OK, MessageBoxImage.Warning);
                MostrarEstado(msgBloqueo, esError: true);
                return;
            }

            try
            {
                _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                var (ok, msg) = ValidacionesReconocimientoFacial
                    .ValidarCamaraDisponible(_camarasDisponibles.Count);

                if (!ok)
                {
                    MessageBox.Show(msg, "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _accesoOtorgado = false;
                _rostroDetectado = false;
                _procesandoFrame = false;

                bdNombreReconocido.Visibility = Visibility.Collapsed;
                txtNombreReconocido.Text = string.Empty;

                _camaraActiva = new VideoCaptureDevice(_camarasDisponibles[0].MonikerString);
                _camaraActiva.NewFrame += CamaraActiva_NewFrame;
                _camaraActiva.Start();

                _camaraEnUso = true;
                pnlSinCamara.Visibility = Visibility.Collapsed;
                btnIniciarCamara.IsEnabled = false;
                btnDetenerCamara.IsEnabled = true;

                MostrarEstado("Cámara iniciada — buscando rostro...");
            }
            catch (Exception ex)
            {
                _camaraEnUso = false;
                DetenerCamara();
                MessageBox.Show(
                    "No se pudo iniciar la cámara:\n" + ex.Message,
                    "Error de cámara", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            RestablecerVisorSinCamara();
            MostrarEstado("Cámara detenida.");
        }

        private void DetenerCamara()
        {
            try
            {
                if (_camaraActiva is { IsRunning: true })
                {
                    _camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                    _camaraActiva.SignalToStop();
                    _camaraActiva.WaitForStop();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al detener la cámara: " + ex.Message);
            }
            finally
            {
                _camaraActiva = null;
                _camaraEnUso = false;
            }
        }

        /// <summary>
        /// Deja el visor en un estado visual limpio: sin frame congelado detrás
        /// del panel "Cámara no iniciada".
        /// </summary>
        private void RestablecerVisorSinCamara()
        {
            imgCamara.Source = null; // clave: sin esto, el último frame queda pegado detrás del panel
            pnlSinCamara.Visibility = Visibility.Visible;
            btnIniciarCamara.IsEnabled = PuedeIniciarCamara();
            btnDetenerCamara.IsEnabled = false;
            bdNombreReconocido.Visibility = Visibility.Collapsed;
            txtNombreReconocido.Text = string.Empty;
            elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(45, 48, 80));
            _rostroDetectado = false;
        }

        // ════════════════════════════════════════════════════════════
        // PROCESAMIENTO DE FRAMES
        // ════════════════════════════════════════════════════════════

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs args)
        {
            // Evita que se acumulen frames si el procesamiento anterior no ha
            // terminado; esto mantiene el video fluido en vez de irse
            // "quedando atrás" con el tiempo.
            if (_procesandoFrame || _accesoOtorgado) return;
            _procesandoFrame = true;

            try
            {
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

                using (Drawing.Graphics g = Drawing.Graphics.FromImage(frame))
                {
                    foreach (var rostro in rostros)
                    {
                        g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 2), rostro);

                        if (!_motor.Entrenado) continue;

                        using var rostroGris = imgGris.Copy(rostro);
                        var (label, distance) = _motor.Predecir(rostroGris);

                        bool valido = _labelEsperado >= 0
                            && ValidacionesReconocimientoFacial.EsReconocimientoValido(label, distance, _labelEsperado);

                        if (valido)
                        {
                            ValidacionesReconocimientoFacial.LimpiarIntentosFallidos(_correoUsuario);
                            FinalizarAcceso(_personas[_labelEsperado].Nombre);
                            break; // ya se otorgó acceso; no seguir procesando más rostros de este frame
                        }

                        // Solo se considera intento de suplantación cuando el sistema
                        // identificó con CONFIANZA un rostro que no es el esperado
                        // (dentro del umbral). Un simple "no reconocido" no cuenta,
                        // para no bloquear cuentas por ruido o ángulos malos.
                        if (_labelEsperado >= 0
                            && distance <= ValidacionesReconocimientoFacial.UmbralReconocimiento
                            && label != _labelEsperado)
                        {
                            RegistrarIntentoSospechoso(label, distance);
                        }

                        string distanciaTexto = distance.ToString("F1");
                        Dispatcher.Invoke(() => txtNombreReconocido.Text = $"Desconocido  (dist: {distanciaTexto})");
                    }
                }

                if (rostros.Length == 0)
                    Dispatcher.Invoke(() => txtNombreReconocido.Text = string.Empty);

                ActualizarVisor(frame);
            }
            catch (Exception ex)
            {
                // Cualquier excepción aquí corre en el hilo de la cámara (fuera del
                // control de WPF); sin este catch podría tumbar toda la aplicación.
                System.Diagnostics.Debug.WriteLine("Error al procesar frame: " + ex.Message);
            }
            finally
            {
                _procesandoFrame = false;
            }
        }

        private void ActualizarVisor(Drawing.Bitmap frame)
        {
            BitmapImage src;
            try
            {
                src = BitmapToImageSource(frame);
            }
            catch
            {
                return; // si un frame puntual falla al convertirse, simplemente se descarta
            }

            bool rostroDetectado = _rostroDetectado;

            Dispatcher.Invoke(() =>
            {
                if (!IsLoaded) return;

                imgCamara.Source = src;

                MostrarEstado(rostroDetectado
                    ? "Rostro detectado, analizando..."
                    : "Buscando rostro...");

                elipseEstado.Fill = rostroDetectado
                    ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                    : new SolidColorBrush(Color.FromRgb(100, 100, 100));
            });
        }

        // ════════════════════════════════════════════════════════════
        // SEGURIDAD: BLOQUEO Y AUDITORÍA
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Registra un intento de acceso con un rostro distinto al esperado.
        /// Aplica un "cooldown" para no inundar el contador de bloqueo ni la
        /// base de datos mientras la misma cara equivocada sigue en pantalla.
        /// </summary>
        private void RegistrarIntentoSospechoso(int label, double distance)
        {
            if (string.IsNullOrEmpty(_correoUsuario)) return;

            var ahora = DateTime.UtcNow;
            if (ahora - _ultimoIntentoRegistrado < CooldownIntentoFallido) return;
            _ultimoIntentoRegistrado = ahora;

            ValidacionesReconocimientoFacial.RegistrarIntentoFallido(_correoUsuario);
            var (bloqueada, msgBloqueo) = ValidacionesReconocimientoFacial.VerificarBloqueo(_correoUsuario);

            string correo = _correoUsuario;
            _ = Task.Run(() => IntentarRegistrarAuditoriaDb(correo, label, distance));

            if (bloqueada)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!IsLoaded) return;
                    DetenerCamara();
                    RestablecerVisorSinCamara();
                    MostrarEstado(msgBloqueo, esError: true);
                    MessageBox.Show(msgBloqueo, "Cuenta bloqueada", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        /// <summary>
        /// Inserta el intento fallido en la tabla de auditoría a través de
        /// RepositorioSql. Es un registro de "mejor esfuerzo": si falla, no
        /// afecta la decisión de seguridad ya tomada en memoria (el bloqueo
        /// ya se aplicó independientemente de esto).
        /// </summary>
        private void IntentarRegistrarAuditoriaDb(string correo, int label, double distance)
        {
            try
            {
                _db.RegistrarIntentoFallidoReconocimiento(correo, label, distance);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Error al registrar auditoría de intento fallido: " + ex.Message);
            }
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
                if (!IsLoaded) return;
                bdNombreReconocido.Visibility = Visibility.Visible;
                txtNombreReconocido.Text = $"¡Bienvenido, {nombre}!";
                MostrarEstado("Acceso concedido. Entrando en 3 segundos...");
                elipseEstado.Fill = new SolidColorBrush(Color.FromRgb(46, 204, 113));
            });

            bool sesionValida = false;
            try
            {
                sesionValida = await Task.Run(() => IntentarIniciarSesion(_correoUsuario), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // ventana cerrada durante el proceso
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al iniciar sesión tras reconocimiento: " + ex.Message);
            }

            if (!sesionValida)
            {
                Dispatcher.Invoke(() =>
                {
                    if (!IsLoaded) return;
                    _accesoOtorgado = false;
                    MostrarEstado("⚠ Reconocido, pero no se pudo cargar tu sesión. Intenta de nuevo.", esError: true);
                    bdNombreReconocido.Visibility = Visibility.Collapsed;
                });
                return;
            }

            try
            {
                await Task.Delay(3000, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                return; // ventana cerrada durante la cuenta regresiva
            }

            Dispatcher.Invoke(() => AbrirDashboardYCerrar());
        }

        /// <summary>
        /// Consulta la BD e inicia sesión de forma segura. Devuelve false si no se
        /// pudo establecer una sesión válida, en cuyo caso NO se debe continuar
        /// hacia el dashboard.
        /// </summary>
        private bool IntentarIniciarSesion(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;

            DataRow datosUsuario = _db.ObtenerUsuarioPorEmail(correo);
            if (datosUsuario is null) return false;

            string email = ObtenerValorTexto(datosUsuario, "Usuario_Email");
            string nombre = ObtenerValorTexto(datosUsuario, "Usuario_Nombre");
            string apellido = ObtenerValorTexto(datosUsuario, "Usuario_Apellido");
            string rol = ObtenerValorTexto(datosUsuario, "Usuario_Rol");

            if (string.IsNullOrWhiteSpace(email)) return false;

            SesionActual.IniciarSesion(email, nombre, apellido, rol);
            return true;
        }

        private static string ObtenerValorTexto(DataRow fila, string columna)
        {
            if (!fila.Table.Columns.Contains(columna)) return string.Empty;
            object valor = fila[columna];
            return (valor == null || valor == DBNull.Value) ? string.Empty : valor.ToString() ?? string.Empty;
        }

        private void AbrirDashboardYCerrar()
        {
            if (_navegando) return;
            _navegando = true;

            DetenerCamara();

            Dasboard_Prueba.MenuPrincipal dashboard = null;
            try
            {
                dashboard = new Dasboard_Prueba.MenuPrincipal();
                dashboard.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                try { dashboard?.Close(); } catch { /* ya inválido, no hay más que hacer */ }

                _navegando = false;
                _accesoOtorgado = false;
                MostrarEstado("⚠ No se pudo abrir el panel principal: " + ex.Message, esError: true);
            }
        }

        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ════════════════════════════════════════════════════════════

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            if (_navegando) return;
            _navegando = true;

            DetenerCamara();

            OpcionSesion anterior = null;
            try
            {
                anterior = new OpcionSesion(_correoUsuario);
                anterior.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                try { anterior?.Close(); } catch { /* ya inválido, no hay más que hacer */ }

                _navegando = false;
                MessageBox.Show("No se pudo regresar a la pantalla anterior:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════

        private void MostrarEstado(string mensaje, bool esError = false)
        {
            void Aplicar()
            {
                if (!IsLoaded) return;
                txtEstado.Text = mensaje;
                txtEstado.Foreground = esError
                    ? new SolidColorBrush(Color.FromRgb(231, 76, 60))
                    : new SolidColorBrush(Color.FromRgb(170, 170, 170));
            }

            if (Dispatcher.CheckAccess()) Aplicar();
            else Dispatcher.Invoke(Aplicar);
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
            img.Freeze(); // permite usarlo desde el hilo de fondo sin violar el hilo de UI
            return img;
        }

        private void LiberarRecursos()
        {
            try
            {
                _cts.Cancel();
                DetenerCamara();
                _detectorRostros?.Dispose();
                _motor.Dispose();
                foreach (var p in _personas) p.Foto?.Dispose();
                _cts.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al liberar recursos: " + ex.Message);
            }
        }

        // ════════════════════════════════════════════════════════════
        // CLEANUP
        // ════════════════════════════════════════════════════════════

        protected override void OnClosed(EventArgs e)
        {
            LiberarRecursos();
            base.OnClosed(e);
        }
    }
}