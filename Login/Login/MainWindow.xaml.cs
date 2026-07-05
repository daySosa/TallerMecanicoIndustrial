using Login.Clases;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Login
{
    public partial class MainWindow : Window
    {
        private static readonly TimeSpan DuracionTransicion = TimeSpan.FromMilliseconds(150);
        private const string MensajeExito = "✅ Ya puedes intentar iniciar sesión.";

        private static readonly Color ColorFoco = (Color)ColorConverter.ConvertFromString("#2563EB");
        private static readonly Color ColorError = (Color)ColorConverter.ConvertFromString("#f44336");
        private static readonly Color ColorExito = (Color)ColorConverter.ConvertFromString("#4CAF50");
        private static readonly Color ColorVacio = Colors.Transparent;

        private bool _contrasenaVisible;
        private bool _procesandoLogin;
        private string _correoActual;
        private DateTime _fechaDesbloqueo;

        private readonly System.Windows.Threading.DispatcherTimer _timerBloqueo = new()
        {
            Interval = TimeSpan.FromSeconds(1)
        };

        private readonly RepositorioSql _repositorio = new();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OSM_remember.dat");

        public MainWindow()
        {
            InitializeComponent();
            _timerBloqueo.Tick += TimerBloqueo_Tick;

            CargarCredencialesRecordadas();

            Closed += async (_, _) =>
            {
                DetenerCuentaRegresiva();
                if (!string.IsNullOrEmpty(_correoActual))
                    await Task.Run(() => _repositorio.ActualizarIntentosFallidos(_correoActual, 0));
            };

            _ = VerificarBloqueoAlIniciarAsync();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BeginStoryboard((Storyboard)Resources["FadeInVentana"]);
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderCorreo, ColorFoco, 2);
        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderCorreo, ColorVacio, 1.5);
        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderContrasena, ColorFoco, 2);
        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e) => AnimarBorde(borderContrasena, ColorVacio, 1.5);

        /// <summary>
        /// Anima el color/grosor del borde en vez de reemplazar el Brush,
        /// para que la transición se vea fluida en lugar de "saltar".
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
            try { if (File.Exists(_archivoRecordar)) File.Delete(_archivoRecordar); }
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

                txtCorreo.Text = datos.GetProperty("Correo").GetString() ?? "";
                txtContrasena.Password = datos.GetProperty("Contrasena").GetString() ?? "";
                chkRecordar.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or JsonException or CryptographicException)
            { }
        }

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
                if (chkRecordar.IsChecked == true) GuardarCredenciales(correo, contrasena);
                else EliminarCredenciales();

                await IniciarSesionAsync(correo, contrasena);
            }
            finally
            {
                _procesandoLogin = false;
                SetCargando(false);
            }
        }

        private bool MostrarError(
            System.Windows.Controls.TextBlock txtError,
            System.Windows.Controls.Border border,
            string mensaje)
        {
            bool hayError = mensaje is not null;
            txtError.Text = mensaje ?? string.Empty;
            txtError.Visibility = hayError ? Visibility.Visible : Visibility.Collapsed;
            AnimarBorde(border, hayError ? ColorError : ColorVacio, hayError ? 1.5 : 1.5);
            return hayError;
        }

        private void SetCargando(bool cargando)
        {
            btnLogin.IsEnabled = !cargando;
            btnLogin.Content = cargando ? "Verificando..." : new System.Windows.Controls.TextBlock
            {
                Text = "Iniciar Sesión",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
        }

        private async Task IniciarSesionAsync(string correo, string contrasena)
        {
            try
            {
                DateTime? fechaBloqueo = await Task.Run(() => _repositorio.ObtenerFechaBloqueo(correo));

                if (fechaBloqueo.HasValue && fechaBloqueo.Value > DateTime.Now)
                {
                    _fechaDesbloqueo = fechaBloqueo.Value;
                    IniciarCuentaRegresiva();
                    return;
                }
                else if (fechaBloqueo.HasValue)
                {
                    int intentosActuales = await Task.Run(() => _repositorio.ObtenerIntentosFallidos(correo));
                    await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentosActuales, null));
                }

                bool valido = await Task.Run(() => _repositorio.ValidarLogin(correo, contrasena));

                if (!valido)
                {
                    int intentos = await Task.Run(() => _repositorio.ObtenerIntentosFallidos(correo)) + 1;
                    int minutos = ValidadorLogin.MinutosDeBloqueo(intentos);

                    if (minutos > 0)
                    {
                        DateTime bloqueoHasta = DateTime.Now.AddMinutes(minutos);
                        await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentos, bloqueoHasta));
                        _fechaDesbloqueo = bloqueoHasta;
                        IniciarCuentaRegresiva();
                    }
                    else
                    {
                        await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentos, null));
                        int restantes = ValidadorLogin.IntentosRestantesParaBloqueo(intentos);
                        MostrarError(txtErrorContrasena, borderContrasena,
                            $"⚠ Correo o contraseña incorrectos. Te quedan {restantes} intento(s).");
                        AnimarBorde(borderCorreo, ColorError, 1.5);
                    }
                    return;
                }

                await Task.Run(() => _repositorio.ActualizarBloqueo(correo, 0, null));
                DetenerCuentaRegresiva();

                new OpcionSesion(correo).Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ Error inesperado: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOlvidoContrasena_Click(object sender, RoutedEventArgs e)
        {
            new RecuperarContrasenia(this).IniciarFlujo();
        }

        private void TxtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtErrorCorreo.Visibility == Visibility.Visible)
                txtErrorCorreo.Visibility = Visibility.Collapsed;
        }

        private void BtnCerrarApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async Task VerificarBloqueoAlIniciarAsync()
        {
            string correo = txtCorreo.Text.Trim();
            if (string.IsNullOrEmpty(correo)) return;

            DateTime? fechaBloqueo = await Task.Run(() => _repositorio.ObtenerFechaBloqueo(correo));

            if (fechaBloqueo.HasValue && fechaBloqueo.Value > DateTime.Now)
            {
                _correoActual = correo;
                _fechaDesbloqueo = fechaBloqueo.Value;
                IniciarCuentaRegresiva();
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
    }
}