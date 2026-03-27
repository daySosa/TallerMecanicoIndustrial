using Login.Clases;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Vehículos
{
    public class Vehiculo : INotifyPropertyChanged
    {
        public string? Vehiculo_Placa { get; set; }
        public string? Vehiculo_Marca { get; set; }
        public string? Vehiculo_Modelo { get; set; }
        public int Vehiculo_Año { get; set; }
        public string? Vehiculo_Tipo { get; set; }
        public string? Vehiculo_Observaciones { get; set; }
        public string Cliente_DNI { get; set; }
        public string? Cliente_NombreCompleto { get; set; }
        public bool EstaActivo { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class VehiWindow : Window
    {
        private clsConsultasBD _db = new clsConsultasBD();
        private string _placaSeleccionada = string.Empty;
        private string _clienteDNI = string.Empty;

        public VehiWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        private void txtClienteDNI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtPlaca_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");
        }

        private void txtClienteDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (borderClienteInfo != null && txtClienteDNI.IsFocused && txtClienteDNI.Text != _clienteDNI)
            {
                borderClienteInfo.Visibility = Visibility.Collapsed;
                _clienteDNI = string.Empty;
            }
        }

        private bool ValidarDNIHondureño(string dni) => Regex.IsMatch(dni, @"^\d{13}$");

        public void EstablecerCliente(int clienteDNI)
        {
            txtClienteDNI.Text = clienteDNI.ToString();
            VerificarClienteEnBD(clienteDNI.ToString());
        }

        private void BtnVerificarCliente_Click(object sender, RoutedEventArgs e)
        {
            string dni = txtClienteDNI.Text.Trim();

            if (!clsValidaciones.ValidarDNIHondureño(dni))
            {
                MostrarClienteError("El DNI debe tener exactamente 13 dígitos numéricos.");
                return;
            }

            VerificarClienteEnBD(dni);
        }

        private void VerificarClienteEnBD(string dni)
        {
            try
            {
                var res = _db.VerificarClienteDNI(dni);
                if (res.existe)
                {
                    _clienteDNI = dni;
                    MostrarClienteOk(res.nombre);
                }
                else
                {
                    _clienteDNI = string.Empty;
                    MostrarClienteError($"No existe ningún cliente con DNI {dni}.");
                }
            }
            catch (Exception ex)
            {
                _clienteDNI = string.Empty;
                MostrarClienteError(ex.Message);
            }
        }

        private void MostrarClienteOk(string nombreCompleto)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountCheck;
            iconClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            txtClienteNombre.Text = nombreCompleto;
            txtClienteEstado.Text = "✔ Cliente verificado correctamente";
            txtClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void MostrarClienteError(string mensaje)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountAlert;
            iconClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            txtClienteNombre.Text = mensaje;
            txtClienteEstado.Text = "✘ Cliente no encontrado";
            txtClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out int año)) return;

            try
            {
                var datos = new
                {
                    Placa = txtPlaca.Text.Trim().ToUpper(),
                    DNI = _clienteDNI,
                    Marca = txtMarca.Text.Trim(),
                    Modelo = txtModelo.Text.Trim(),
                    Anio = año,
                    Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Obs = string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                ? (object)DBNull.Value
                                : txtObservaciones.Text.Trim(),
                    Activo = true
                };

                _db.GuardarOActualizarVehiculo(true, datos);
                MessageBox.Show("✅ Vehículo registrado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private bool ValidarCampos(out int año)
        {
            año = 0;

            // ── 1. PLACA ────────────────────────────────────────────────────
            string placa = txtPlaca.Text.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(placa))
            {
                MessageBox.Show("⚠ La placa del vehículo es obligatoria.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlaca.Focus(); return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(placa, @"^[A-Z0-9]+$"))
            {
                MessageBox.Show("⚠ La placa solo puede contener letras y números.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlaca.Focus(); return false;
            }

            if (placa.Length < 5 || placa.Length > 8)
            {
                MessageBox.Show("⚠ La placa debe tener entre 5 y 8 caracteres.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlaca.Focus(); return false;
            }

            bool formatoTurismo = System.Text.RegularExpressions.Regex.IsMatch(placa, @"^[A-Z]{3}\d{4}$");
            bool formatoMoto = System.Text.RegularExpressions.Regex.IsMatch(placa, @"^[A-Z]{1,2}\d{4}$");
            bool formatoCamion = System.Text.RegularExpressions.Regex.IsMatch(placa, @"^[A-Z]{1,3}\d{3,4}[A-Z]?$");

            if (!formatoTurismo && !formatoMoto && !formatoCamion)
            {
                MessageBox.Show(
                    "⚠ Formato de placa no reconocido.\n\n" +
                    "Formatos válidos:\n" +
                    "  • Turismo / Pickup:   ABC1234\n" +
                    "  • Motocicleta:        A1234  o  AB1234\n" +
                    "  • Camiones:           ABC1234A",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPlaca.Focus(); return false;
            }

            // ── 2. MARCA ────────────────────────────────────────────────────
            string marca = txtMarca.Text.Trim();

            if (string.IsNullOrWhiteSpace(marca))
            {
                MessageBox.Show("⚠ La marca del vehículo es obligatoria.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMarca.Focus(); return false;
            }

            if (marca.Length > 50)
            {
                MessageBox.Show("⚠ La marca no puede superar los 50 caracteres.",
                    "Marca inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMarca.Focus(); return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(marca, @"^[a-zA-Z0-9\s\-\.]+$"))
            {
                MessageBox.Show("⚠ La marca solo puede contener letras, números, espacios, guiones y puntos.",
                    "Marca inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMarca.Focus(); return false;
            }

            // ── 3. MODELO ───────────────────────────────────────────────────
            string modelo = txtModelo.Text.Trim();

            if (string.IsNullOrWhiteSpace(modelo))
            {
                MessageBox.Show("⚠ El modelo del vehículo es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtModelo.Focus(); return false;
            }

            if (modelo.Length > 80)
            {
                MessageBox.Show("⚠ El modelo no puede superar los 80 caracteres.",
                    "Modelo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtModelo.Focus(); return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(modelo, @"^[a-zA-Z0-9\s\-\.\(\)\/]+$"))
            {
                MessageBox.Show("⚠ El modelo contiene caracteres no permitidos.",
                    "Modelo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtModelo.Focus(); return false;
            }

            // ── 4. AÑO ──────────────────────────────────────────────────────
            string anioTexto = txtAnio.Text.Trim();

            if (string.IsNullOrWhiteSpace(anioTexto))
            {
                MessageBox.Show("⚠ El año del vehículo es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAnio.Focus(); return false;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(anioTexto, @"^\d{4}$"))
            {
                MessageBox.Show("⚠ El año debe ser exactamente 4 dígitos numéricos.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAnio.Focus(); return false;
            }

            if (!int.TryParse(anioTexto, out año))
            {
                MessageBox.Show("⚠ El año ingresado no es válido.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAnio.Focus(); return false;
            }

            int añoActual = DateTime.Now.Year;
            if (año < 1900 || año > añoActual + 1)
            {
                MessageBox.Show($"⚠ El año debe estar entre 1900 y {añoActual + 1}.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAnio.Focus(); return false;
            }

            // ── 5. TIPO ──────────────────────────────────────────────────────
            if (cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("⚠ Debes seleccionar el tipo de vehículo.",
                    "Tipo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbTipo.Focus(); return false;
            }

            // ── 6. OBSERVACIONES ─────────────────────────────────────────────
            if (!string.IsNullOrEmpty(txtObservaciones.Text) && txtObservaciones.Text.Length > 500)
            {
                MessageBox.Show("⚠ Las observaciones no pueden superar los 500 caracteres.",
                    "Texto demasiado largo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtObservaciones.Focus(); return false;
            }

            // ── 7. CLIENTE DNI VERIFICADO ─────────────────────────────────────
            // ESTE ES EL QUE DETIENE EL ERROR DEL SP
            if (string.IsNullOrWhiteSpace(_clienteDNI))
            {
                MessageBox.Show("⚠ Debes buscar y verificar el DNI del cliente antes de guardar.\n\n" +
                                "Ingresa el DNI y presiona el botón 'Buscar'.",
                    "Cliente no verificado", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClienteDNI.Focus(); return false;
            }

            return true;
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_placaSeleccionada))
            {
                MessageBox.Show("No hay ningún vehículo cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidarCampos(out int año)) return;

            try
            {
                var datos = new
                {
                    Placa = txtPlaca.Text.Trim().ToUpper(),
                    DNI = _clienteDNI,
                    Marca = txtMarca.Text.Trim(),
                    Modelo = txtModelo.Text.Trim(),
                    Anio = año,
                    Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Obs = string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim(),
                    Activo = toggleActivo.IsChecked == true
                };

                _db.GuardarOActualizarVehiculo(false, datos, _placaSeleccionada);

                MessageBox.Show("✅ Vehículo actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            if (iconEstado != null) iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            if (iconEstado != null) iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
        }

        public void CargarVehiculoParaEditar(Vehiculo vehiculo)
        {
            _placaSeleccionada = vehiculo.Vehiculo_Placa;

            txtPlaca.Text = vehiculo.Vehiculo_Placa;
            txtMarca.Text = vehiculo.Vehiculo_Marca;
            txtModelo.Text = vehiculo.Vehiculo_Modelo;
            txtAnio.Text = vehiculo.Vehiculo_Año.ToString();
            txtObservaciones.Text = vehiculo.Vehiculo_Observaciones;
            txtClienteDNI.Text = vehiculo.Cliente_DNI;
            _clienteDNI = vehiculo.Cliente_DNI;
            MostrarClienteOk(vehiculo.Cliente_NombreCompleto);

            foreach (ComboBoxItem item in cmbTipo.Items)
            {
                if (item.Content.ToString() == vehiculo.Vehiculo_Tipo)
                {
                    cmbTipo.SelectedItem = item;
                    break;
                }
            }

            toggleActivo.IsChecked = vehiculo.EstaActivo;
            btnGuardar.IsEnabled = false;
            btnGuardar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
        }

        private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPlaca.IsReadOnly) return;
            int caret = txtPlaca.CaretIndex;
            txtPlaca.Text = txtPlaca.Text.ToUpper();
            txtPlaca.CaretIndex = caret;
        }

        private void LimpiarFormulario()
        {
            txtPlaca.Clear();
            txtMarca.Clear();
            txtModelo.Clear();
            txtAnio.Clear();
            txtObservaciones.Clear();
            cmbTipo.SelectedIndex = -1;
            toggleActivo.IsChecked = true;
            txtClienteDNI.Clear();
            borderClienteInfo.Visibility = Visibility.Collapsed;
            _placaSeleccionada = string.Empty;
            _clienteDNI = string.Empty;
        }
    }
}