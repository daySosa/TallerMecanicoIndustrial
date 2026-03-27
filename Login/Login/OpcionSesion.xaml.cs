using System.Windows;
using System.Windows.Input;

namespace Login
{
    /// <summary>
    /// Ventana que permite al usuario seleccionar el método de inicio de sesión.
    /// Ofrece opciones como código de verificación (2FA) o reconocimiento facial.
    /// </summary>
    public partial class OpcionSesion : Window
    {
        /// <summary>
        /// Almacena el correo del usuario que inició el proceso de autenticación.
        /// </summary>
        private readonly string _correoUsuario;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="OpcionSesion"/>.
        /// </summary>
        /// <param name="correo">Correo electrónico del usuario autenticado.</param>
        public OpcionSesion(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;
        }

        /// <summary>
        /// Permite arrastrar la ventana cuando el usuario mantiene presionado el botón izquierdo del mouse.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Información del evento del mouse.</param>
        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Evento click del botón de código de verificación.
        /// Abre la ventana de autenticación mediante 2FA (código de verificación).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Información del evento de clic.</param>
        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            Verificacion2FA ventana = new Verificacion2FA(_correoUsuario);
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón de reconocimiento facial.
        /// Abre la ventana que permite autenticar al usuario mediante reconocimiento facial.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Información del evento de clic.</param>
        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            ReconocimientoFacial ventana = new ReconocimientoFacial();
            ventana.Show();
            this.Close();
        }
    }
}
