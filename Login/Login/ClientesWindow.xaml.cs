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

        private void txtTelefono_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");

            if (soloNumeros.Length > 8)
                soloNumeros = soloNumeros.Substring(0, 8);

<<<<<<< HEAD
            string formateado = "";

            if (soloNumeros.Length <= 4)
                formateado = soloNumeros;
            else
                formateado = soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4);
=======
            string formateado;
>>>>>>> 50570af77cd41100b96c7dfa9ee9d9a0b02f2f56

            if (soloNumeros.Length == 0)
                formateado = "";
            else if (soloNumeros.Length <= 4)
                formateado = soloNumeros;
            else
                formateado = soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4);

            int caretPos = txtTelefono.CaretIndex;
            txtTelefono.Text = formateado;
<<<<<<< HEAD
            txtTelefono.CaretIndex = txtTelefono.Text.Length;
=======

            int nuevosCaret = caretPos;
            if (nuevosCaret > 4) nuevosCaret = Math.Min(nuevosCaret + 1, formateado.Length);
            else nuevosCaret = Math.Min(nuevosCaret, formateado.Length);

            txtTelefono.CaretIndex = nuevosCaret;
>>>>>>> 50570af77cd41100b96c7dfa9ee9d9a0b02f2f56

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

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;

            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!clsValidaciones.ValidarDNIHondureño(txtDPI.Text.Trim())) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del cliente")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarTextoRequerido(txtApellido.Text, "apellido del cliente")) { btnAgregar.IsEnabled = true; return; }
<<<<<<< HEAD
            if (!clsValidaciones.Telefono(telefonoLimpio)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarSoloLetras(txtNombre.Text, "nombre")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarSoloLetras(txtApellido.Text, "apellido")) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarCorreo(txtCorreo.Text)) { btnAgregar.IsEnabled = true; return; }
=======
            if (!clsValidaciones.ValidarTelefono(telefonoLimpio, 8)) { btnAgregar.IsEnabled = true; return; }
            if (!clsValidaciones.ValidarSoloLetras(txtNombre.Text, "nombre")) return;
            if (!clsValidaciones.ValidarSoloLetras(txtApellido.Text, "apellido")) return;
            if (!clsValidaciones.ValidarCorreo(txtCorreo.Text)) return;
            if (!clsValidaciones.Telefono(telefonoLimpio))
            {
                btnAgregar.IsEnabled = true;
                return;
            }
>>>>>>> 50570af77cd41100b96c7dfa9ee9d9a0b02f2f56

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

<<<<<<< HEAD
            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del cliente")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtApellido.Text, "apellido del cliente")) return;
=======
            string nuevoDni = txtDPI.Text.Trim();
            string telefonoLimpio = txtTelefono.Text.Replace("-", "").Trim();

            if (!clsValidaciones.ValidarDNIHondureño(nuevoDni)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del cliente")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtApellido.Text, "apellido del cliente")) return;
            if (!clsValidaciones.ValidarTelefono(telefonoLimpio, 8)) return;
>>>>>>> 50570af77cd41100b96c7dfa9ee9d9a0b02f2f56
            if (!clsValidaciones.Telefono(telefonoLimpio)) return;
            if (!clsValidaciones.ValidarSoloLetras(txtNombre.Text, "nombre")) return;
            if (!clsValidaciones.ValidarSoloLetras(txtApellido.Text, "apellido")) return;
            if (!clsValidaciones.ValidarCorreo(txtCorreo.Text)) return;

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