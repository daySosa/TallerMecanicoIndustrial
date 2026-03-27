using Login.Clases;
using System.Data.SqlClient;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Login
{
    /// <summary>
    /// Ventana principal de inicio de sesión.
    /// Permite autenticación de usuarios, validación de credenciales
    /// y manejo de autenticación en dos factores (2FA).
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Indica si la contraseña se muestra en texto visible o en modo oculto.
        /// </summary>
        private bool _contrasenaVisible = false;

        /// <summary>
        /// Instancia utilizada para validar credenciales en la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Ruta del archivo donde se almacenan las credenciales recordadas del usuario.
        /// </summary>
        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="MainWindow"/>
        /// y carga las credenciales guardadas si existen.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            CargarCredencialesRecordadas();
        }

        /// <summary>
        /// Permite mover la ventana arrastrándola con el mouse.
        /// </summary>
        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Aplica estilo visual al campo de correo cuando recibe el foco.
        /// </summary>
        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            borderCorreo.BorderThickness = new Thickness(2);
        }

        /// <summary>
        /// Restaura el estilo visual del campo de correo al perder el foco.
        /// </summary>
        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderCorreo.BorderThickness = new Thickness(1.5);
        }

        /// <summary>
        /// Aplica estilo visual al campo de contraseña cuando recibe el foco.
        /// </summary>
        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            borderContrasena.BorderThickness = new Thickness(2);
        }

        /// <summary>
        /// Restaura el estilo visual del campo de contraseña al perder el foco.
        /// </summary>
        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderContrasena.BorderThickness = new Thickness(1.5);
        }

        /// <summary>
        /// Alterna la visibilidad de la contraseña entre texto oculto y visible.
        /// </summary>
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


        /// <summary>
        /// Obtiene la contraseña ingresada por el usuario según el modo de visualización.
        /// </summary>
        /// <returns>Contraseña ingresada.</returns>
        private string ObtenerContrasena()
        {
            return _contrasenaVisible
                ? txtContrasenaVisible.Text
                : txtContrasena.Password;
        }


        /// <summary>
        /// Guarda las credenciales del usuario en un archivo local en formato JSON.
        /// </summary>
        private void GuardarCredenciales(string correo, string contrasena)
        {
            var datos = new { Correo = correo, Contrasena = contrasena };
            string json = JsonSerializer.Serialize(datos);
            File.WriteAllText(_archivoRecordar, json);
        }

        /// <summary>
        /// Elimina las credenciales almacenadas localmente.
        /// </summary>
        private void EliminarCredenciales()
        {
            if (File.Exists(_archivoRecordar))
                File.Delete(_archivoRecordar);
        }

        //// <summary>
        /// Carga las credenciales almacenadas previamente y las muestra en la interfaz.
        /// </summary>
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

        /// <summary>
        /// Valida los datos ingresados y ejecuta el proceso de inicio de sesión.
        /// </summary>
        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            bool hayError = false;

            string errorCorreo = clsValidaciones.ValidarCorreoLogin(txtCorreo.Text);

            if (errorCorreo != null)
            {
                txtErrorCorreo.Text = errorCorreo;
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
            string errorContrasena = clsValidaciones.ValidarContrasenaLogin(contrasena);

            if (errorContrasena != null)
            {
                txtErrorContrasena.Text = errorContrasena;
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

        /// <summary>
        /// Verifica las credenciales del usuario y ejecuta la autenticación en dos factores (2FA).
        /// </summary>
        private void IniciarSesion(string correo, string contrasena)
        {
            try
            {
                bool valido = _db.ValidarLogin(correo, contrasena);

                if (valido)
                {
                    clsAutenticacion autenticacion = new clsAutenticacion();
                    string codigo2FA = autenticacion.GenerarCodigo(correo);
                    bool enviado = autenticacion.EnviarCorreo(correo, codigo2FA);

                    if (enviado)
                    {
                        OpcionSesion ventanaVerificacion = new OpcionSesion(correo);
                        ventanaVerificacion.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("⚠ No se pudo enviar el código. Intenta nuevamente.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
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

        private void txtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }
    }
}