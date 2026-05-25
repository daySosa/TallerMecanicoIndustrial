using Login.Clases;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Login
{
    public partial class VentanaLogin : Window
    {
        private bool _contrasenaVisible = false;
        private clsConsultasBD _db = new clsConsultasBD();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        public VentanaLogin()
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
            => _contrasenaVisible ? txtContrasenaVisible.Text : txtContrasena.Password;

        private void GuardarCredenciales(string correo, string contrasena)
        {
            var datos = new { Correo = correo, Contrasena = contrasena };
            File.WriteAllText(_archivoRecordar, JsonSerializer.Serialize(datos));
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
            catch { }
        }

        private void txtCorreo_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtErrorCorreo.Visibility == Visibility.Visible)
            {
                txtErrorCorreo.Visibility = Visibility.Collapsed;
                borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LimpiarErrores();

            string correo = txtCorreo.Text.Trim();
            string contrasena = ObtenerContrasena();

            string? error = clsValidacionLogin.ValidarTodo(correo, contrasena);

            if (error != null)
            {
                MostrarErrorCorreo(error);
                return;
            }

            if (chkRecordar.IsChecked == true)
                GuardarCredenciales(correo, contrasena);
            else
                EliminarCredenciales();

            IniciarSesion(correo, contrasena);
        }

        private void IniciarSesion(string correo, string contrasena)
        {
            try
            {
                bool valido = _db.ValidarLogin(correo, contrasena);

                if (valido)
                {
                    clsValidacionLogin.ResetearIntentos();

                    clsAutenticacion autenticacion = new clsAutenticacion();
                    string codigo2FA = autenticacion.GenerarCodigo(correo);
                    bool enviado = autenticacion.EnviarCorreo(correo, codigo2FA);

                    if (enviado)
                    {
                        OpcionSesion ventanaVerificacion = new OpcionSesion(correo);
                        this.Close();
                        ventanaVerificacion.Show();
                    }
                    else
                    {
                        MessageBox.Show(
                            "⚠ No se pudo enviar el código de verificación. Intenta nuevamente.",
                            "Error de envío",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
                else
                {
                    clsValidacionLogin.RegistrarIntentoFallido();

                    if (clsValidacionLogin.EstasBloqueado())
                    {
                        MostrarErrorCorreo(
                            $"⛔ Cuenta bloqueada temporalmente. Espera {clsValidacionLogin.TiempoRestanteBloqueo()}.");
                    }
                    else
                    {
                        MostrarErrorCorreo(
                            $"⚠ Correo o contraseña incorrectos. " +
                            $"Intentos restantes: {clsValidacionLogin.IntentosRestantes()}");
                        MostrarBordeError(borderContrasena);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al conectar con el servidor: " + ex.Message,
                    "Error de conexión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LimpiarErrores()
        {
            txtErrorCorreo.Visibility = Visibility.Collapsed;
            txtErrorContrasena.Visibility = Visibility.Collapsed;
            borderCorreo.BorderBrush = new SolidColorBrush(Colors.Transparent);
            borderContrasena.BorderBrush = new SolidColorBrush(Colors.Transparent);
        }

        private void MostrarErrorCorreo(string mensaje)
        {
            txtErrorCorreo.Text = mensaje;
            txtErrorCorreo.Visibility = Visibility.Visible;
            MostrarBordeError(borderCorreo);
        }

        private void MostrarErrorContrasena(string mensaje)
        {
            txtErrorContrasena.Text = mensaje;
            txtErrorContrasena.Visibility = Visibility.Visible;
            MostrarBordeError(borderContrasena);
        }

        private void MostrarBordeError(System.Windows.Controls.Border border)
        {
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            border.BorderThickness = new Thickness(1.5);
        }
    }
}