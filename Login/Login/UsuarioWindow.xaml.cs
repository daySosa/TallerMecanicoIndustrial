using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazClientes
{
    public partial class VentanaUsuario : Window
    {
        private readonly clsConsultasBD _db = new();
        private string _usuarioEmail = null;
        private bool _esEdicion = false;

        // ── CONSTRUCTORES ────────────────────────────────────────────

        public VentanaUsuario()
        {
            InitializeComponent();
        }

        public VentanaUsuario(string usuarioEmail) : this()
        {
            _usuarioEmail = usuarioEmail;
            _esEdicion = true;
            CargarDatosParaEditar(usuarioEmail);
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── TOGGLE ESTADO ────────────────────────────────────────────

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoUsuarioLabel == null) return;
            txtEstadoUsuarioLabel.Text = "El usuario está activo";
            txtEstadoUsuarioLabel.Foreground = Pincel("#4CAF50");
            txtEstadoUsuarioSub.Text = "Tiene acceso al sistema";
            iconEstadoUsuario.Foreground = Pincel("#4CAF50");
            iconEstadoUsuario.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoUsuarioLabel == null) return;
            txtEstadoUsuarioLabel.Text = "El usuario está inactivo";
            txtEstadoUsuarioLabel.Foreground = Pincel("#f44336");
            txtEstadoUsuarioSub.Text = "Sin acceso al sistema";
            iconEstadoUsuario.Foreground = Pincel("#f44336");
            iconEstadoUsuario.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        // ── CARGAR PARA EDITAR ───────────────────────────────────────

        private void CargarDatosParaEditar(string email)
        {
            try
            {
                DataRow fila = _db.ObtenerUsuarioPorEmail(email);
                if (fila == null) return;

                txtNombre.Text = fila["Usuario_Nombre"].ToString();
                txtApellido.Text = fila["Usuario_Apellido"].ToString();
                txtCorreo.Text = fila["Usuario_Email"].ToString();
                txtCorreo.IsEnabled = false; // el correo es la PK, no se edita
                txtTelefono.Text = fila["Usuario_Telefono"]?.ToString() ?? string.Empty;

                string rol = fila["Usuario_Rol"].ToString() ?? string.Empty;
                foreach (ComboBoxItem item in cmbRol.Items)
                {
                    if (item.Content?.ToString() == rol)
                    {
                        cmbRol.SelectedItem = item;
                        break;
                    }
                }

                toggleActivo.IsChecked = fila["Usuario_Activo"] != DBNull.Value
                                          && (bool)fila["Usuario_Activo"];

                txtContrasena.ToolTip = "Deja en blanco para no cambiar la contraseña";
                tbTituloVentana.Text = "Editar Usuario";
                tbSubtitulo.Text = "Modifica los datos del empleado";
                iconAvatar.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountEdit;
                btnGuardar.Content = "Actualizar Usuario";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuario: " + ex.Message);
            }
        }

        // ── GUARDAR ──────────────────────────────────────────────────

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                string nombre = txtNombre.Text.Trim();
                string apellido = txtApellido.Text.Trim();
                string correo = txtCorreo.Text.Trim();
                string telefono = txtTelefono.Text.Trim();
                string rol = (cmbRol.SelectedItem as ComboBoxItem)
                                       ?.Content?.ToString() ?? string.Empty;
                bool activo = toggleActivo.IsChecked == true;
                string password = txtContrasena.Password;

                if (_esEdicion)
                {
                    _db.ActualizarUsuario(_usuarioEmail, nombre, apellido, telefono, rol);
                    _db.CambiarEstadoUsuario(_usuarioEmail, activo);

                    if (!string.IsNullOrWhiteSpace(password))
                        _db.CambiarContrasenaUsuario(_usuarioEmail, password);

                    MessageBox.Show("✅ Usuario actualizado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _db.AgregarUsuario(nombre, apellido, correo, telefono, rol, password);
                    MessageBox.Show("✅ Usuario registrado correctamente.", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        // ── CANCELAR ─────────────────────────────────────────────────

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        // ── VALIDACIONES ─────────────────────────────────────────────

        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            { Aviso("El nombre es requerido."); txtNombre.Focus(); return false; }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            { Aviso("El apellido es requerido."); txtApellido.Focus(); return false; }

            if (string.IsNullOrWhiteSpace(txtCorreo.Text) || !txtCorreo.Text.Contains('@'))
            { Aviso("Ingresa un correo válido."); txtCorreo.Focus(); return false; }

            if (!_esEdicion && string.IsNullOrWhiteSpace(txtContrasena.Password))
            { Aviso("La contraseña es requerida para nuevos usuarios."); return false; }

            if (cmbRol.SelectedItem == null)
            { Aviso("Selecciona un rol."); cmbRol.Focus(); return false; }

            return true;
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private static void Aviso(string msg) =>
            MessageBox.Show(msg, "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}