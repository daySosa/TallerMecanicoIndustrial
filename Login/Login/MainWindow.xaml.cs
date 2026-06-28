using Login.Clases;
using Serilog;
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
        private bool _contrasenaVisible = false;
        private bool _procesandoLogin = false;

        private readonly clsConsultasBD _db = new clsConsultasBD();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        private static readonly SolidColorBrush BrushFocus = CrearBrush("#2563EB");
        private static readonly SolidColorBrush BrushError = CrearBrush("#f44336");
        private static readonly SolidColorBrush BrushNormal = CrearBrush("#FFFFFF", 0.12);
        private static readonly SolidColorBrush BrushTransparente = CrearBrushTransparente();

        private static SolidColorBrush CrearBrush(string hex, double? opacidad = null)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            if (opacidad.HasValue) brush.Opacity = opacidad.Value;
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush CrearBrushTransparente()
        {
            var brush = new SolidColorBrush(Colors.Transparent);
            brush.Freeze();
            return brush;
        }

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
            borderCorreo.BorderBrush = BrushFocus;
            borderCorreo.BorderThickness = new Thickness(2);
        }

        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
        {
            borderCorreo.BorderBrush = BrushTransparente;
            borderCorreo.BorderThickness = new Thickness(1.5);
        }

        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush = BrushFocus;
            borderContrasena.BorderThickness = new Thickness(2);
        }

        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e)
        {
            borderContrasena.BorderBrush = BrushTransparente;
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

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GuardarCredenciales(string correo, string contrasena)
        {
            try
            {
                var datos = new { Correo = correo, Contrasena = contrasena };
                string json = JsonSerializer.Serialize(datos);
                File.WriteAllText(_archivoRecordar, json);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudieron guardar las credenciales recordadas.");
            }
        }

        private void EliminarCredenciales()
        {
            try
            {
                if (File.Exists(_archivoRecordar))
                    File.Delete(_archivoRecordar);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudieron eliminar las credenciales recordadas.");
            }
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
            catch (Exception ex)
            {
                Log.Warning(ex, "No se pudieron cargar las credenciales recordadas.");
            }
        }

        /// <summary>
        /// Valida los datos ingresados y ejecuta el proceso de inicio de sesión de forma asíncrona,
        /// evitando que la UI se congele mientras se consulta la base de datos o se envía el correo 2FA.
        /// </summary>
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_procesandoLogin) return; // evita doble clic / doble validación

            bool hayError = false;

            string errorCorreo = clsValidaciones.ValidarCorreoLogin(txtCorreo.Text);
            if (errorCorreo != null)
            {
                txtErrorCorreo.Text = errorCorreo;
                txtErrorCorreo.Visibility = Visibility.Visible;
                borderCorreo.BorderBrush = BrushError;
                hayError = true;
            }
            else
            {
                txtErrorCorreo.Visibility = Visibility.Collapsed;
                borderCorreo.BorderBrush = BrushTransparente;
            }

            string contrasena = ObtenerContrasena();
            string errorContrasena = clsValidaciones.ValidarContrasenaLogin(contrasena);
            if (errorContrasena != null)
            {
                txtErrorContrasena.Text = errorContrasena;
                txtErrorContrasena.Visibility = Visibility.Visible;
                borderContrasena.BorderBrush = BrushError;
                hayError = true;
            }
            else
            {
                txtErrorContrasena.Visibility = Visibility.Collapsed;
                borderContrasena.BorderBrush = BrushTransparente;
            }

            if (hayError) return;

            string correo = txtCorreo.Text.Trim();

            _procesandoLogin = true;
            SetCargando(true, sender as System.Windows.Controls.Button);

            try
            {
                if (chkRecordar.IsChecked == true)
                    GuardarCredenciales(correo, contrasena);
                else
                    EliminarCredenciales();

                await IniciarSesionAsync(correo, contrasena);
            }
            finally
            {
                _procesandoLogin = false;
                SetCargando(false, sender as System.Windows.Controls.Button);
            }
        }

        /// <summary>
        /// Habilita/deshabilita el botón de login y cambia su texto mientras se procesa la autenticación.
        /// </summary>
        private void SetCargando(bool cargando, System.Windows.Controls.Button boton)
        {
            if (boton == null) return;
            boton.IsEnabled = !cargando;
            boton.Content = cargando ? "Verificando..." : "Iniciar Sesión";
        }

        /// <summary>
        /// Verifica las credenciales del usuario y ejecuta la autenticación en dos factores (2FA),
        /// corriendo la consulta a la base de datos y el envío de correo en un hilo de fondo
        /// para no bloquear la interfaz.
        /// </summary>
        private async System.Threading.Tasks.Task IniciarSesionAsync(string correo, string contrasena)
        {
            try
            {
                bool valido = await System.Threading.Tasks.Task.Run(
                    () => _db.ValidarLogin(correo, contrasena));

                if (!valido)
                {
                    txtErrorCorreo.Text = "⚠ Correo o contraseña incorrectos.";
                    txtErrorCorreo.Visibility = Visibility.Visible;
                    borderCorreo.BorderBrush = BrushError;
                    borderContrasena.BorderBrush = BrushError;
                    return;
                }

                clsAutenticacion autenticacion = new clsAutenticacion();

                string codigo2FA = await System.Threading.Tasks.Task.Run(
                    () => autenticacion.GenerarCodigo(correo));

                bool enviado = await System.Threading.Tasks.Task.Run(
                    () => autenticacion.EnviarCorreo(correo, codigo2FA));

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
            catch (Exception ex)
            {
                Log.Error(ex, "Error durante el proceso de inicio de sesión para {Correo}", correo);
                MessageBox.Show("Error al conectar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }
    }
}