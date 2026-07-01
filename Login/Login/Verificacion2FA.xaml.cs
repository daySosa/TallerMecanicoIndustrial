using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
        private readonly string _correoUsuario;
        private readonly clsConsultasBD _db = new();
        private readonly DispatcherTimer _timer = new();
        private readonly TextBox[] _cajas;
        private readonly CancellationTokenSource _cts = new();

        private int _segundos = 300;
        private bool _navegando;
        private bool _verificando;

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            _cajas = [d1, d2, d3, d4, d5, d6];
            ConfigurarCajas();
            IniciarTimer();

            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MostrarError("⚠ No hay un correo válido asociado a esta verificación.");
                btnVerificar.IsEnabled = false;
                btnReenviar.IsEnabled = false;
            }

            Closed += (_, _) => LiberarRecursos();
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
            catch (InvalidOperationException)
            { }
        }

        private void LiberarRecursos()
        {
            try
            {
                _timer.Stop();
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            { }
        }

        private void ConfigurarCajas()
        {
            foreach (var caja in _cajas)
            {
                caja.PreviewTextInput += Caja_PreviewTextInput;
                caja.TextChanged += Caja_TextChanged;
                caja.KeyDown += Caja_KeyDown;
                caja.GotFocus += (s, e) => ((TextBox)s).SelectAll();
            }
        }

        private void Caja_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = e.Text.Length == 0 || !char.IsDigit(e.Text, 0);
        }

        private void Caja_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarError();

            var caja = (TextBox)sender;
            int index = Array.IndexOf(_cajas, caja);
            if (index < 0) return;

            if (caja.Text.Length == 1 && index < _cajas.Length - 1)
                _cajas[index + 1].Focus();
        }

        private void Caja_KeyDown(object sender, KeyEventArgs e)
        {
            var caja = (TextBox)sender;
            int index = Array.IndexOf(_cajas, caja);
            if (index < 0) return;

            if (e.Key == Key.Back && string.IsNullOrEmpty(caja.Text) && index > 0)
            {
                _cajas[index - 1].Focus();
                _cajas[index - 1].Clear();
                e.Handled = true;
            }
        }

        private string ObtenerCodigo()
            => string.Concat(_cajas.Select(c => c.Text));

        private void LimpiarCajas()
        {
            foreach (var c in _cajas) c.Clear();
            _cajas[0].Focus();
        }

        private void IniciarTimer()
        {
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _segundos--;
            int min = Math.Max(_segundos, 0) / 60;
            int seg = Math.Max(_segundos, 0) % 60;
            runTimer.Text = $"{min:D2}:{seg:D2}";

            if (_segundos <= 0)
            {
                _timer.Stop();
                runTimer.Text = "00:00";
                btnVerificar.IsEnabled = false;
                MostrarError("⚠ El código ha expirado. Reenvíalo para continuar.");
            }
        }

        private async void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            if (_verificando) return;
            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MostrarError("⚠ No hay un correo válido para verificar.");
                return;
            }

            string codigo = ObtenerCodigo();

            var (esValido, mensaje) = clsValidacionCodigo2FA.ValidarCodigo(codigo);
            if (!esValido)
            {
                MostrarError(mensaje);
                return;
            }

            _verificando = true;
            btnVerificar.IsEnabled = false;
            OcultarError();

            try
            {
                bool codigoCorrecto = await Task.Run(
                    () => _db.ValidarCodigoOTP(_correoUsuario, codigo), _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (!codigoCorrecto)
                {
                    MostrarError("⚠ Código incorrecto o expirado. Intenta nuevamente.");
                    LimpiarCajas();
                    return;
                }

                DataRow datosUsuario = await Task.Run(
                    () => _db.ObtenerUsuarioPorEmail(_correoUsuario), _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (!IntentarIniciarSesion(datosUsuario))
                {
                    MostrarError("⚠ No se pudo cargar la información de tu cuenta. Intenta de nuevo o contacta soporte.");
                    return;
                }

                _timer.Stop();
                AbrirDashboardYCerrar();
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MostrarError("⚠ Error al validar el código: " + ex.Message);
            }
            finally
            {
                _verificando = false;
                if (IsLoaded && _segundos > 0)
                    btnVerificar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Extrae los datos del usuario desde el DataRow de forma segura (sin acceso
        /// directo a columnas que puedan no existir o venir en DBNull) e inicia sesión.
        /// Devuelve false si los datos son insuficientes; en ese caso NO se debe
        /// continuar hacia el dashboard.
        /// </summary>
        private static bool IntentarIniciarSesion(DataRow datosUsuario)
        {
            if (datosUsuario is null) return false;

            string email = ObtenerValorTexto(datosUsuario, "Usuario_Email");
            string nombre = ObtenerValorTexto(datosUsuario, "Usuario_Nombre");
            string apellido = ObtenerValorTexto(datosUsuario, "Usuario_Apellido");
            string rol = ObtenerValorTexto(datosUsuario, "Usuario_Rol");

            if (string.IsNullOrWhiteSpace(email))
                return false;

            clsSesion.IniciarSesion(email, nombre, apellido, rol);
            return true;
        }

        /// <summary>
        /// Lee una columna de un DataRow protegiéndose de tres fallos comunes:
        /// que la columna no exista, que el valor sea DBNull, o que sea null.
        /// </summary>
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

            Dasboard_Prueba.MenuPrincipal dashboard = null;
            try
            {
                dashboard = new Dasboard_Prueba.MenuPrincipal();
                dashboard.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                try { dashboard?.Close(); } catch { }

                _navegando = false;
                MostrarError("⚠ No se pudo abrir el panel principal: " + ex.Message);
            }
        }

        private async void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_correoUsuario))
            {
                MessageBox.Show("No hay un correo válido para reenviar el código.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnReenviar.IsEnabled = false;

            try
            {
                bool enviado = await Task.Run(() =>
                {
                    string codigo = _db.GenerarCodigoOTP(_correoUsuario);
                    return _db.EnviarCorreoOTP(_correoUsuario, codigo);
                }, _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (enviado)
                {
                    _timer.Stop();
                    _segundos = 300;
                    runTimer.Text = "05:00";
                    btnVerificar.IsEnabled = true;
                    _timer.Start();

                    LimpiarCajas();
                    OcultarError();

                    MessageBox.Show("✅ Código reenviado a tu correo.", "Código enviado",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MostrarError("⚠ No se pudo reenviar el código. Intenta de nuevo.");
                }
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MessageBox.Show("⚠ No se pudo reenviar el código: " + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (IsLoaded)
                    btnReenviar.IsEnabled = true;
            }
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            if (_navegando) return;
            _navegando = true;

            _timer.Stop();

            if (!string.IsNullOrEmpty(_correoUsuario))
            {
                var db = _db;
                string correo = _correoUsuario;
                _ = Task.Run(() =>
                {
                    try
                    {
                        string nuevoCodigo = db.GenerarCodigoOTP(correo);
                        db.EnviarCorreoOTP(correo, nuevoCodigo);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "Error al regenerar código OTP al regresar: " + ex.Message);
                    }
                }, _cts.Token);
            }

            OpcionSesion anterior = null;
            try
            {
                anterior = new OpcionSesion(_correoUsuario);
                anterior.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                try { anterior?.Close(); } catch { }

                _navegando = false;
                MessageBox.Show("No se pudo regresar a la pantalla anterior:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarError(string msg)
        {
            txtErrorCodigo.Text = msg;
            txtErrorCodigo.Visibility = Visibility.Visible;
        }

        private void OcultarError()
            => txtErrorCodigo.Visibility = Visibility.Collapsed;

        protected override void OnClosed(EventArgs e)
        {
            LiberarRecursos();
            base.OnClosed(e);
        }
    }
}