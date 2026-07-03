#nullable enable
using Login.Clases;
using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private readonly string _correoUsuario;
        private readonly RepositorioSql _repositorio = new();
        private bool _navegando;
        private bool _enviandoCodigo;

        public OpcionSesion(string correo)
        {
            InitializeComponent();
            _correoUsuario = string.IsNullOrWhiteSpace(correo) ? string.Empty : correo.Trim();

            if (_correoUsuario.Length == 0)
            {
                MessageBox.Show(
                    "No se recibió un correo válido para continuar con la verificación.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            try
            {
                DragMove();
            }
            catch (InvalidOperationException)
            {
                // Ocurre si el mouse se suelta antes de iniciar el arrastre; se ignora.
            }
        }

        /// <summary>
        /// Crea la siguiente ventana del flujo y cierra la actual. Bloquea clics
        /// repetidos mientras la navegación está en curso (_navegando) y revierte
        /// el estado si la creación de la ventana falla.
        /// </summary>
        private void Navegar<T>(Func<T> crear) where T : Window
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

                MessageBox.Show(
                    "No se pudo abrir la siguiente ventana:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CerrarSiPosible(Window? ventana)
        {
            if (ventana is null) return;
            try { ventana.Close(); }
            catch { /* La ventana ya pudo haberse cerrado o nunca llegó a mostrarse */ }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MainWindow());

        /// <summary>
        /// Único punto donde se genera y envía el código OTP inicial: la elección
        /// explícita del usuario por "Código de verificación". No debe enviarse
        /// antes de este punto (ni en el login, ni en el constructor de esta
        /// ventana, ni en el de Verificacion2FA).
        /// </summary>
        private async void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            if (_enviandoCodigo) return;

            if (_correoUsuario.Length == 0)
            {
                MessageBox.Show(
                    "No es posible enviar el código: no hay un correo asociado a esta sesión.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _enviandoCodigo = true;
            btnCodigoVerificacion.IsEnabled = false;
            btnReconocimientoFacial.IsEnabled = false;

            try
            {
                bool enviado = await Task.Run(() =>
                {
                    string codigo = _repositorio.GenerarCodigoOTP(_correoUsuario);
                    return _repositorio.EnviarCorreoOTP(_correoUsuario, codigo);
                });

                if (!enviado)
                {
                    MessageBox.Show(
                        "⚠ No se pudo enviar el código. Intenta nuevamente.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Navegar(() => new Verificacion2FA(_correoUsuario));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "⚠ No se pudo enviar el código: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _enviandoCodigo = false;
                if (!_navegando)
                {
                    btnCodigoVerificacion.IsEnabled = true;
                    btnReconocimientoFacial.IsEnabled = true;
                }
            }
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            if (_correoUsuario.Length == 0)
            {
                MessageBox.Show(
                    "No es posible iniciar el reconocimiento facial: no hay un correo asociado a esta sesión.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Navegar(() => new ReconocimientoFacial());
        }
    }
}