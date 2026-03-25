using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Órdenes_de_Trabajo
{
    public partial class OrdenWindow : Window
    {
        private clsConsultasBD _db = new clsConsultasBD(); 
        private string _clienteDNI = string.Empty;
        private string _vehiculoPlaca = string.Empty;
        private bool _buscarPorDNI = true;
        private int _ordenIDEditar = 0;
        private string _rutaFoto = string.Empty;

        private ObservableCollection<RepuestoOrden> _repuestos
            = new ObservableCollection<RepuestoOrden>();

        public OrdenWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;

            dpFecha.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");
            dpEntrega.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");

            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;

            txtPrecioServicio.TextChanged += (s, e) => RecalcularPrecios();

            txtPrecioServicio.LostFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                if (decimal.TryParse(texto,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal valor))
                    txtPrecioServicio.Text = $"L {valor:N2}";
                else
                    txtPrecioServicio.Text = "L 0.00";
            };

            txtPrecioServicio.GotFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                txtPrecioServicio.Text = texto == "0.00" ? "0" : texto;
                txtPrecioServicio.SelectAll();
            };
        }

        public async Task CargarOrdenParaEditar(int ordenID)
        {
            _ordenIDEditar = ordenID;
            try
            {
                var orden = _db.ObtenerOrdenParaEditar(ordenID); 
                if (orden == default) return;

                _clienteDNI = orden.clienteDNI;
                _vehiculoPlaca = orden.vehiculoPlaca;

                txtClienteNombre.Text = orden.nombreCompleto;
                txtClienteTelefono.Text = orden.telefono;
                txtClienteEmail.Text = orden.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                txtVehiculoNombre.Text = orden.vehiculoNombre;
                txtVehiculoTipo.Text = orden.vehiculoTipo;
                txtVehiculoPropietario.Text = orden.nombreCompleto;
                borderVehiculoInfo.Visibility = Visibility.Visible;

                dpFecha.SelectedDate = orden.fecha;
                dpEntrega.SelectedDate = orden.fechaEntrega;
                txtObservaciones.Text = orden.observaciones;
                txtPrecioServicio.Text = $"L {orden.servicioPrecio:N2}";

                if (!string.IsNullOrEmpty(orden.foto) && System.IO.File.Exists(orden.foto))
                {
                    _rutaFoto = orden.foto;
                    imgFoto.Source = new BitmapImage(new Uri(orden.foto));
                    imgFoto.Visibility = Visibility.Visible;
                    txtFotoPlaceholder.Visibility = Visibility.Collapsed;
                }

                foreach (ComboBoxItem item in cmbEstado.Items)
                {
                    if (item.Content.ToString() == orden.estado)
                    {
                        cmbEstado.SelectedItem = item;
                        break;
                    }
                }

                var repuestos = _db.ObtenerRepuestosOrden(ordenID); 
                foreach (var rep in repuestos)
                {
                    rep.PropertyChanged += (s, e) => RecalcularPrecios();
                    _repuestos.Add(rep);
                }

                foreach (ComboBoxItem item in cmbPrioridad.Items)
                {
                    if (item.Content.ToString() == "Normal")
                    {
                        cmbPrioridad.SelectedItem = item;
                        break;
                    }
                }

                btnAñadir.IsEnabled = false;
                btnAñadir.Opacity = 0.4;
                btnActualizar.IsEnabled = true;
                btnActualizar.Opacity = 1;

                RecalcularPrecios();
            }
            catch (Exception ex) { MostrarError("Error al cargar la orden: " + ex.Message); }
        }

        private void TabDNI_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = true;
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            if (tabPlaca.Child is TextBlock tb) tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7293"));
            lblBuscar.Text = "DNI del Cliente";
            txtBuscar.Text = string.Empty;
            LimpiarResultados();
        }

        private void TabPlaca_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = false;
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            if (tabPlaca.Child is TextBlock tb) tb.Foreground = new SolidColorBrush(Colors.White);
            lblBuscar.Text = "Placa del Vehículo";
            txtBuscar.Text = string.Empty;
            LimpiarResultados();
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string valor = txtBuscar.Text.Trim();
            if (string.IsNullOrEmpty(valor))
            {
                MostrarError(_buscarPorDNI ? "Ingresa un DNI para buscar." : "Ingresa una placa para buscar.");
                return;
            }
            LimpiarResultados();
            if (_buscarPorDNI) BuscarPorDNI(valor);
            else BuscarPorPlaca(valor.ToUpper());
        }

        private void BuscarPorDNI(string dni)
        {
            try
            {
                var resultado = _db.BuscarClientePorDNI(dni); 
                if (resultado == default)
                {
                    MostrarError($"No existe cliente con DNI '{dni}'.");
                    return;
                }

                _clienteDNI = dni;
                txtClienteNombre.Text = resultado.nombreCompleto;
                txtClienteTelefono.Text = resultado.telefono;
                txtClienteEmail.Text = resultado.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(resultado.vehiculoPlaca))
                {
                    _vehiculoPlaca = resultado.vehiculoPlaca;
                    txtVehiculoNombre.Text = resultado.vehiculoNombre;
                    txtVehiculoTipo.Text = resultado.vehiculoTipo;
                    txtVehiculoPropietario.Text = resultado.nombreCompleto;
                    borderVehiculoInfo.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BuscarPorPlaca(string placa)
        {
            try
            {
                var resultado = _db.BuscarVehiculoPorPlaca(placa); 
                if (resultado == default)
                {
                    MostrarError($"No existe vehículo con placa '{placa}'.");
                    return;
                }

                _vehiculoPlaca = placa;
                _clienteDNI = resultado.clienteDNI;
                txtVehiculoNombre.Text = resultado.vehiculoNombre;
                txtVehiculoTipo.Text = resultado.vehiculoTipo;
                txtVehiculoPropietario.Text = resultado.nombreCompleto;
                txtClienteNombre.Text = resultado.nombreCompleto;
                txtClienteTelefono.Text = resultado.telefono;
                txtClienteEmail.Text = resultado.email;
                borderVehiculoInfo.Visibility = Visibility.Visible;
                borderClienteInfo.Visibility = Visibility.Visible;
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_clienteDNI) || string.IsNullOrEmpty(_vehiculoPlaca))
            {
                MessageBox.Show("⚠ Busca un cliente o vehículo antes de guardar.",
                    "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbEstado.SelectedItem == null)
            {
                MessageBox.Show("⚠ Selecciona el estado de la orden.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpFecha.SelectedDate.HasValue &&
                    dpFecha.SelectedDate.Value > DateTime.Today.AddYears(1))
            {
                MessageBox.Show("⚠ La fecha de inicio no puede ser mayor a un año en el futuro.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dpFecha.SelectedDate.HasValue && dpEntrega.SelectedDate.HasValue &&
                    dpEntrega.SelectedDate.Value < dpFecha.SelectedDate.Value)
            {
                MessageBox.Show("⚠ La fecha de entrega no puede ser anterior a la fecha de inicio.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string precioCheck = txtPrecioServicio.Text.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            if (!string.IsNullOrWhiteSpace(precioCheck) &&
                !decimal.TryParse(precioCheck, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _))
            {
                MessageBox.Show("⚠ El precio del servicio debe ser un número válido.",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            string precioTexto = txtPrecioServicio.Text
                .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            decimal.TryParse(precioTexto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioServicio);

            decimal total = totalRepuestos + precioServicio;
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";
            int productoID = _repuestos.Count > 0 ? _repuestos[0].ProductoID : 0;

            try
            {
                _db.AgregarOrden( 
                    _clienteDNI, _vehiculoPlaca, productoID, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, _rutaFoto,
                    _repuestos.ToList()
                );

                MessageBox.Show("✅ Orden guardada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dpFecha.SelectedDate.HasValue)
            {
                var fechaOrden = dpFecha.SelectedDate.Value;
                var hoy = DateTime.Today;
                if (fechaOrden.Year < hoy.Year ||
                   (fechaOrden.Year == hoy.Year && fechaOrden.Month < hoy.Month))
                {
                    MessageBox.Show("No se pueden actualizar órdenes de meses anteriores.",
                        "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string precioTexto = txtPrecioServicio.Text
                .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            decimal.TryParse(precioTexto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal precioServicio);

            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            decimal total = totalRepuestos + precioServicio;
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";

            try
            {
                _db.ActualizarOrden( 
                    _ordenIDEditar, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, _rutaFoto,
                    _repuestos.ToList()
                );

                MessageBox.Show("✅ Orden actualizada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdjuntarFoto_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Seleccionar foto del vehículo"
            };

            if (dialog.ShowDialog() == true)
            {
                _rutaFoto = dialog.FileName;
                imgFoto.Source = new BitmapImage(new Uri(_rutaFoto));
                imgFoto.Visibility = Visibility.Visible;
                txtFotoPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarRepuesto();
            ventana.Owner = this;
            ventana.ShowDialog();

            if (ventana.RepuestoResultado != null)
            {
                ventana.RepuestoResultado.Numero = _repuestos.Count + 1;
                ventana.RepuestoResultado.PropertyChanged += (s, e) => RecalcularPrecios();
                _repuestos.Add(ventana.RepuestoResultado);
                RecalcularPrecios();
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        private void RecalcularPrecios()
        {
            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            txtPrecioRepuesto.Text = $"L {totalRepuestos:N2}";

            string precioTexto = txtPrecioServicio.Text
                .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            decimal.TryParse(precioTexto, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal servicio);

            txtCostoTotal.Text = $"L {(totalRepuestos + servicio):N2}";
        }

        private void MostrarError(string mensaje)
        {
            borderError.Visibility = Visibility.Visible;
            txtError.Text = mensaje;
        }

        private void LimpiarResultados()
        {
            borderClienteInfo.Visibility = Visibility.Collapsed;
            borderVehiculoInfo.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
            _clienteDNI = string.Empty;
            _vehiculoPlaca = string.Empty;
        }

        private void LimpiarFormulario()
        {
            LimpiarResultados();
            txtBuscar.Clear();
            _repuestos.Clear();
            _ordenIDEditar = 0;
            _rutaFoto = string.Empty;
            txtPrecioRepuesto.Text = "L 0.00";
            txtPrecioServicio.Text = "L 0.00";
            txtCostoTotal.Text = "L 0.00";
            dpFecha.SelectedDate = null;
            dpEntrega.SelectedDate = null;
            txtObservaciones?.Clear();
            cmbEstado.SelectedIndex = -1;
            cmbPrioridad.SelectedIndex = -1;
            imgFoto.Source = null;
            imgFoto.Visibility = Visibility.Collapsed;
            txtFotoPlaceholder.Visibility = Visibility.Visible;
            btnAñadir.IsEnabled = true;
            btnAñadir.Opacity = 1;
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }
    }
}