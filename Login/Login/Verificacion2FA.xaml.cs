using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
        private readonly string _correoUsuario;
        private readonly clsAutenticacion _autenticacion = new clsAutenticacion();

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;
        }

        private void Window_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

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

        private void TxtCodigo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCodigo.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            borderCodigo.BorderThickness = new Thickness(2);
        }

        private void TxtCodigo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCodigo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderCodigo.BorderThickness = new Thickness(1.5);
        }

        private void txtCodigo_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtErrorCodigo.Visibility == Visibility.Visible)
            {
                txtErrorCodigo.Visibility = Visibility.Collapsed;
                borderCodigo.BorderBrush = new SolidColorBrush(Colors.Transparent);
                borderCodigo.BorderThickness = new Thickness(1.5);
            }
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            OpcionSesion ventana = new OpcionSesion(_correoUsuario);
            ventana.Show();
            this.Close();
        }
    }
}