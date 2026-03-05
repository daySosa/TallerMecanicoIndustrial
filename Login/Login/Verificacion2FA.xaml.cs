using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login
{
    public partial class Verificacion2FA : Window
    {
        private string userEmail;
        private clsAutenticacion autenticacion = new clsAutenticacion();

        public Verificacion2FA(string correo)
        {
            InitializeComponent();
            userEmail = correo;
        }

        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            string codigoIngresado = txtCodigo.Text.Trim();

            if (string.IsNullOrWhiteSpace(codigoIngresado))
            {
                txtErrorCorreo.Text = "⚠ Ingresa el código.";
                txtErrorCorreo.Visibility = Visibility.Visible;
                return;
            }

            bool codigoValido = autenticacion.ValidarCodigo(userEmail, codigoIngresado);

            if (codigoValido)
            {
                Dasboard_Prueba.MenuPrincipal menu = new Dasboard_Prueba.MenuPrincipal();
                menu.Show();
                this.Close();
            }
            else
            {
                txtErrorCorreo.Text = "⚠ Código incorrecto o expirado. Intenta nuevamente.";
                txtErrorCorreo.Visibility = Visibility.Visible;

                borderCodigo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                borderCodigo.BorderThickness = new Thickness(2);
            }
        }

        private void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = autenticacion.GenerarCodigo(userEmail);
            bool enviado = autenticacion.EnviarCorreo(userEmail, codigo);

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

            if (txtErrorCorreo.Visibility == Visibility.Visible)
            {
                txtErrorCorreo.Visibility = Visibility.Collapsed;
                borderCodigo.BorderBrush = new SolidColorBrush(Colors.Transparent);
                borderCodigo.BorderThickness = new Thickness(1.5);
            }
        }

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            OpcionSesion ventana = new OpcionSesion(userEmail);
            ventana.Show();
            this.Close();
        }
    }
}