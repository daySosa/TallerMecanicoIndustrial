using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.IO;
using System.Text.Json;

namespace Login
{
    public partial class MainWindow : Window
    {
        private bool _contrasenaVisible = false;

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        public MainWindow()
        {
            InitializeComponent();
            CargarCredencialesRecordadas();
        }


        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }


        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            borderCorreo.BorderThickness = new Thickness(2);
        }

        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderCorreo.BorderThickness = new Thickness(1.5);
        }

        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
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

            if (_contrasenaVisible)
            {
                
                txtContrasenaVisible.Text = txtContrasena.Password;
                txtContrasena.Visibility = Visibility.Collapsed;
                txtContrasenaVisible.Visibility = Visibility.Visible;
                txtContrasenaVisible.Focus();
                txtContrasenaVisible.CaretIndex = txtContrasenaVisible.Text.Length;
                iconOjo.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
            }
            else
            {
               
                txtContrasena.Password = txtContrasenaVisible.Text;
                txtContrasenaVisible.Visibility = Visibility.Collapsed;
                txtContrasena.Visibility = Visibility.Visible;
                txtContrasena.Focus();
                iconOjo.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
            }
        }


        private string ObtenerContrasena()
        {
            return _contrasenaVisible
                ? txtContrasenaVisible.Text
                : txtContrasena.Password;
        }


        private void GuardarCredenciales(string correo, string contrasena)
        {
            var datos = new { Correo = correo, Contrasena = contrasena };
            string json = JsonSerializer.Serialize(datos);
            File.WriteAllText(_archivoRecordar, json);
        }

        private void EliminarCredenciales()
        {
            if (File.Exists(_archivoRecordar))
                File.Delete(_archivoRecordar);
        }

        private void CargarCredencialesRecordadas()
        {
            try
            {
                if (!File.Exists(_archivoRecordar)) return;

                string json = File.ReadAllText(_archivoRecordar);
                var datos = JsonSerializer.Deserialize<JsonElement>(json);

                txtCorreo.Text = datos.GetProperty("Correo").GetString() ?? "";
                txtContrasena.Password = datos.GetProperty("Contrasena").GetString() ?? "";
                chkRecordar.IsChecked = true;
            }
            catch
            {
                
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            bool hayError = false;


            if (string.IsNullOrWhiteSpace(txtCorreo.Text))
            {
                txtErrorCorreo.Text = "⚠ El correo es obligatorio.";
                txtErrorCorreo.Visibility = Visibility.Visible;
                borderCorreo.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                hayError = true;
            }
            else
            {
                txtErrorCorreo.Visibility = Visibility.Collapsed;
                borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }


            string contrasena = ObtenerContrasena();
            if (string.IsNullOrWhiteSpace(contrasena))
            {
                txtErrorContrasena.Text = "⚠ La contraseña es obligatoria.";
                txtErrorContrasena.Visibility = Visibility.Visible;
                borderContrasena.BorderBrush =
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                hayError = true;
            }
            else
            {
                txtErrorContrasena.Visibility = Visibility.Collapsed;
                borderContrasena.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }

            if (hayError) return;


            if (chkRecordar.IsChecked == true)
                GuardarCredenciales(txtCorreo.Text.Trim(), contrasena);
            else
                EliminarCredenciales();

            IniciarSesion(txtCorreo.Text.Trim(), contrasena);
        }

        private void IniciarSesion(string correo, string contrasena)
        {
            try
            {
                clsConexion conexion = new clsConexion();
                conexion.Abrir();

                string consulta = @"SELECT * FROM LOGIN
                                    WHERE Usuario_Email     = @correo
                                    AND   Usuario_Contraseña = @contrasena";

                SqlCommand comando = new SqlCommand(consulta, conexion.SqlC);
                comando.Parameters.AddWithValue("@correo", correo);
                comando.Parameters.AddWithValue("@contrasena", contrasena);

                SqlDataReader lector = comando.ExecuteReader();

                if (lector.Read())
                {
                    lector.Close();
                    conexion.Cerrar();

<<<<<<< Updated upstream
                    Dasboard_Prueba.MenuPrincipal ventanaPrincipal =
                        new Dasboard_Prueba.MenuPrincipal();
                    ventanaPrincipal.Show();
                    this.Hide();
=======
                    /*MessageBox.Show("¡Bienvenido!", "Inicio de sesión exitoso",
                        MessageBoxButton.OK, MessageBoxImage.Information);*/
                    clsAutenticacion autenticacion = new clsAutenticacion();
                    string codigo2FA = autenticacion.GenerarCodigo(correo);
                    bool enviado = autenticacion.EnviarCorreo(correo, codigo2FA);

                    if (enviado)
                    {
                        //Abrir ventana de verificación
<<<<<<< Updated upstream
                        OpcionSesion ventanaVerificacion = new OpcionSesion(correo);
                        ventanaVerificacion.Show();
=======
                        Verificacion2FA ventanaOpcion = new OpcionSesion(correo);
                        ventanaOpcion.Show();
>>>>>>> Stashed changes
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("⚠ No se pudo enviar el código. Intenta nuevamente.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
>>>>>>> Stashed changes
                }
                else
                {
                    lector.Close();
                    conexion.Cerrar();

                    txtErrorCorreo.Text = "⚠ Correo o contraseña incorrectos.";
                    txtErrorCorreo.Visibility = Visibility.Visible;
                    borderCorreo.BorderBrush =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                    borderContrasena.BorderBrush =
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al conectar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}