using Login.Clases;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazClientes
{
    public partial class ClientesWindow : Window
    {
        private string _dniEditando = string.Empty;
        public clsCliente ClienteResultado { get; private set; }
        clsConsultasBD db = new clsConsultasBD();

        public ClientesWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtNombre_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");
        }

        private void txtApellido_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");
        }

        private void txtDireccion_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\-\.\(\)\/]+$");
        }


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

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        private bool ValidarCampos(string dni, string telefonoLimpio)
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

            return true;
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;

            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!ValidarCampos(txtDPI.Text.Trim(), telefonoLimpio))
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

            if (!ValidarCampos(nuevoDni, telefonoLimpio)) return;

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