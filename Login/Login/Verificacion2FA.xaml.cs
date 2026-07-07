#nullable enable
using Login.Clases;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
        #region Constantes y estado

        private const int MaxIntentosFallidos = 5;
        private const int SegundosIniciales = 300;

        private readonly string _correoUsuario;
        private readonly RepositorioSql _db = new();
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
        private readonly TextBox[] _cajas;
        private readonly CancellationTokenSource _cts = new();

        private int _segundos = SegundosIniciales;
        private int _intentosFallidos;
        private bool _navegando;
        private bool _verificando;
        private bool _disposed;
        private bool _codigoInicialEnviado;

        #endregion

        #region Constructor y ciclo de vida de la ventana

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            _cajas = [d1, d2, d3, d4, d5, d6];
            ConfigurarCajas();
            HabilitarCajas(false);

            _timer.Tick += Timer_Tick;

            if (_correoUsuario.Length == 0)
            {
                barraEnvioInicial.Visibility = Visibility.Collapsed;
                txtEstadoEnvioInicial.Text = "⚠ No hay un correo válido asociado a esta verificación.";
                btnReenviar.IsEnabled = false;
            }
        }

        /// <summary>
        /// Al cargar la ventana: aplica un fade-in suave y, si hay un correo
        /// válido, dispara el envío del código inicial. Este envío es una
        /// continuación directa de la acción explícita del usuario (haber
        /// elegido "Código de verificación" en la pantalla anterior), por lo
        /// que dispararlo aquí SÍ es correcto — no es un envío no solicitado.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, fadeIn);

            if (_codigoInicialEnviado || _correoUsuario.Length == 0) return;
            _codigoInicialEnviado = true;

            await EnviarCodigoInicialAsync();
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            try { DragMove(); }
            catch (InvalidOperationException) { }
        }

        /// <summary>
        /// Libera recursos una única vez. RepositorioSql no implementa IDisposable
        /// (cada consulta abre/cierra su propia conexión internamente), por lo que
        /// aquí solo se libera el CTS y el timer.
        /// </summary>
        private void LiberarRecursos()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _timer.Stop();
                _cts.Cancel();
                _cts.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al liberar recursos: " + ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            LiberarRecursos();
            base.OnClosed(e);
        }

        #endregion

        #region Envío del código OTP (inicial y reenvío)

        /// <summary>
        /// Genera un nuevo código OTP y lo envía por correo, en un hilo de fondo
        /// y respetando la cancelación de la ventana. Único punto donde se llama
        /// a <c>GenerarCodigoOTP</c> + <c>EnviarCorreoOTP</c>; antes esta pareja de
        /// llamadas estaba duplicada en el envío inicial y en el reenvío manual.
        /// </summary>
        private Task<bool> GenerarYEnviarCodigoOtpAsync() =>
            Task.Run(() =>
            {
                string codigo = _db.GenerarCodigoOTP(_correoUsuario);
                return _db.EnviarCorreoOTP(_correoUsuario, codigo);
            }, _cts.Token);

        private async Task EnviarCodigoInicialAsync()
        {
            barraEnvioInicial.Visibility = Visibility.Visible;
            txtEstadoEnvioInicial.Text = "Enviando código a tu correo...";
            btnReintentarEnvio.Visibility = Visibility.Collapsed;
            panelEnvioInicial.Visibility = Visibility.Visible;
            panelContenidoCodigo.Visibility = Visibility.Collapsed;

            try
            {
                bool enviado = await GenerarYEnviarCodigoOtpAsync();

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (enviado)
                    MostrarPanelListoParaIngresarCodigo();
                else
                    MostrarErrorEnvioInicial("⚠ No se pudo enviar el código. Intenta de nuevo.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MostrarErrorEnvioInicial("⚠ No se pudo enviar el código: " + ex.Message);
            }
        }

        private void MostrarPanelListoParaIngresarCodigo()
        {
            panelEnvioInicial.Visibility = Visibility.Collapsed;
            panelContenidoCodigo.Visibility = Visibility.Visible;

            HabilitarCajas(true);
            btnVerificar.IsEnabled = true;
            d1.Focus();

            _segundos = SegundosIniciales;
            runTimer.Text = "05:00";
            _timer.Start();
        }

        private void MostrarErrorEnvioInicial(string mensaje)
        {
            barraEnvioInicial.Visibility = Visibility.Collapsed;
            txtEstadoEnvioInicial.Text = mensaje;
            btnReintentarEnvio.Visibility = Visibility.Visible;
        }

        private async void BtnReintentarEnvio_Click(object sender, RoutedEventArgs e)
        {
            btnReintentarEnvio.Visibility = Visibility.Collapsed;
            await EnviarCodigoInicialAsync();
        }

        /// <summary>
        /// Reenvío explícito solicitado por el usuario (botón "Reenviar ahora").
        /// Independiente del envío inicial automático de EnviarCodigoInicialAsync.
        /// </summary>
        private async void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            if (_correoUsuario.Length == 0)
            {
                MessageBox.Show("No hay un correo válido para reenviar el código.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnReenviar.IsEnabled = false;

            try
            {
                bool enviado = await GenerarYEnviarCodigoOtpAsync();

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (enviado)
                {
                    _timer.Stop();
                    _segundos = SegundosIniciales;
                    _intentosFallidos = 0;
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
            {
            }
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

        #endregion

        #region Cajas de dígitos

        private void ConfigurarCajas()
        {
            foreach (var caja in _cajas)
            {
                caja.PreviewTextInput += Caja_PreviewTextInput;
                caja.TextChanged += Caja_TextChanged;
                caja.KeyDown += Caja_KeyDown;
                caja.GotFocus += Caja_GotFocus;
                DataObject.AddPastingHandler(caja, Caja_Pasting);
            }
        }

        private static void Caja_GotFocus(object sender, RoutedEventArgs e)
            => ((TextBox)sender).SelectAll();

        /// <summary>
        /// Valida TODO el texto entrante (no solo el primer carácter), ya que este
        /// evento también se dispara al pegar texto completo, no solo al teclear.
        /// </summary>
        private void Caja_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = string.IsNullOrEmpty(e.Text) || !SonSoloDigitosAscii(e.Text);
        }

        /// <summary>
        /// Maneja el pegado (Ctrl+V) de forma explícita: si el contenido pegado
        /// son exactamente 6 dígitos, distribuye el código en todas las cajas.
        /// Si no cumple ese formato, se descarta para no romper la validación.
        /// </summary>
        private void Caja_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text) &&
                e.DataObject.GetData(DataFormats.Text) is string texto)
            {
                texto = texto.Trim();
                if (texto.Length == Validador2FA.LongitudCodigo && SonSoloDigitosAscii(texto))
                    DistribuirCodigoEnCajas(texto);
            }

            e.CancelCommand();
        }

        private void DistribuirCodigoEnCajas(string codigo)
        {
            for (int i = 0; i < _cajas.Length && i < codigo.Length; i++)
                _cajas[i].Text = codigo[i].ToString();

            _cajas[^1].Focus();
            OcultarError();
        }

        private static bool SonSoloDigitosAscii(string texto)
        {
            foreach (char c in texto)
            {
                if (!char.IsAsciiDigit(c)) return false;
            }
            return true;
        }

        private void Caja_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarError();

            var caja = (TextBox)sender;

            if (caja.Text.Length > 1)
            {
                caja.Text = caja.Text[..1];
                caja.CaretIndex = caja.Text.Length;
                return;
            }

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

            if (e.Key == Key.Back && caja.Text.Length == 0 && index > 0)
            {
                _cajas[index - 1].Focus();
                _cajas[index - 1].Clear();
                e.Handled = true;
            }
        }

        private string ObtenerCodigo()
        {
            var sb = new StringBuilder(_cajas.Length);
            foreach (var c in _cajas)
                sb.Append(c.Text);
            return sb.ToString();
        }

        private void LimpiarCajas()
        {
            foreach (var c in _cajas) c.Clear();
            _cajas[0].Focus();
        }

        private void HabilitarCajas(bool habilitar)
        {
            foreach (var c in _cajas) c.IsEnabled = habilitar;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _segundos--;
            int restante = Math.Max(_segundos, 0);
            runTimer.Text = $"{restante / 60:D2}:{restante % 60:D2}";

            if (_segundos <= 0)
            {
                _timer.Stop();
                btnVerificar.IsEnabled = false;
                MostrarError("⚠ El código ha expirado. Reenvíalo para continuar.");
            }
        }

        #endregion

        #region Verificación del código

        private async void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            if (_verificando) return;

            if (_correoUsuario.Length == 0)
            {
                MostrarError("⚠ No hay un correo válido para verificar.");
                return;
            }

            string codigo = ObtenerCodigo();
            var (esValido, mensaje) = Validador2FA.ValidarCodigo(codigo);
            if (!esValido)
            {
                MostrarError(mensaje);
                return;
            }

            _verificando = true;
            btnVerificar.IsEnabled = false;
            HabilitarCajas(false);
            OcultarError();

            try
            {
                bool codigoCorrecto = await Task.Run(
                    () => _db.ValidarCodigoOTP(_correoUsuario, codigo), _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (!codigoCorrecto)
                {
                    RegistrarIntentoFallido();
                    return;
                }

                _intentosFallidos = 0;

                DataRow? datosUsuario = await Task.Run(
                    () => _db.ObtenerUsuarioPorEmail(_correoUsuario), _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                if (!IntentarIniciarSesion(datosUsuario))
                {
                    MostrarError("⚠ No se pudo cargar la información de tu cuenta. Intenta de nuevo o contacta soporte.");
                    return;
                }

                AbrirDashboardYCerrar();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MostrarError("⚠ Error al validar el código: " + ex.Message);
            }
            finally
            {
                _verificando = false;
                if (IsLoaded)
                {
                    HabilitarCajas(true);
                    if (_segundos > 0 && _intentosFallidos < MaxIntentosFallidos)
                        btnVerificar.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Registra un intento fallido de verificación. Si se alcanza el máximo
        /// permitido, bloquea el botón de verificar para mitigar fuerza bruta.
        /// </summary>
        private void RegistrarIntentoFallido()
        {
            _intentosFallidos++;

            if (_intentosFallidos >= MaxIntentosFallidos)
            {
                btnVerificar.IsEnabled = false;
                MostrarError("⚠ Demasiados intentos fallidos. Reenvía un nuevo código para continuar.");
            }
            else
            {
                int restantes = MaxIntentosFallidos - _intentosFallidos;
                MostrarError($"⚠ Código incorrecto o expirado. Te quedan {restantes} intento(s).");
            }

            LimpiarCajas();
        }

        /// <summary>
        /// Extrae los datos del usuario desde el DataRow de forma segura (sin acceso
        /// directo a columnas que puedan no existir o venir en DBNull) e inicia sesión.
        /// Devuelve false si los datos son insuficientes; en ese caso NO se debe
        /// continuar hacia el dashboard.
        /// </summary>
        private static bool IntentarIniciarSesion(DataRow? datosUsuario)
        {
            if (datosUsuario is null) return false;

            string email = ObtenerValorTexto(datosUsuario, "Usuario_Email");
            if (string.IsNullOrWhiteSpace(email)) return false;

            string nombre = ObtenerValorTexto(datosUsuario, "Usuario_Nombre");
            string apellido = ObtenerValorTexto(datosUsuario, "Usuario_Apellido");
            string rol = ObtenerValorTexto(datosUsuario, "Usuario_Rol");

            SesionActual.IniciarSesion(email, nombre, apellido, rol);
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
            return valor is null or DBNull ? string.Empty : valor.ToString() ?? string.Empty;
        }

        #endregion

        #region Navegación (dashboard / regresar)

        /// <summary>
        /// Ejecuta el patrón común de navegación: evita reentradas concurrentes,
        /// crea la ventana destino, la muestra y cierra la actual. Si algo falla,
        /// cierra de forma segura la ventana a medio crear y reporta el error con
        /// el callback proporcionado. Antes este bloque estaba duplicado entre
        /// "ir al dashboard" y "regresar a OpcionSesion", cada uno con su propio
        /// try/catch casi idéntico.
        /// </summary>
        private void EjecutarNavegacion<T>(Func<T> crear, Action<string> alFallar) where T : Window
        {
            if (_navegando) return;
            _navegando = true;

            T? ventanaNueva = null;
            try
            {
                ventanaNueva = crear();
                ventanaNueva.Show();
                Close();
            }
            catch (Exception ex)
            {
                CerrarSiPosible(ventanaNueva);
                _navegando = false;
                alFallar(ex.Message);
            }
        }

        private void AbrirDashboardYCerrar()
        {
            _timer.Stop();
            EjecutarNavegacion(
                () => new Dasboard_Prueba.MenuPrincipal(),
                msg => MostrarError("⚠ No se pudo abrir el panel principal: " + msg));
        }

        /// <summary>
        /// Regresa a la pantalla anterior SIN generar ni enviar un nuevo código.
        /// </summary>
        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            EjecutarNavegacion(
                () => new OpcionSesion(_correoUsuario),
                msg => MessageBox.Show("No se pudo regresar a la pantalla anterior:\n" + msg,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error));
        }

        private static void CerrarSiPosible(Window? ventana)
        {
            if (ventana is null) return;
            try { ventana.Close(); }
            catch { }
        }

        #endregion

        #region Mensajes de error en pantalla

        private void MostrarError(string msg)
        {
            txtErrorCodigo.Text = msg;
            txtErrorCodigo.Visibility = Visibility.Visible;
        }

        private void OcultarError()
            => txtErrorCodigo.Visibility = Visibility.Collapsed;

        #endregion
    }
}