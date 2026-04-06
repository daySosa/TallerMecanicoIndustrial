using Login.Clases;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana encargada del registro y actualización de clientes en el sistema.
    /// Permite agregar nuevos clientes y editar los existentes con validaciones de formulario.
    /// </summary>
    public partial class ClientesWindow : Window
    {
        /// <summary>
        /// DNI del cliente que se encuentra en edición actualmente.
        /// Permanece vacío cuando se está registrando un nuevo cliente.
        /// </summary>
        private string _dniEditando = string.Empty;

        /// <summary>
        /// Obtiene el cliente resultante luego de una operación exitosa de agregar o actualizar.
        /// </summary>
        public clsCliente ClienteResultado { get; private set; }

        /// <summary>
        /// Instancia para consultas a la base de datos.
        /// </summary>
        clsConsultasBD db = new clsConsultasBD();

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ClientesWindow"/>.
        /// Deshabilita el botón de actualizar al inicio, ya que se requiere cargar un cliente primero.
        /// </summary>
        public ClientesWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        /// <summary>
        /// Restringe el campo de DNI para aceptar únicamente caracteres numéricos.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>
        /// Restringe el campo de nombre para aceptar únicamente letras y espacios,
        /// incluyendo caracteres especiales del español.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtNombre_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");
        }

        /// <summary>
        /// Restringe el campo de apellido para aceptar únicamente letras y espacios,
        /// incluyendo caracteres especiales del español.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtApellido_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");
        }

        /// <summary>
        /// Restringe el campo de dirección para aceptar letras, números, espacios
        /// y caracteres de puntuación comunes como guiones, puntos, comas y paréntesis.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtDireccion_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\-\.\,\(\)\/]+$");
        }

        /// <summary>
        /// Restringe el campo de teléfono para aceptar únicamente dígitos
        /// y limita la entrada a un máximo de 8 números.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");
            if (soloNumeros.Length >= 8)
                e.Handled = true;
        }

        /// <summary>
        /// Maneja el evento de cambio de texto en el campo de teléfono.
        /// Aplica automáticamente el formato hondureño XXXX-XXXX y ajusta
        /// la posición del cursor tras el formateo.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de cambio de texto.</param>
        private void txtTelefono_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");
            if (soloNumeros.Length > 8) soloNumeros = soloNumeros.Substring(0, 8);

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
        /// Carga los datos de un cliente existente en el formulario para su edición.
        /// Deshabilita el botón de agregar y habilita el de actualizar.
        /// </summary>
        /// <param name="c">Instancia de <see cref="clsCliente"/> con los datos a cargar.</param>
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
        /// Maneja el evento de activación del toggle de estado del cliente.
        /// Actualiza el texto e ícono del indicador visual al estado activo (verde).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        /// <summary>
        /// Maneja el evento de desactivación del toggle de estado del cliente.
        /// Actualiza el texto e ícono del indicador visual al estado inactivo (rojo).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        /// <summary>
        /// Maneja el evento Click del botón Cancelar.
        /// Cierra la ventana sin guardar ningún cambio.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Ejecuta las validaciones del formulario antes de registrar o actualizar un cliente.
        /// Verifica campos obligatorios, formatos, longitudes y duplicados en la base de datos.
        /// </summary>
        /// <param name="dni">DNI ingresado en el formulario.</param>
        /// <param name="telefonoLimpio">Teléfono sin guiones ni espacios.</param>
        /// <param name="dniActual">
        /// DNI original del cliente en edición. Se envía vacío al registrar un nuevo cliente.
        /// </param>
        /// <returns><c>true</c> si todos los campos son válidos; de lo contrario, <c>false</c>.</returns>
        private bool ValidarCampos(string dni, string telefonoLimpio, string dniActual = "")
        {
            if (!clsValidacionesClientes.ValidarFormularioVacio(
                dni, txtNombre.Text, txtApellido.Text,
                telefonoLimpio, txtDireccion.Text)) return false;

            if (!clsValidacionesClientes.ValidarDNIHondureño(dni)) return false;
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtNombre.Text, "nombre")) return false;
            if (!clsValidacionesClientes.ValidarLongitudNombre(txtApellido.Text, "apellido")) return false;
            if (!clsValidaciones.Telefono(telefonoLimpio)) return false;
            if (!clsValidacionesClientes.ValidarLongitudCorreo(txtCorreo.Text)) return false;
            if (!clsValidacionesClientes.ValidarDireccion(txtDireccion.Text)) return false;
            if (!clsValidacionesClientes.ValidarDNINoDuplicado(dni, dniActual, db)) return false;
            if (!clsValidacionesClientes.ValidarTelefonoNoDuplicado(telefonoLimpio, dniActual, db)) return false;

            return true;
        }

        /// <summary>
        /// Maneja el evento Click del botón Agregar.
        /// Valida el formulario e inserta un nuevo cliente en la base de datos.
        /// Cierra la ventana con <see cref="Window.DialogResult"/> en <c>true</c> si la operación es exitosa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;

            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!ValidarCampos(txtDPI.Text.Trim(), telefonoLimpio, dniActual: ""))
            {
                btnAgregar.IsEnabled = true;
                return;
            }

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
        /// Maneja el evento Click del botón Actualizar.
        /// Valida el formulario y actualiza los datos del cliente cargado en la base de datos.
        /// Actualiza <see cref="_dniEditando"/> si el DNI fue modificado y cierra la ventana con
        /// <see cref="Window.DialogResult"/> en <c>true</c> si la operación es exitosa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_dniEditando))
            {
                MessageBox.Show("⚠ No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nuevoDni = txtDPI.Text.Trim();
            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!ValidarCampos(nuevoDni, telefonoLimpio, dniActual: _dniEditando)) return;

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