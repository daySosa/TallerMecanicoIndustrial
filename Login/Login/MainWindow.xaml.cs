using Login.Clases;
using System.Data;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Login
{
    /// <summary>
    /// Ventana de inicio de sesión de OSM Taller.
    /// Gestiona validación de credenciales, bloqueo por intentos fallidos,
    /// recordatorio de credenciales (cifradas con DPAPI) y feedback visual animado.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constantes y colores

        private static readonly TimeSpan DuracionTransicion = TimeSpan.FromMilliseconds(150);
        private const string MensajeExito = "✅ Ya puedes intentar iniciar sesión.";
        private const string TextoBotonNormal = "Iniciar Sesión";
        private const string TextoBotonCargando = "Verificando...";

        private static readonly Color ColorFoco = (Color)ColorConverter.ConvertFromString("#2563EB");
        private static readonly Color ColorError = (Color)ColorConverter.ConvertFromString("#f44336");
        private static readonly Color ColorExito = (Color)ColorConverter.ConvertFromString("#4CAF50");
        private static readonly Color ColorVacio = Colors.Transparent;

        #endregion

        #region Campos de estado

        private bool _contrasenaVisible;
        private bool _procesandoLogin;
        private string _correoActual = string.Empty;
        private DateTime _fechaDesbloqueo;

        /// <summary>
        /// Cancela cualquier operación asíncrona en curso si la ventana se cierra
        /// antes de que termine (evita excepciones al tocar controles ya destruidos).
        /// </summary>
        private readonly CancellationTokenSource _ctsCierre = new();

        private readonly System.Windows.Threading.DispatcherTimer _timerBloqueo = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly RepositorioSql _repositorio = new();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OSM_remember.dat");

        #endregion

        public MainWindow()
        {
            InitializeComponent();
            _timerBloqueo.Tick += TimerBloqueo_Tick;

            CargarCredencialesRecordadas();

            Closed += MainWindow_Closed;

            _ = VerificarBloqueoAlIniciarAsync();
        }

        #region Ciclo de vida de la ventana

        private void Window_Loaded(object sender, RoutedEventArgs e) =>
            BeginStoryboard((Storyboard)Resources["FadeInVentana"]);

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private async void MainWindow_Closed(object sender, EventArgs e)
        {
            _ctsCierre.Cancel();
            DetenerCuentaRegresiva();

            if (!string.IsNullOrEmpty(_correoActual))
            {
                try
                {
                    await Task.Run(() => _repositorio.ActualizarIntentosFallidos(_correoActual, 0));
                }
                catch (Exception)
                {
                }
            }

            _timerBloqueo.Tick -= TimerBloqueo_Tick;
            _ctsCierre.Dispose();

            if (_repositorio is IDisposable disposable)
                disposable.Dispose();
        }

        private void BtnCerrarApp_Click(object sender, RoutedEventArgs e) =>
            Application.Current.Shutdown();

        #endregion

        #region Foco y animaciones de campos

        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderCorreo, ColorFoco, 2);
        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderCorreo, ColorVacio, 1.5);
        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderContrasena, ColorFoco, 2);
        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderContrasena, ColorVacio, 1.5);

        /// <summary>
        /// Anima el color y grosor del borde de un contenedor de forma fluida,
        /// en lugar de reemplazar el Brush directamente (lo que causaría un "salto" visual).
        /// </summary>
        private static void AnimarBorde(System.Windows.Controls.Border border, Color color, double grosor)
        {
            if (border.BorderBrush is not SolidColorBrush brush || brush.IsFrozen)
            {
                brush = new SolidColorBrush(color);
                border.BorderBrush = brush;
            }

            brush.BeginAnimation(SolidColorBrush.ColorProperty,
                new ColorAnimation(color, DuracionTransicion));

            border.BeginAnimation(System.Windows.Controls.Border.BorderThicknessProperty,
                new ThicknessAnimation(new Thickness(grosor), DuracionTransicion));
        }

        #endregion

        #region Visibilidad de contraseña

        private void BtnVerContrasena_Click(object sender, RoutedEventArgs e)
        {
            _contrasenaVisible = !_contrasenaVisible;

            if (_contrasenaVisible)
            {
                txtContrasenaVisible.Text = txtContrasena.Password;
                txtContrasena.Visibility = Visibility.Collapsed;
                txtContrasenaVisible.Visibility = Visibility.Visible;
                txtContrasenaVisible.Focus();
                txtContrasenaVisible.CaretIndex = txtContrasenaVisible.Text.Length;
                iconOjo.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
            }
            else
            {
                txtContrasena.Password = txtContrasenaVisible.Text;
                txtContrasenaVisible.Visibility = Visibility.Collapsed;
                txtContrasena.Visibility = Visibility.Visible;
                txtContrasena.Focus();
                iconOjo.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
            }
        }

        private string ObtenerContrasena() =>
            _contrasenaVisible ? txtContrasenaVisible.Text : txtContrasena.Password;

        #endregion

        #region Recordar credenciales (DPAPI)

        /// <summary>
        /// Cifra y guarda el correo/contraseña usando DPAPI ligado al usuario de Windows
        /// actual. Solo puede descifrarse en la misma cuenta de Windows y equipo.
        /// </summary>
        private void GuardarCredenciales(string correo, string contrasena)
        {
            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(new { Correo = correo, Contrasena = contrasena });
                byte[] cifrado = ProtectedData.Protect(payload, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(_archivoRecordar, cifrado);
            }
            catch (IOException) { }
            catch (CryptographicException) { }
        }

        private void EliminarCredenciales()
        {
            try
            {
                if (File.Exists(_archivoRecordar))
                    File.Delete(_archivoRecordar);
            }
            catch (IOException) { }
        }

        private void CargarCredencialesRecordadas()
        {
            try
            {
                if (!File.Exists(_archivoRecordar)) return;

                byte[] cifrado = File.ReadAllBytes(_archivoRecordar);
                byte[] payload = ProtectedData.Unprotect(cifrado, null, DataProtectionScope.CurrentUser);
                var datos = JsonSerializer.Deserialize<JsonElement>(payload);

                txtCorreo.Text = datos.GetProperty("Correo").GetString() ?? string.Empty;
                txtContrasena.Password = datos.GetProperty("Contrasena").GetString() ?? string.Empty;
                chkRecordar.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or JsonException or CryptographicException)
            {
            }
        }

        #endregion

        #region Flujo de login

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            txtContador.Visibility = Visibility.Collapsed;

            if (_procesandoLogin) return;

            _correoActual = txtCorreo.Text.Trim();

            string errorCorreo = ValidadorLogin.ValidarCorreo(txtCorreo.Text);
            bool errorEnCorreo = MostrarError(txtErrorCorreo, borderCorreo, errorCorreo);

            string contrasena = ObtenerContrasena();
            string errorContrasena = ValidadorLogin.ValidarContrasena(contrasena);
            bool errorEnContrasena = MostrarError(txtErrorContrasena, borderContrasena, errorContrasena);

            if (errorEnCorreo || errorEnContrasena) return;

            string correo = _correoActual;

            _procesandoLogin = true;
            SetCargando(true);

            try
            {
                if (chkRecordar.IsChecked == true)
                    GuardarCredenciales(correo, contrasena);
                else
                    EliminarCredenciales();

                await IniciarSesionAsync(correo, contrasena);
            }
            finally
            {
                _procesandoLogin = false;
                SetCargando(false);
            }
        }

        /// <summary>
        /// Muestra u oculta el mensaje de error de un campo y anima su borde en consecuencia.
        /// No depende de datos de instancia, por eso se marca como static (CA1822).
        /// </summary>
        /// <returns><c>true</c> si el campo tiene un error.</returns>
        private static bool MostrarError(
            System.Windows.Controls.TextBlock txtError,
            System.Windows.Controls.Border border,
            string mensaje)
        {
            bool hayError = mensaje is not null;
            txtError.Text = mensaje ?? string.Empty;
            txtError.Visibility = hayError ? Visibility.Visible : Visibility.Collapsed;
            AnimarBorde(border, hayError ? ColorError : ColorVacio, 1.5);
            return hayError;
        }

        /// <summary>
        /// Activa/desactiva el estado "cargando" del botón de login sin recrear
        /// el TextBlock interno en cada llamada (evita asignaciones innecesarias).
        /// </summary>
        private void SetCargando(bool cargando)
        {
            btnLogin.IsEnabled = !cargando;
            txtBotonLogin.Text = cargando ? TextoBotonCargando : TextoBotonNormal;
        }

        /// <summary>
        /// Valida las credenciales contra la base de datos, gestiona el bloqueo por
        /// intentos fallidos y, si todo es correcto, abre la ventana de opciones de sesión.
        /// </summary>
        private async Task IniciarSesionAsync(string correo, string contrasena)
        {
            var token = _ctsCierre.Token;

            try
            {
                DateTime? fechaBloqueo = await Task.Run(() => _repositorio.ObtenerFechaBloqueo(correo), token);

                if (fechaBloqueo.HasValue && fechaBloqueo.Value > DateTime.Now)
                {
                    _fechaDesbloqueo = fechaBloqueo.Value;
                    IniciarCuentaRegresiva();
                    return;
                }

                if (fechaBloqueo.HasValue)
                {
                    int intentosActuales = await Task.Run(() => _repositorio.ObtenerIntentosFallidos(correo), token);
                    await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentosActuales, null), token);
                }

                bool valido = await Task.Run(() => _repositorio.ValidarLogin(correo, contrasena), token);

                if (!valido)
                {
                    await RegistrarIntentoFallidoAsync(correo, token);
                    return;
                }

                if (!await UsuarioEstaActivoAsync(correo, token))
                {
                    MostrarError(txtErrorContrasena, borderContrasena,
                        "⚠ Esta cuenta está desactivada. Contacta a un administrador.");
                    AnimarBorde(borderCorreo, ColorError, 1.5);
                    return;
                }

                await Task.Run(() => _repositorio.ActualizarBloqueo(correo, 0, null), token);
                DetenerCuentaRegresiva();

                new OpcionSesion(correo).Show();
                Close();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ Error inesperado: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Incrementa el contador de intentos fallidos y, según la política de bloqueo,
        /// activa la cuenta regresiva o muestra cuántos intentos quedan.
        /// </summary>
        private async Task RegistrarIntentoFallidoAsync(string correo, CancellationToken token)
        {
            int intentos = await Task.Run(() => _repositorio.ObtenerIntentosFallidos(correo), token) + 1;
            int minutos = ValidadorLogin.MinutosDeBloqueo(intentos);

            if (minutos > 0)
            {
                DateTime bloqueoHasta = DateTime.Now.AddMinutes(minutos);
                await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentos, bloqueoHasta), token);
                _fechaDesbloqueo = bloqueoHasta;
                IniciarCuentaRegresiva();
            }
            else
            {
                await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentos, null), token);
                int restantes = ValidadorLogin.IntentosRestantesParaBloqueo(intentos);
                MostrarError(txtErrorContrasena, borderContrasena,
                    $"⚠ Correo o contraseña incorrectos. Te quedan {restantes} intento(s).");
                AnimarBorde(borderCorreo, ColorError, 1.5);
            }
        }

        private async Task<bool> UsuarioEstaActivoAsync(string correo, CancellationToken token)
        {
            DataRow filaUsuario = await Task.Run(() => _repositorio.ObtenerUsuarioPorEmail(correo), token);

            return filaUsuario != null
                   && filaUsuario["Usuario_Activo"] != DBNull.Value
                   && (bool)filaUsuario["Usuario_Activo"];
        }

        private void BtnOlvidoContrasena_Click(object sender, RoutedEventArgs e) =>
            new RecuperarContrasenia(this).IniciarFlujo();

        private void TxtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtErrorCorreo.Visibility == Visibility.Visible)
                txtErrorCorreo.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Bloqueo por intentos fallidos / cuenta regresiva

        private async Task VerificarBloqueoAlIniciarAsync()
        {
            string correo = txtCorreo.Text.Trim();
            if (string.IsNullOrEmpty(correo)) return;

            try
            {
                DateTime? fechaBloqueo = await Task.Run(
                    () => _repositorio.ObtenerFechaBloqueo(correo), _ctsCierre.Token);

                if (fechaBloqueo.HasValue && fechaBloqueo.Value > DateTime.Now)
                {
                    _correoActual = correo;
                    _fechaDesbloqueo = fechaBloqueo.Value;
                    IniciarCuentaRegresiva();
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void IniciarCuentaRegresiva()
        {
            ActualizarContador();
            AnimarBorde(borderCorreo, ColorError, 1.5);
            AnimarBorde(borderContrasena, ColorError, 1.5);

            if (!_timerBloqueo.IsEnabled)
                _timerBloqueo.Start();
        }

        private void TimerBloqueo_Tick(object sender, EventArgs e)
        {
            if ((_fechaDesbloqueo - DateTime.Now).TotalSeconds <= 0)
            {
                DetenerCuentaRegresiva();
                txtContador.Text = MensajeExito;
                txtContador.Foreground = new SolidColorBrush(ColorExito);
                txtContador.Visibility = Visibility.Visible;
                AnimarBorde(borderCorreo, ColorVacio, 1.5);
                AnimarBorde(borderContrasena, ColorVacio, 1.5);
            }
            else
            {
                ActualizarContador();
            }
        }

        private void ActualizarContador()
        {
            TimeSpan restante = _fechaDesbloqueo - DateTime.Now;
            txtContador.Foreground = new SolidColorBrush(ColorError);
            txtContador.Text = $"⛔ Cuenta bloqueada. Espere {(int)restante.TotalMinutes}:{restante.Seconds:D2} min.";
            txtContador.Visibility = Visibility.Visible;
        }

        private void DetenerCuentaRegresiva()
        {
            _timerBloqueo.Stop();
            txtContador.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}