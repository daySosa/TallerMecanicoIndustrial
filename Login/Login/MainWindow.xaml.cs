using Login.Clases;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Login
{
    public partial class MainWindow : Window
    {
        private bool _contrasenaVisible = false;
        private bool _procesandoLogin = false;

        private readonly clsConsultasBD _db = new clsConsultasBD();

        private readonly string _archivoRecordar =
            Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "OSM_remember.json");

        // ── Brushes estáticos (Freeze evita re-renders innecesarios) ─
        private static readonly SolidColorBrush BrushFocus = Pincel("#2563EB");
        private static readonly SolidColorBrush BrushError = Pincel("#f44336");
        private static readonly SolidColorBrush BrushNormal = Pincel("#FFFFFF", 0.12);
        private static readonly SolidColorBrush BrushVacio = PincelTransparente();

        private static SolidColorBrush Pincel(string hex, double? opacidad = null)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            if (opacidad.HasValue) brush.Opacity = opacidad.Value;
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush PincelTransparente()
        {
            var brush = new SolidColorBrush(Colors.Transparent);
            brush.Freeze();
            return brush;
        }

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public MainWindow()
        {
            InitializeComponent();
            CargarCredencialesRecordadas();
        }

        // ════════════════════════════════════════════════════════════
        // DRAG & FOCUS
        // ════════════════════════════════════════════════════════════

        private void Window_Drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void TxtCorreo_GotFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderCorreo, true);

        private void TxtCorreo_LostFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderCorreo, false);

        private void TxtContrasena_GotFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderContrasena, true);

        private void TxtContrasena_LostFocus(object sender, RoutedEventArgs e)
            => SetBorderFocus(borderContrasena, false);

        private static void SetBorderFocus(System.Windows.Controls.Border border, bool enfocado)
        {
            border.BorderBrush = enfocado ? BrushFocus : BrushVacio;
            border.BorderThickness = new Thickness(enfocado ? 2 : 1.5);
        }

        // ════════════════════════════════════════════════════════════
        // VER / OCULTAR CONTRASEÑA
        // ════════════════════════════════════════════════════════════

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

        private string ObtenerContrasena() =>
            _contrasenaVisible ? txtContrasenaVisible.Text : txtContrasena.Password;

        // ════════════════════════════════════════════════════════════
        // RECORDAR CREDENCIALES
        // ════════════════════════════════════════════════════════════

        private void GuardarCredenciales(string correo, string contrasena)
        {
            try
            {
                File.WriteAllText(_archivoRecordar,
                    JsonSerializer.Serialize(new { Correo = correo, Contrasena = contrasena }));
            }
            catch { /* fallo silencioso: no crítico */ }
        }

        private void EliminarCredenciales()
        {
            try { if (File.Exists(_archivoRecordar)) File.Delete(_archivoRecordar); }
            catch { }
        }

        private void CargarCredencialesRecordadas()
        {
            try
            {
                if (!File.Exists(_archivoRecordar)) return;

                var datos = JsonSerializer.Deserialize<JsonElement>(
                    File.ReadAllText(_archivoRecordar));

                txtCorreo.Text = datos.GetProperty("Correo").GetString() ?? "";
                txtContrasena.Password = datos.GetProperty("Contrasena").GetString() ?? "";
                chkRecordar.IsChecked = true;
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════════
        // LOGIN
        // ════════════════════════════════════════════════════════════

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (_procesandoLogin) return;

            // — Validaciones —
            bool hayError = false;

            string errorCorreo = clsValidaciones.ValidarCorreoLogin(txtCorreo.Text);
            MostrarError(txtErrorCorreo, borderCorreo, errorCorreo, ref hayError);

            string contrasena = ObtenerContrasena();
            string errorContrasena = clsValidaciones.ValidarContrasenaLogin(contrasena);
            MostrarError(txtErrorContrasena, borderContrasena, errorContrasena, ref hayError);

            if (hayError) return;

            string correo = txtCorreo.Text.Trim();
            var btnLogin = sender as System.Windows.Controls.Button;

            _procesandoLogin = true;
            SetCargando(true, btnLogin);

            try
            {
                if (chkRecordar.IsChecked == true) GuardarCredenciales(correo, contrasena);
                else EliminarCredenciales();

                await IniciarSesionAsync(correo, contrasena);
            }
            finally
            {
                _procesandoLogin = false;
                SetCargando(false, btnLogin);
            }
        }

        private void MostrarError(
            System.Windows.Controls.TextBlock txtError,
            System.Windows.Controls.Border border,
            string mensaje,
            ref bool hayError)
        {
            if (mensaje != null)
            {
                txtError.Text = mensaje;
                txtError.Visibility = Visibility.Visible;
                border.BorderBrush = BrushError;
                hayError = true;
            }
            else
            {
                txtError.Visibility = Visibility.Collapsed;
                border.BorderBrush = BrushVacio;
            }
        }

        private void SetCargando(bool cargando, System.Windows.Controls.Button boton)
        {
            if (boton == null) return;
            boton.IsEnabled = !cargando;
            boton.Content = cargando ? "Verificando..." : "Iniciar Sesión";
        }

        private async System.Threading.Tasks.Task IniciarSesionAsync(string correo, string contrasena)
        {
            try
            {
                bool valido = await System.Threading.Tasks.Task.Run(
                    () => _db.ValidarLogin(correo, contrasena));

                if (!valido)
                {
                    bool dummy = false;
                    MostrarError(txtErrorCorreo, borderCorreo, "⚠ Correo o contraseña incorrectos.", ref dummy);
                    MostrarError(txtErrorContrasena, borderContrasena, null, ref dummy);
                    borderContrasena.BorderBrush = BrushError;
                    return;
                }

                clsAutenticacion auth = new clsAutenticacion();

                string codigo2FA = await System.Threading.Tasks.Task.Run(
                    () => auth.GenerarCodigo(correo));

                bool enviado = await System.Threading.Tasks.Task.Run(
                    () => auth.EnviarCorreo(correo, codigo2FA));

                if (enviado)
                {
                    new OpcionSesion(correo).Show();
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
                MessageBox.Show("⚠ Error inesperado: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // OTROS
        // ════════════════════════════════════════════════════════════

        private void BtnOlvidoContrasena_Click(object sender, RoutedEventArgs e)
        {
            // Implementar recuperación de contraseña
        }

        private void txtCorreo_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            //Limpiar error en tiempo real si lo deseas:
            if (txtErrorCorreo.Visibility == Visibility.Visible)
                txtErrorCorreo.Visibility = Visibility.Collapsed;
        }
    }
}