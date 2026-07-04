#nullable enable
using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        private readonly string _correoUsuario;
        private bool _navegando;

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
            try { DragMove(); }
            catch (InvalidOperationException) { /* Soltó el botón antes de iniciar el arrastre */ }
        }

        /// <summary>
        /// Crea la siguiente ventana y cierra la actual de forma INMEDIATA, sin
        /// esperar operaciones de red/BD aquí. Cualquier tarea larga —como el
        /// envío del código OTP— se ejecuta dentro de la ventana de destino, con
        /// su propio indicador de carga, para que el cambio de pantalla se sienta
        /// instantáneo en vez de "congelado".
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
        /// Navega de inmediato a Verificacion2FA. Esa ventana es responsable de
        /// generar y enviar el código OTP inicial (una sola vez, al cargarse),
        /// mostrando su propio estado de carga mientras dura el envío.
        /// </summary>
        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            if (_correoUsuario.Length == 0)
            {
                MessageBox.Show(
                    "No es posible continuar: no hay un correo asociado a esta sesión.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Navegar(() => new Verificacion2FA(_correoUsuario));
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

            // ✅ Ahora sí se le pasa el correo de la cuenta que se está verificando
            Navegar(() => new ReconocimientoFacial(_correoUsuario));
        }
    }
}