using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login
{
    /// <summary>
    /// Ventana encargada de la verificación de autenticación en dos factores (2FA).
    /// Permite validar un código enviado al correo del usuario, reenviarlo y redirigir
    /// al menú principal en caso de éxito.
    /// </summary>
    public partial class Verificacion2FA : Window
    {
        /// <summary>
        /// Correo del usuario autenticado.
        /// </summary>
        private readonly string _correoUsuario;

        /// <summary>
        /// Instancia del servicio de autenticación.
        /// </summary>
        private readonly clsAutenticacion _autenticacion = new clsAutenticacion();

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="Verificacion2FA"/>.
        /// </summary>
        /// <param name="correo">Correo electrónico del usuario.</param>
        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;
        }

        /// <summary>
        /// Permite arrastrar la ventana cuando el usuario mantiene presionado el botón izquierdo del mouse.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento del mouse.</param>
        private void Window_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Maneja el evento Click del botón de verificación del código 2FA.
        /// Valida que el código sea numérico, tenga 6 dígitos y sea correcto.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            string codigoIngresado = txtCodigo.Text.Trim();

            if (string.IsNullOrWhiteSpace(codigoIngresado))
            {
                txtErrorCodigo.Text = "⚠ Ingresa el código de verificación.";
                txtErrorCodigo.Visibility = Visibility.Visible;
                borderCodigo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                borderCodigo.BorderThickness = new Thickness(2);
                return;
            }

            if (!codigoIngresado.All(char.IsDigit))
            {
                txtErrorCodigo.Text = "⚠ El código solo debe contener números.";
                txtErrorCodigo.Visibility = Visibility.Visible;
                borderCodigo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                borderCodigo.BorderThickness = new Thickness(2);
                return;
            }

            if (codigoIngresado.Length != 6)
            {
                txtErrorCodigo.Text = "⚠ El código debe tener exactamente 6 dígitos.";
                txtErrorCodigo.Visibility = Visibility.Visible;
                borderCodigo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                borderCodigo.BorderThickness = new Thickness(2);
                return;
            }

            bool codigoValido = _autenticacion.ValidarCodigo(_correoUsuario, codigoIngresado);
            if (codigoValido)
            {
                Dasboard_Prueba.MenuPrincipal menu = new Dasboard_Prueba.MenuPrincipal();
                menu.Show();
                this.Close();
            }
            else
            {
                txtErrorCodigo.Text = "⚠ Código incorrecto o expirado. Intenta nuevamente.";
                txtErrorCodigo.Visibility = Visibility.Visible;
                borderCodigo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                borderCodigo.BorderThickness = new Thickness(2);
            }
        }


        /// <summary>
        /// Maneja el evento Click del botón para reenviar el código de verificación.
        /// Genera un nuevo código y lo envía al correo del usuario.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = _autenticacion.GenerarCodigo(_correoUsuario);
            bool enviado = _autenticacion.EnviarCorreo(_correoUsuario, codigo);

            if (enviado)
            {
                MessageBox.Show("✅ Código reenviado a tu correo.", "Código enviado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("⚠ No se pudo reenviar el código. Intenta nuevamente.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el evento GotFocus del campo de texto del código.
        /// Cambia el estilo del borde para indicar que el campo está activo.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void TxtCodigo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCodigo.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            borderCodigo.BorderThickness = new Thickness(2);
        }

        /// <summary>
        /// Maneja el evento LostFocus del campo de texto del código.
        /// Restablece el estilo del borde cuando el campo pierde el foco.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void TxtCodigo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCodigo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderCodigo.BorderThickness = new Thickness(1.5);
        }

        /// <summary>
        /// Maneja el evento TextChanged del campo de código.
        /// Oculta el mensaje de error cuando el usuario modifica el texto.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void txtCodigo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtErrorCodigo.Visibility == Visibility.Visible)
            {
                txtErrorCodigo.Visibility = Visibility.Collapsed;
                borderCodigo.BorderBrush = new SolidColorBrush(Colors.Transparent);
                borderCodigo.BorderThickness = new Thickness(1.5);
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón regresar.
        /// Abre la ventana de opciones de sesión y cierra la ventana actual.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            OpcionSesion ventana = new OpcionSesion(_correoUsuario);
            ventana.Show();
            this.Close();
        }
    }
}

