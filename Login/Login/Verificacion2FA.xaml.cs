#nullable enable
using Login.Clases;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
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

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            _cajas = [d1, d2, d3, d4, d5, d6];
            ConfigurarCajas();

            _timer.Tick += Timer_Tick;
            _timer.Start();

            if (_correoUsuario.Length == 0)
            {
                MostrarError("⚠ No hay un correo válido asociado a esta verificación.");
                btnVerificar.IsEnabled = false;
                btnReenviar.IsEnabled = false;
            }
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            try { DragMove(); }
            catch (InvalidOperationException) { /* soltó el botón antes de iniciar el arrastre */ }
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

            // Siempre se cancela el pegado por defecto: o ya se distribuyó
            // manualmente, o el contenido no era válido.
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

            // Defensa adicional: si por cualquier vía llegara más de 1 carácter,
            // se recorta al primero en vez de confiar únicamente en MaxLength del XAML.
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

                _timer.Stop();
                AbrirDashboardYCerrar();
            }
            catch (OperationCanceledException)
            {
                // Ventana cerrada mientras la verificación estaba en curso; nada que hacer.
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

        private void AbrirDashboardYCerrar()
        {
            if (_navegando) return;
            _navegando = true;

            Dasboard_Prueba.MenuPrincipal? dashboard = null;
            try
            {
                dashboard = new Dasboard_Prueba.MenuPrincipal();
                dashboard.Show();
                Close();
            }
            catch (Exception ex)
            {
                CerrarSiPosible(dashboard);
                _navegando = false;
                MostrarError("⚠ No se pudo abrir el panel principal: " + ex.Message);
            }
        }

        /// <summary>
        /// Único punto donde se solicita un nuevo código: acción explícita del
        /// usuario al presionar "Reenviar ahora". Nunca debe dispararse de forma
        /// automática (ni al abrir la ventana ni al presionar "Cancelar").
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
                bool enviado = await Task.Run(() =>
                {
                    string codigo = _db.GenerarCodigoOTP(_correoUsuario);
                    return _db.EnviarCorreoOTP(_correoUsuario, codigo);
                }, _cts.Token);

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
                // Ventana cerrada mientras se reenviaba; nada que hacer.
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

        /// <summary>
        /// Regresa a la pantalla anterior SIN generar ni enviar un nuevo código.
        /// El reenvío es una acción explícita reservada a "Reenviar ahora".
        /// </summary>
        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            if (_navegando) return;
            _navegando = true;

            _timer.Stop();

            OpcionSesion? anterior = null;
            try
            {
                anterior = new OpcionSesion(_correoUsuario);
                anterior.Show();
                Close();
            }
            catch (Exception ex)
            {
                CerrarSiPosible(anterior);
                _navegando = false;
                MessageBox.Show("No se pudo regresar a la pantalla anterior:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CerrarSiPosible(Window? ventana)
        {
            if (ventana is null) return;
            try { ventana.Close(); }
            catch { /* La ventana ya pudo haberse cerrado o nunca llegó a mostrarse */ }
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