#nullable enable
using Login.Clases;
using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana modal para crear o editar un usuario del sistema.
    /// Si se construye sin argumentos, opera en modo "creación"; si se le
    /// pasa un correo existente, carga esos datos y opera en modo "edición".
    /// Incluye una transición de fade-in al abrirse y fade-out al cerrarse.
    /// </summary>
    public partial class VentanaUsuario : Window
    {
        #region Constantes

        private const string TituloError = "Error";
        private const string TituloExito = "Éxito";
        private const string TituloCampoRequerido = "Campo requerido";

        /// <summary>Duración de las transiciones de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private string? _usuarioEmail;
        private readonly bool _esEdicion;

        /// <summary>
        /// Evita el reingreso a Window_Closing: la primera vez se cancela el cierre
        /// y se dispara el fade-out; al completarse la animación se vuelve a llamar
        /// a Close() con esta bandera en true para dejarlo cerrar de verdad.
        /// </summary>
        private bool _cerrandoConAnimacion;

        #endregion

        #region Constructores

        /// <summary>Crea la ventana en modo "Nuevo Usuario".</summary>
        public VentanaUsuario()
        {
            InitializeComponent();
            _esEdicion = false;
        }

        /// <summary>Crea la ventana en modo "Editar Usuario", precargando los datos del correo indicado.</summary>
        /// <param name="usuarioEmail">Correo (clave primaria) del usuario a editar.</param>
        public VentanaUsuario(string usuarioEmail) : this()
        {
            _usuarioEmail = usuarioEmail;
            _esEdicion = true;
            CargarDatosParaEditar(usuarioEmail);
        }

        #endregion

        #region Ciclo de vida y transición de entrada/salida

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>Aplica un fade-in suave al mostrar la ventana (entra con Opacity="0" desde XAML).</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Intercepta el cierre de la ventana para reproducir un fade-out antes de cerrar
        /// de verdad. La primera vez cancela el cierre y lanza la animación; al terminar,
        /// se vuelve a invocar Close() con la bandera activada para completar el cierre
        /// sin volver a interceptarlo.
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_cerrandoConAnimacion) return;

            e.Cancel = true;
            _cerrandoConAnimacion = true;

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>Libera la conexión a la base de datos al cerrar la ventana.</summary>
        private void Window_Closed(object? sender, EventArgs e)
        {
            (_db as IDisposable)?.Dispose();
        }

        #endregion

        #region Toggle de estado (Activo/Inactivo)

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoUsuarioLabel == null) return;
            txtEstadoUsuarioLabel.Text = "El usuario está activo";
            txtEstadoUsuarioLabel.Foreground = Pincel("#4CAF50");
            txtEstadoUsuarioSub.Text = "Tiene acceso al sistema";
            iconEstadoUsuario.Foreground = Pincel("#4CAF50");

#pragma warning disable CA1416
            iconEstadoUsuario.Kind = PackIconKind.CheckCircleOutline;
