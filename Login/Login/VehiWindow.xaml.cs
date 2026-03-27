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
                    Obs = string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim(),
                    Activo = true
                };
                _db.GuardarOActualizarVehiculo(true, datos);
                MessageBox.Show("✅ Vehículo registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
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
                if (item.Content.ToString() == vehiculo.Vehiculo_Tipo) { cmbTipo.SelectedItem = item; break; }
            }
            toggleActivo.IsChecked = vehiculo.EstaActivo;
            btnGuardar.IsEnabled = false; btnGuardar.Opacity = 0.4;
            btnActualizar.IsEnabled = true; btnActualizar.Opacity = 1;
        }

        private bool ValidarCampos(out int año)
        {
            año = 0;

            if (!clsValidaciones.ValidarTextoRequerido(txtPlaca.Text, "placa del vehículo")) { txtPlaca.Focus(); return false; }
            if (!clsValidaciones.ValidarTextoRequerido(txtMarca.Text, "marca del vehículo")) { txtMarca.Focus(); return false; }
            if (!clsValidaciones.ValidarTextoRequerido(txtModelo.Text, "modelo del vehículo")) { txtModelo.Focus(); return false; }
            if (!clsValidaciones.ValidarTextoRequerido(txtAnio.Text, "año del vehículo")) { txtAnio.Focus(); return false; }
            if (!clsValidaciones.ValidarComboSeleccionado(cmbTipo.SelectedItem, "tipo de vehículo")) { cmbTipo.Focus(); return false; }

            if (!clsValidaciones.ValidarPlaca(txtPlaca.Text.Trim()))
            {
                txtPlaca.Focus();
                return false;
            }

            if (!clsValidaciones.ValidarAnio(txtAnio.Text, out año))
            {
                txtAnio.Focus();
                return false;
            }

            if (!clsValidaciones.ValidarTextoAlfanumerico(txtMarca.Text, "marca"))
            {
                txtMarca.Focus();
                return false;
            }

            if (!clsValidaciones.ValidarTextoAlfanumerico(txtModelo.Text, "modelo"))
            {
                txtModelo.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(_clienteDNI))
            {
                MessageBox.Show("⚠ Debes verificar el DNI del cliente antes de guardar.",
                    "Cliente requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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
            txtPlaca.Clear(); txtMarca.Clear(); txtModelo.Clear(); txtAnio.Clear(); txtObservaciones.Clear();
            cmbTipo.SelectedIndex = -1; toggleActivo.IsChecked = true; txtClienteDNI.Clear();
            borderClienteInfo.Visibility = Visibility.Collapsed;
            _placaSeleccionada = string.Empty;
            _clienteDNI = string.Empty;
        }
    }
}