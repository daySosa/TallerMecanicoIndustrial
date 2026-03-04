using System.Collections;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Login
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _contrasenaVisible = false;

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#2563EB"));
            borderCorreo.BorderThickness = new Thickness(2);
        }
        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderCorreo.BorderThickness = new Thickness(1.5);
        }

        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#2563EB"));
            borderContrasena.BorderThickness = new Thickness(2);
        }
        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderContrasena.BorderThickness = new Thickness(1.5);
        }

        private void BtnVerContrasena_Click(object sender, RoutedEventArgs e)
        {
            _contrasenaVisible = !_contrasenaVisible;
            iconOjo.Kind = _contrasenaVisible
                ? MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline
                : MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            bool hayError = false;

            if (string.IsNullOrWhiteSpace(txtCorreo.Text))
            {
                txtErrorCorreo.Text = "⚠ El correo es obligatorio.";
                txtErrorCorreo.Visibility = Visibility.Visible;
                borderCorreo.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#f44336"));
                hayError = true;
            }
            else
            {
                txtErrorCorreo.Visibility = Visibility.Collapsed;
                borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }

            if (string.IsNullOrWhiteSpace(txtContrasena.Password))
            {
                txtErrorContrasena.Text = "⚠ La contraseña es obligatoria.";
                txtErrorContrasena.Visibility = Visibility.Visible;
                borderContrasena.BorderBrush = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#f44336"));
                hayError = true;
            }
            else
            {
                txtErrorContrasena.Visibility = Visibility.Collapsed;
                borderContrasena.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }

            if (hayError) return;

            IniciarSesion(txtCorreo.Text.Trim(), txtContrasena.Password.Trim());
        }

        private void IniciarSesion(string correo, string contrasena)
        {
            try
            {
                clsConexion conexion = new clsConexion();
                conexion.Abrir();

                string consulta = @"SELECT * FROM LOGIN 
                                    WHERE Usuario_Email = @correo 
                                    AND Usuario_Contraseña = @contrasena";

                SqlCommand comando = new SqlCommand(consulta, conexion.SqlC);
                comando.Parameters.AddWithValue("@correo", correo);
                comando.Parameters.AddWithValue("@contrasena", contrasena);

                SqlDataReader lector = comando.ExecuteReader();

                if (lector.Read())
                {
                    lector.Close();
                    conexion.Cerrar();

                    // Abrir ventana principal
                    // MenuPrincipal ventanaPrincipal = new MenuPrincipal();
                    // ventanaPrincipal.Show();
                    // this.Close();

                    MessageBox.Show("¡Bienvenido!", "Inicio de sesión exitoso",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    lector.Close();
                    conexion.Cerrar();

                    txtErrorCorreo.Text = "⚠ Correo o contraseña incorrectos.";
                    txtErrorCorreo.Visibility = Visibility.Visible;
                    borderCorreo.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#f44336"));
                    borderContrasena.BorderBrush = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#f44336"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }



    }
}