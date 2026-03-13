using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Órdenes_de_Trabajo
{
    public partial class MainWindow : Window
    {
        private string _rutaFoto = string.Empty;
        private ObservableCollection<RepuestoItem> _repuestos = new();

        public MainWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;
            dpFecha.SelectedDate = DateTime.Today;
            txtOrdenNumero.Text = "1";
        }

        private void RecalcularTotal()
        {
            if (txtPrecioRepuesto == null || txtPrecioServicio == null || txtCostoTotal == null)
                return;

            double repuesto = ObtenerValor(txtPrecioRepuesto.Text);
            double servicio = ObtenerValor(txtPrecioServicio.Text);
            txtCostoTotal.Text = $"S/ {repuesto + servicio:N2}";
        }

        private double ObtenerValor(string texto)
        {
            texto = texto.Replace("S/", "").Trim();
            return double.TryParse(texto, out double v) ? v : 0;
        }

        private void txtPrecioRepuesto_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcularTotal();

        private void txtPrecioServicio_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcularTotal();

        private void txtPrecio_TextChanged(object sender, TextChangedEventArgs e)
            => RecalcularTotal();

        private void borderFoto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Seleccionar foto del vehículo",
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp;*.gif"
            };

            if (dlg.ShowDialog() != true) return;

            _rutaFoto = dlg.FileName;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(_rutaFoto);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            imgFotoVehiculo.Source = bmp;
            imgFotoVehiculo.Visibility = Visibility.Visible;
            stackFotoPlaceholder.Visibility = Visibility.Collapsed;
            btnQuitarFoto.Visibility = Visibility.Visible;
        }

        private void btnQuitarFoto_Click(object sender, RoutedEventArgs e)
        {
            _rutaFoto = string.Empty;
            imgFotoVehiculo.Source = null;
            imgFotoVehiculo.Visibility = Visibility.Collapsed;
            stackFotoPlaceholder.Visibility = Visibility.Visible;
            btnQuitarFoto.Visibility = Visibility.Collapsed;
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e) { }

        private void lstResultados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstResultados.SelectedItem is not ResultadoBusqueda r) return;
            popupBusqueda.IsOpen = false;
            txtBuscar.Text = string.Empty;

            txtClienteNombre.Text = r.NombreCompleto;
            txtClienteTelefono.Text = r.Telefono;
            txtClienteTelefonoOpcional.Text = r.TelefonoOpcional;
            txtClienteEmail.Text = r.Email;
            txtVehiculoPlaca.Text = r.Placa;
            txtVehiculoModelo.Text = r.Modelo;
            txtVehiculoMotor.Text = r.Motor;
            txtVehiculoAnio.Text = r.Anio;

            foreach (ComboBoxItem item in cmbVehiculoMarca.Items)
                if (item.Content?.ToString() == r.Marca)
                { cmbVehiculoMarca.SelectedItem = item; break; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AgregarRepuesto ventana = new AgregarRepuesto();
            ventana.ShowDialog();
        }

        private void btnAgregarRepuestos_Click(object sender, RoutedEventArgs e)
            => Button_Click(sender, e);

        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            MessageBox.Show("✅ Orden registrada correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            LimpiarFormulario();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarFormulario()) return;

            MessageBox.Show("✅ Orden actualizada correctamente.", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("¿Deseas cancelar y volver al menú?", "Cancelar",
                                      MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                MenúPrincipalOrdenes ventana = new MenúPrincipalOrdenes();
                this.Close();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
            => btnCancelar_Click(sender, e);

        private bool ValidarFormulario()
        {
            if (string.IsNullOrWhiteSpace(txtClienteNombre.Text))
            {
                MessageBox.Show("El nombre del cliente es obligatorio.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtClienteNombre.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtVehiculoPlaca.Text))
            {
                MessageBox.Show("La placa del vehículo es obligatoria.", "Validación",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                txtVehiculoPlaca.Focus();
                return false;
            }
            return true;
        }

        private void LimpiarFormulario()
        {
            txtClienteNombre.Text = string.Empty;
            txtClienteTelefono.Text = string.Empty;
            txtClienteTelefonoOpcional.Text = string.Empty;
            txtClienteEmail.Text = string.Empty;
            txtVehiculoPlaca.Text = string.Empty;
            txtVehiculoModelo.Text = string.Empty;
            txtVehiculoMotor.Text = string.Empty;
            txtVehiculoAnio.Text = string.Empty;
            txtOrdenObservaciones.Text = string.Empty;
            txtPrecioRepuesto.Text = "S/ 0.00";
            txtPrecioServicio.Text = "S/ 0.00";
            txtCostoTotal.Text = "S/ 0.00";
            cmbVehiculoMarca.SelectedIndex = -1;
            cmbOrdenEstado.SelectedIndex = 0;
            cmbOrdenPrioridad.SelectedIndex = 0;
            dpFecha.SelectedDate = DateTime.Today;
            dpEntrega.SelectedDate = null;
            _repuestos.Clear();
            btnQuitarFoto_Click(null, null);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
        private void cmbVehiculoMarca_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    }

    public class RepuestoItem
    {
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public bool Incluido { get; set; }
    }

    public class ResultadoBusqueda
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public string InfoVehiculo { get; set; } = string.Empty;
        public string Placa { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string Modelo { get; set; } = string.Empty;
        public string Motor { get; set; } = string.Empty;
        public string Anio { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string TelefonoOpcional { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}