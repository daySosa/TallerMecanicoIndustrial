using Login.Clases;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana encargada de gestionar el registro y actualización de clientes.
    /// Incluye validaciones de entrada, formateo automático y control de estado del cliente.
    /// </summary>
    public partial class ClientesWindow : Window
    {
        /// <summary>
        /// DNI del cliente que se encuentra en edición.
        /// </summary>
        private string _dniEditando = string.Empty;

        /// <summary>
        /// Objeto que almacena el cliente registrado o actualizado.
        /// </summary>
        public clsCliente ClienteResultado { get; private set; }

        /// <summary>
        /// Instancia para realizar operaciones en la base de datos.
        /// </summary>
        clsConsultasBD db = new clsConsultasBD();

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ClientesWindow"/>.
        /// </summary>
        public ClientesWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        /// <summary>
        /// Restringe la entrada del DNI a únicamente valores numéricos.
        /// </summary>
        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>
        /// Restringe la entrada del teléfono a números y limita su longitud a 8 dígitos.
        /// </summary>
        private void txtTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");
            if (soloNumeros.Length >= 8)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Formatea automáticamente el número telefónico en el formato ####-####.
        /// </summary>
        private void txtTelefono_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");

            if (soloNumeros.Length > 8)
                soloNumeros = soloNumeros.Substring(0, 8);

            // CORRECCIÓN: se eliminó la primera declaración duplicada de formateado
            // y se agregó el caso vacío que faltaba en la primera versión
            string formateado;

            if (soloNumeros.Length == 0)
                formateado = "";
            else if (soloNumeros.Length <= 4)
                formateado = soloNumeros;
            else
                formateado = soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4);

            int caretPos = txtTelefono.CaretIndex;
            txtTelefono.Text = formateado;

            int nuevosCaret = caretPos;
            if (nuevosCaret > 4) nuevosCaret = Math.Min(nuevosCaret + 1, formateado.Length);
            else nuevosCaret = Math.Min(nuevosCaret, formateado.Length);

            txtTelefono.CaretIndex = nuevosCaret;

            txtTelefono.TextChanged += txtTelefono_TextChanged;
        }

        /// <summary>
        /// Carga los datos de un cliente en la interfaz para su edición.
        /// </summary>
        public void CargarClienteParaEditar(clsCliente c)
        {
            _dniEditando = c.Cliente_DPI;
            txtDPI.Text = c.Cliente_DPI;
            txtDPI.IsReadOnly = false;
            txtNombre.Text = c.Cliente_Nombre;
            txtApellido.Text = c.Cliente_Apellido;

            string soloNumeros = Regex.Replace(c.Cliente_Telefono, @"\D", "");
            if (soloNumeros.Length > 8) soloNumeros = soloNumeros.Substring(0, 8);
            txtTelefono.Text = soloNumeros.Length == 8
                ? soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4)
                : soloNumeros;

            txtCorreo.Text = c.Cliente_Correo;
            txtDireccion.Text = c.Cliente_Direccion;
            toggleActivo.IsChecked = c.Cliente_Activo;

            btnAgregar.IsEnabled = false;
            btnAgregar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
        }

        /// <summary>
        /// Cambia el estado visual del cliente a activo.
        /// </summary>
        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        /// <summary>
        /// Cambia el estado visual del cliente a inactivo.
        /// </summary>
        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        /// <summary>
        /// Cierra la ventana sin realizar ninguna acción.
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Valida los datos ingresados y registra un nuevo cliente en la base de datos.
        /// </summary>
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;

            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            // CORRECCIÓN: se eliminaron las líneas duplicadas y las que no rehabilitaban
            // el botón antes del return. Ahora todas siguen el mismo patrón consistente.
            if (!clsValidaciones.ValidarDNIHondureño(txtDPI.Text.Trim())) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del cliente")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarTextoRequerido(txtApellido.Text, "apellido del cliente")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarSoloLetras(txtNombre.Text, "nombre")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarSoloLetras(txtApellido.Text, "apellido")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.Telefono(telefonoLimpio)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarTelefono(telefonoLimpio, 8)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarCorreo(txtCorreo.Text)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtNombre.Text, "nombre")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtApellido.Text, "apellido")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidacionesClientes.ValidarLongitudCorreo(txtCorreo.Text)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidacionesClientes.ValidarDireccion(txtDireccion.Text)) { btnAgregar.IsEnabled = true; return; }

            try
            {
                bool insertado = db.AgregarCliente(
                    txtDPI.Text.Trim(),
                    txtNombre.Text.Trim(),
                    txtApellido.Text.Trim(),
                    telefonoLimpio,
                    txtCorreo.Text.Trim(),
                    txtDireccion.Text.Trim()
                );

                if (!insertado)
                {
                    MessageBox.Show("Ya existe un cliente con ese DNI.",
                        "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnAgregar.IsEnabled = true;
                    return;
                }

                ClienteResultado = new clsCliente
                {
                    Cliente_DPI = txtDPI.Text.Trim(),
                    Cliente_Nombre = txtNombre.Text.Trim(),
                    Cliente_Apellido = txtApellido.Text.Trim(),
                    Cliente_Telefono = telefonoLimpio,
                    Cliente_Correo = txtCorreo.Text.Trim(),
                    Cliente_Direccion = txtDireccion.Text.Trim(),
                    Cliente_Activo = true
                };

                MessageBox.Show("✅ Cliente guardado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                btnAgregar.IsEnabled = true;
                MessageBox.Show("⚠ Error al agregar cliente:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Valida los datos ingresados y actualiza la información de un cliente existente.
        /// </summary>
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_dniEditando))
            {
                MessageBox.Show("⚠ No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // CORRECCIÓN: se eliminó la segunda declaración duplicada de telefonoLimpio
            // y nuevoDni, y se eliminaron las validaciones repetidas que quedaban abajo
            string nuevoDni = txtDPI.Text.Trim();
            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!clsValidaciones.ValidarDNIHondureño(nuevoDni)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del cliente")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtApellido.Text, "apellido del cliente")) return;
            if (!clsValidaciones.ValidarSoloLetras(txtNombre.Text, "nombre")) return;
            if (!clsValidaciones.ValidarSoloLetras(txtApellido.Text, "apellido")) return;
            if (!clsValidaciones.Telefono(telefonoLimpio)) return;
            if (!clsValidaciones.ValidarTelefono(telefonoLimpio, 8)) return;
            if (!clsValidaciones.ValidarCorreo(txtCorreo.Text)) return;
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtNombre.Text, "nombre")) return;
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtApellido.Text, "apellido")) return;
            if (!clsValidacionesClientes.ValidarLongitudCorreo(txtCorreo.Text)) return;
            if (!clsValidacionesClientes.ValidarDireccion(txtDireccion.Text)) return;

            try
            {
                db.ActualizarCliente(
                    _dniEditando,
                    txtNombre.Text.Trim(),
                    txtApellido.Text.Trim(),
                    telefonoLimpio,
                    txtCorreo.Text.Trim(),
                    txtDireccion.Text.Trim(),
                    toggleActivo.IsChecked == true,
                    nuevoDni
                );

                _dniEditando = nuevoDni;

                MessageBox.Show("✅ Cliente actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}