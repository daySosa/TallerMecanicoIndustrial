using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Login
{
    public partial class VerificacionCodigo : Window
    {
        private readonly string _correoUsuario;
        private readonly clsAutenticacion _autenticacion = new clsAutenticacion();

        public VerificacionCodigo(string correo)
        {
            InitializeComponent();
            _correoUsuario = correo;
        }

        private void Window_Drag(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  VERIFICAR
        // ════════════════════════════════════════════════════════════════════════

        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = txtCodigo.Text.Trim();

            var (ok, mensaje) = clsValidacionCodigo2FA.ValidarCodigo(codigo);
            if (!ok)
            {
                MostrarError(mensaje);
                return;
            }

            bool codigoValido = _autenticacion.ValidarCodigo(_correoUsuario, codigo);
            if (codigoValido)
            {
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            }
            else
            {
                MostrarError("⚠ Código incorrecto o expirado. Intenta nuevamente.");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  REENVIAR
        // ════════════════════════════════════════════════════════════════════════

        private void BtnReenviar_Click(object sender, RoutedEventArgs e)
        {
            string codigo = _autenticacion.GenerarCodigo(_correoUsuario);
            bool enviado = _autenticacion.EnviarCorreo(_correoUsuario, codigo);

            if (enviado)
                MessageBox.Show("✅ Código reenviado a tu correo.", "Código enviado",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("⚠ No se pudo reenviar el código. Intenta nuevamente.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ════════════════════════════════════════════════════════════════════════
        //  FOCO Y CAMBIO DE TEXTO
        // ════════════════════════════════════════════════════════════════════════

        private void TxtCodigo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCodigo.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
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

        // ════════════════════════════════════════════════════════════════════════
        //  REGRESAR
        // ════════════════════════════════════════════════════════════════════════

        private void BtnRegresar_Click(object sender, RoutedEventArgs e)
        {
            new OpcionSesion(_correoUsuario).Show();
            this.Close();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  HELPER
        // ════════════════════════════════════════════════════════════════════════

        private void MostrarError(string mensaje)
        {
            txtErrorCodigo.Text = mensaje;
            txtErrorCodigo.Visibility = Visibility.Visible;
            borderCodigo.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
            borderCodigo.BorderThickness = new Thickness(2);
        }
    }
}