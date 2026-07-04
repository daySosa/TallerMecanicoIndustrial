using Login.Clases;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Login
{
    public partial class MainWindow : Window
    {
        private bool _contrasenaVisible = false;
        private bool _procesandoLogin = false;
        private System.Windows.Threading.DispatcherTimer _timerBloqueo;
        private DateTime _fechaDesbloqueo;
        private string _correoActual;

        private readonly RepositorioSql _repositorio = new();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        private static readonly SolidColorBrush BrushFocus = Pincel("#2563EB");
        private static readonly SolidColorBrush BrushError = Pincel("#f44336");
        private static readonly SolidColorBrush BrushExito = Pincel("#4CAF50");
        private static readonly SolidColorBrush BrushVacio = PincelTransparente();

        private static SolidColorBrush Pincel(string hex, double? opacidad = null)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            if (opacidad.HasValue) brush.Opacity = opacidad.Value;
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush PincelTransparente()
        {
            var brush = new SolidColorBrush(Colors.Transparent);
            brush.Freeze();
            return brush;
        }

        public MainWindow()
        {
            InitializeComponent();
            CargarCredencialesRecordadas();
            Closed += async (_, _) =>
            {
                DetenerCuentaRegresiva();
                if (!string.IsNullOrEmpty(_correoActual))
                    await Task.Run(() => _repositorio.ActualizarIntentosFallidos(_correoActual, 0));
            };
            _ = VerificarBloqueoAlIniciarAsync();
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderCorreo, true);

        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderCorreo, false);

        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderContrasena, true);

        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderContrasena, false);

        private static void SetBorderFocus(System.Windows.Controls.Border border, bool enfocado)
        {
            border.BorderBrush = enfocado ? BrushFocus : BrushVacio;
            border.BorderThickness = new Thickness(enfocado ? 2 : 1.5);
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
                File.WriteAllText(_archivoRecordar,
                    JsonSerializer.Serialize(new { Correo = correo, Contrasena = contrasena }));
            }
            catch (IOException) { /* No es crítico si falla el guardado local */ }
        }

        private void EliminarCredenciales()
        {
            try { if (File.Exists(_archivoRecordar)) File.Delete(_archivoRecordar); }
            catch (IOException) { /* No es crítico si falla el borrado local */ }
        }

        private void CargarCredencialesRecordadas()
        {
            try
            {
                if (!File.Exists(_archivoRecordar)) return;

                var datos = JsonSerializer.Deserialize<JsonElement>(
                    File.ReadAllText(_archivoRecordar));

                txtCorreo.Text = datos.GetProperty("Correo").GetString() ?? "";
                txtContrasena.Password = datos.GetProperty("Contrasena").GetString() ?? "";
                chkRecordar.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                // Archivo corrupto o inaccesible: se ignora y el usuario escribe sus datos de nuevo.
            }
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

            string correo = txtCorreo.Text.Trim();
            var btnLogin = sender as System.Windows.Controls.Button;

            _procesandoLogin = true;
            SetCargando(true, btnLogin);

            try
            {
                if (chkRecordar.IsChecked == true) GuardarCredenciales(correo, contrasena);
                else EliminarCredenciales();

                await IniciarSesionAsync(correo, contrasena);
            }
            finally
            {
                _procesandoLogin = false;
                SetCargando(false, btnLogin);
            }
        }

        private static bool MostrarError(
            System.Windows.Controls.TextBlock txtError,
            System.Windows.Controls.Border border,
            string mensaje)
        {
            bool hayError = mensaje is not null;
            txtError.Text = mensaje ?? string.Empty;
            txtError.Visibility = hayError ? Visibility.Visible : Visibility.Collapsed;
            border.BorderBrush = hayError ? BrushError : BrushVacio;
            return hayError;
        }

        private static void SetCargando(bool cargando, System.Windows.Controls.Button boton)
        {
            if (boton is null) return;
            boton.IsEnabled = !cargando;
            boton.Content = cargando ? "Verificando..." : "Iniciar Sesión";
        }

        private async Task IniciarSesionAsync(string correo, string contrasena)
        {
            try
            {
                //Verificar bloqueo activo en BD
                DateTime? fechaBloqueo = await Task.Run(() => _repositorio.ObtenerFechaBloqueo(correo));

                if (fechaBloqueo.HasValue && fechaBloqueo.Value > DateTime.Now)
                {
                    _fechaDesbloqueo = fechaBloqueo.Value;
                    IniciarCuentaRegresiva();
                    return;
                }
                else if (fechaBloqueo.HasValue)
                {
                    //Bloqueo expirado, solo limpiar fecha, mantener intentos
                    int intentosActuales = await Task.Run(() => _repositorio.ObtenerIntentosFallidos(correo));
                    await Task.Run(() => _repositorio.ActualizarBloqueo(correo, intentosActuales, null));
                }

                //Validar credenciales
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
                        borderCorreo.BorderBrush = BrushError;
                    }
                    return;
                }

                //Login exitoso: solo se navega a OpcionSesion. El código OTP se genera y
                //envía únicamente cuando el usuario elige explícitamente "Código de
                //verificación" en esa pantalla, no aquí.
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
            var recuperar = new RecuperarContrasenia(this);
            recuperar.IniciarFlujo();
        }

        private void txtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
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
            DetenerCuentaRegresiva();

            txtContador.Foreground = BrushError;
            ActualizarContador();

            borderCorreo.BorderBrush = BrushError;
            borderContrasena.BorderBrush = BrushError;

            _timerBloqueo = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timerBloqueo.Tick += (s, e) =>
            {
                if ((_fechaDesbloqueo - DateTime.Now).TotalSeconds <= 0)
                {
                    DetenerCuentaRegresiva();
                    txtContador.Text = "✅ Ya puedes intentar iniciar sesión.";
                    txtContador.Foreground = BrushExito;
                    txtContador.Visibility = Visibility.Visible;
                    borderCorreo.BorderBrush = BrushVacio;
                    borderContrasena.BorderBrush = BrushVacio;
                }
                else
                {
                    ActualizarContador();
                }
            };
            _timerBloqueo.Start();
        }

        private void ActualizarContador()
        {
            TimeSpan restante = _fechaDesbloqueo - DateTime.Now;
            txtContador.Text = $"⛔ Cuenta bloqueada. Espere {(int)restante.TotalMinutes}:{restante.Seconds:D2} min.";
            txtContador.Visibility = Visibility.Visible;
        }

        private void DetenerCuentaRegresiva()
        {
            _timerBloqueo?.Stop();
            _timerBloqueo = null;
            txtContador.Visibility = Visibility.Collapsed;
        }
    }
}