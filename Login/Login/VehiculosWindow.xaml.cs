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
        public string Cliente_DNI { get; set; } = string.Empty;
        public string? Cliente_NombreCompleto { get; set; }
        public bool EstaActivo { get; set; } = true;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class VehiWindow : Window
    {
        private readonly RepositorioSql _db = new();
        private string _placaSeleccionada = string.Empty;
        private string _clienteDNI = string.Empty;

        public VehiWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ── RESTRICCIONES DE ENTRADA ─────────────────────────────────

        private void txtAnio_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

        private void txtClienteDNI_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

        private void txtPlaca_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");

        private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPlaca.IsReadOnly) return;
            int caret = txtPlaca.CaretIndex;
            txtPlaca.Text = txtPlaca.Text.ToUpper();
            txtPlaca.CaretIndex = caret;

            int len = txtPlaca.Text.Length;
            txtContadorPlaca.Text = $"{len}/7";
        }

        private void txtClienteDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (borderClienteInfo != null && txtClienteDNI.IsFocused && txtClienteDNI.Text != _clienteDNI)
            {
                borderClienteInfo.Visibility = Visibility.Collapsed;
                _clienteDNI = string.Empty;
            }

            string texto = txtClienteDNI.Text.Trim();
            if (texto.Length >= 3)
            {
                try
                {
                    var sugerencias = _db.BuscarClientesPorDNI(texto);
                    if (sugerencias.Count > 0)
                    {
                        listAutoComplete.ItemsSource = sugerencias;
                        popupAutoComplete.IsOpen = true;
                        return;
                    }
                }
                catch { }
            }
            popupAutoComplete.IsOpen = false;
        }

        private void txtClienteDNI_LostFocus(object sender, RoutedEventArgs e)
        {
            // Pequeño delay para permitir click en el popup
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!listAutoComplete.IsMouseOver)
                    popupAutoComplete.IsOpen = false;
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void txtClienteDNI_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!popupAutoComplete.IsOpen) return;
            if (e.Key == Key.Down)
            {
                listAutoComplete.Focus();
                if (listAutoComplete.Items.Count > 0)
                    listAutoComplete.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popupAutoComplete.IsOpen = false;
            }
        }

        private void listAutoComplete_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && listAutoComplete.SelectedItem != null)
            {
                SeleccionarSugerencia(listAutoComplete.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                popupAutoComplete.IsOpen = false;
                txtClienteDNI.Focus();
            }
        }

        private void listAutoComplete_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (listAutoComplete.SelectedItem != null)
                SeleccionarSugerencia(listAutoComplete.SelectedItem);
        }

        private void SeleccionarSugerencia(object item)
        {
            if (item is RepositorioSql.ClienteSugerencia s)
            {
                txtClienteDNI.Text = s.DNI;
                _clienteDNI = s.DNI;
                MostrarClienteOk(s.NombreCompleto);
            }
            popupAutoComplete.IsOpen = false;
        }

        // ── CLIENTE ──────────────────────────────────────────────────

        public void EstablecerCliente(int clienteDNI)
        {
            txtClienteDNI.Text = clienteDNI.ToString();
            VerificarClienteEnBD(clienteDNI.ToString());
        }

        private void BtnVerificarCliente_Click(object sender, RoutedEventArgs e)
        {
            string dni = txtClienteDNI.Text.Trim();
            if (!clsValidacionesVehiculo.ValidarFormatoDNICliente(dni))
            {
                txtClienteDNI.Focus();
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
            iconClienteEstado.Foreground = Pincel("#4CAF50");
            txtClienteNombre.Text = nombreCompleto;
            txtClienteEstado.Text = "✔ Cliente verificado correctamente";
            txtClienteEstado.Foreground = Pincel("#4CAF50");
        }

        private void MostrarClienteError(string mensaje)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountAlert;
            iconClienteEstado.Foreground = Pincel("#f44336");
            txtClienteNombre.Text = mensaje;
            txtClienteEstado.Text = "✘ Cliente no encontrado";
            txtClienteEstado.Foreground = Pincel("#f44336");
        }

        // ── VALIDACIONES ─────────────────────────────────────────────

        private bool ValidarCamposComunes(out int año)
        {
            año = 0;

            if (!clsValidacionesVehiculo.ValidarFormularioVacio(
                txtPlaca.Text, txtMarca.Text, txtModelo.Text,
                txtAnio.Text, txtClienteDNI.Text)) return false;

            if (!clsValidacionesVehiculo.ValidarPlacaNoNula(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarPlacaSoloAlfanumerico(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarLongitudPlaca(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            string tipoStr = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            if (!clsValidacionesVehiculo.ValidarFormatoPlacaSegunTipo(txtPlaca.Text, tipoStr))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarPlacaNoReservada(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarMarca(txtMarca.Text))
            { txtMarca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarModelo(txtModelo.Text))
            { txtModelo.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarAnioVehiculo(txtAnio.Text, out año))
            { txtAnio.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarTipoVehiculo(cmbTipo.SelectedItem))
            { cmbTipo.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarObservaciones(txtObservaciones.Text))
            { txtObservaciones.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarClienteDNI(txtClienteDNI.Text, _clienteDNI))
            { txtClienteDNI.Focus(); return false; }

            return true;
        }

        private bool ValidarCamposGuardar(out int año)
        {
            año = 0;
            if (!ValidarCamposComunes(out año)) return false;

            if (!clsValidacionesVehiculo.ValidarPlacaNoDuplicada(txtPlaca.Text, p => _db.ExistePlaca(p)))
            { txtPlaca.Focus(); return false; }

            return true;
        }

        

        // ── CANCELAR ─────────────────────────────────────────────────

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        // ── TOGGLE ESTADO ────────────────────────────────────────────

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
            => SetEstado("El vehículo está activo", "#4CAF50");

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
            => SetEstado("El vehículo está inactivo", "#f44336");

        private void SetEstado(string texto, string color)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = texto;
            txtEstadoLabel.Foreground = Pincel(color);
            if (iconEstado != null) iconEstado.Foreground = Pincel(color);
        }

        // ── CARGAR PARA EDITAR ───────────────────────────────────────

        public void CargarVehiculoParaEditar(Vehiculo vehiculo)
        {
            _placaSeleccionada = vehiculo.Vehiculo_Placa ?? string.Empty;
            txtPlaca.Text = vehiculo.Vehiculo_Placa;
            txtMarca.Text = vehiculo.Vehiculo_Marca;
            txtModelo.Text = vehiculo.Vehiculo_Modelo;
            txtAnio.Text = vehiculo.Vehiculo_Año.ToString();
            txtObservaciones.Text = vehiculo.Vehiculo_Observaciones;
            txtClienteDNI.Text = vehiculo.Cliente_DNI;
            _clienteDNI = vehiculo.Cliente_DNI;
            MostrarClienteOk(vehiculo.Cliente_NombreCompleto ?? string.Empty);

            foreach (ComboBoxItem item in cmbTipo.Items)
            {
                if (item.Content?.ToString() == vehiculo.Vehiculo_Tipo)
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

        // ── LIMPIAR ──────────────────────────────────────────────────

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

        // ── HELPER ───────────────────────────────────────────────────

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}