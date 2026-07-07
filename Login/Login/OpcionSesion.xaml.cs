#nullable enable
using System.Windows;
using System.Windows.Input;

namespace Login
{
    public partial class OpcionSesion : Window
    {
        #region Campos

        private readonly string _correoUsuario;
        private bool _navegando;

        #endregion

        #region Constructor

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

        #endregion

        #region Ventana sin borde (arrastre)

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            try { DragMove(); }
            catch (InvalidOperationException) { }
        }

        #endregion

        #region Navegación entre ventanas

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
            catch { }
        }

        #endregion

        #region Validación de correo disponible

        /// <summary>
        /// Verifica que haya un correo asociado a la sesión antes de continuar con
        /// cualquiera de los dos métodos de verificación. Si no lo hay, muestra el
        /// mensaje de aviso correspondiente y devuelve false para que el llamador
        /// corte el flujo. Centraliza la validación que antes estaba duplicada en
        /// ambos manejadores de clic, cada uno con su propio texto de aviso.
        /// </summary>
        private bool HayCorreoDisponible(string mensajeSiFalta)
        {
            if (_correoUsuario.Length > 0) return true;

            MessageBox.Show(mensajeSiFalta, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        #endregion

        #region Manejadores de botones

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MainWindow());

        /// <summary>
        /// Navega de inmediato a Verificacion2FA. Esa ventana es responsable de
        /// generar y enviar el código OTP inicial (una sola vez, al cargarse),
        /// mostrando su propio estado de carga mientras dura el envío.
        /// </summary>
        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            if (!HayCorreoDisponible("No es posible continuar: no hay un correo asociado a esta sesión."))
                return;

            Navegar(() => new Verificacion2FA(_correoUsuario));
        }

        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            if (!HayCorreoDisponible("No es posible iniciar el reconocimiento facial: no hay un correo asociado a esta sesión."))
                return;

            Navegar(() => new ReconocimientoFacial(_correoUsuario));
        }

        #endregion
    }
}