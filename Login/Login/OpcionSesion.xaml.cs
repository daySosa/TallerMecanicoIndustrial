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
        /// Instancia diferida de ReconocimientoFacial.
        /// Se crea solo cuando el usuario pulsa el botón por primera vez,
        /// evitando cargar Emgu.CV y AForge al arrancar la aplicación.
        /// </summary>
        private ReconocimientoFacial? _ventanaReconocimiento;

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
        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Evento click del botón de código de verificación.
        /// Abre la ventana de autenticación mediante 2FA.
        /// </summary>
        private void BtnCodigoVerificacion_Click(object sender, RoutedEventArgs e)
        {
            Verificacion2FA ventana = new Verificacion2FA(_correoUsuario);
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón de reconocimiento facial.
        /// Crea la ventana de reconocimiento solo la primera vez que se pulsa
        /// (carga diferida) para no cargar Emgu.CV al arrancar la app.
        /// </summary>
        private void BtnReconocimientoFacial_Click(object sender, RoutedEventArgs e)
        {
            // ✅ Lazy loading: Emgu.CV y AForge solo se inicializan aquí,
            //    no al arrancar la aplicación completa.
            _ventanaReconocimiento ??= new ReconocimientoFacial(_correoUsuario);

            _ventanaReconocimiento.Show();
            this.Close();
        }
    }
}