#pragma warning disable CA1416
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoUsuarioLabel == null) return;
            txtEstadoUsuarioLabel.Text = "El usuario está inactivo";
            txtEstadoUsuarioLabel.Foreground = Pincel("#f44336");
            txtEstadoUsuarioSub.Text = "Sin acceso al sistema";
            iconEstadoUsuario.Foreground = Pincel("#f44336");
            iconEstadoUsuario.Kind = PackIconKind.CloseCircleOutline;
        }

        #endregion

        #region Carga de datos para edición

        /// <summary>
        /// Precarga en el formulario los datos del usuario correspondiente al correo indicado
        /// y adapta la ventana al modo edición (correo y contraseña bloqueados, títulos y
        /// texto del botón actualizados).
        /// </summary>
        /// <param name="email">Correo del usuario a cargar.</param>
        private void CargarDatosParaEditar(string email)
        {
            try
            {
                DataRow? fila = _db.ObtenerUsuarioPorEmail(email);
                if (fila == null)
                {
                    MessageBox.Show("No se encontró el usuario indicado.", TituloError,
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                txtNombre.Text = fila["Usuario_Nombre"]?.ToString() ?? string.Empty;
                txtApellido.Text = fila["Usuario_Apellido"]?.ToString() ?? string.Empty;
                txtCorreo.Text = fila["Usuario_Email"]?.ToString() ?? string.Empty;
                txtCorreo.IsEnabled = false; // el correo es la PK, no se edita
                txtTelefono.Text = fila["Usuario_Telefono"]?.ToString() ?? string.Empty;

                string rol = fila["Usuario_Rol"]?.ToString() ?? string.Empty;
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

                txtContrasena.IsEnabled = false;
                txtContrasena.ToolTip = "La contraseña no se puede modificar desde aquí";
                tbTituloVentana.Text = "Editar Usuario";
                tbSubtitulo.Text = "Modifica los datos del empleado";
                iconAvatar.Kind = PackIconKind.AccountEdit;
                btnGuardar.Content = "Actualizar Usuario";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuario: " + ex.Message, TituloError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Guardar / Cancelar

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                string nombre = txtNombre.Text.Trim();
                string apellido = txtApellido.Text.Trim();
                string correo = txtCorreo.Text.Trim();
                string telefono = txtTelefono.Text.Trim();
                string rol = (cmbRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
                bool activo = toggleActivo.IsChecked == true;
                string password = txtContrasena.Password;

                if (_esEdicion)
                    GuardarEdicion(nombre, apellido, telefono, rol, activo, password);
                else
                    GuardarNuevo(nombre, apellido, correo, telefono, rol, password);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message, TituloError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Persiste los cambios de un usuario existente y registra la acción en bitácora.</summary>
        private void GuardarEdicion(string nombre, string apellido, string telefono,
            string rol, bool activo, string password)
        {
            _db.ActualizarUsuario(_usuarioEmail!, nombre, apellido, telefono, rol);
            _db.CambiarEstadoUsuario(_usuarioEmail!, activo);

            _db.RegistrarBitacora(SesionActual.Email, "Usuarios", "Actualizar",
                $"Usuario {_usuarioEmail} - Rol: {rol}{(activo ? "" : " (Desactivado)")}"
                + (!string.IsNullOrWhiteSpace(password) ? " · Contraseña cambiada" : ""));

            MessageBox.Show("✅ Usuario actualizado correctamente.", TituloExito,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>Registra un usuario nuevo y la acción correspondiente en bitácora.</summary>
        private void GuardarNuevo(string nombre, string apellido, string correo,
            string telefono, string rol, string password)
        {
            _db.AgregarUsuario(nombre, apellido, correo, telefono, rol, password);

            _db.RegistrarBitacora(SesionActual.Email, "Usuarios", "Agregar",
                $"Usuario {correo} - Rol: {rol}");

            MessageBox.Show("✅ Usuario registrado correctamente.", TituloExito,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region Validaciones

        /// <summary>
        /// Valida todos los campos del formulario. Cada validación fallida informa al
        /// usuario con un mensaje concreto y coloca el foco en el campo correspondiente.
        /// </summary>
        private bool ValidarCampos()
        {
            if (!ValidadorUsuario.ValidarNombre(txtNombre.Text))
            {
                Aviso("Ingresa un nombre válido.");
                txtNombre.Focus();
                return false;
            }

            if (!ValidadorUsuario.ValidarApellido(txtApellido.Text))
            {
                Aviso("Ingresa un apellido válido.");
                txtApellido.Focus();
                return false;
            }

            if (!ValidadorUsuario.ValidarCorreo(txtCorreo.Text))
            {
                Aviso("Ingresa un correo electrónico válido.");
                txtCorreo.Focus();
                return false;
            }

            if (!ValidadorUsuario.ValidarTelefono(txtTelefono.Text))
            {
                Aviso("Ingresa un número de teléfono válido.");
                txtTelefono.Focus();
                return false;
            }

            if (!ValidadorUsuario.ValidarRolSeleccionado(cmbRol.SelectedItem))
            {
                Aviso("Selecciona un rol para el usuario.");
                cmbRol.Focus();
                return false;
            }

            // La contraseña solo es obligatoria al crear un usuario nuevo;
            // en edición el campo está deshabilitado y se ignora.
            if (!_esEdicion && string.IsNullOrWhiteSpace(txtContrasena.Password))
            {
                Aviso("Ingresa una contraseña para el nuevo usuario.");
                txtContrasena.Focus();
                return false;
            }

            return true;
        }

        #endregion

        #region Helpers

        /// <summary>Muestra un aviso estándar de campo requerido/incompleto.</summary>
        private static void Aviso(string mensaje) =>
            MessageBox.Show(mensaje, TituloCampoRequerido, MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>Crea un <see cref="SolidColorBrush"/> a partir de un color hexadecimal.</summary>
        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        #endregion
    }
}