using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Órdenes_de_Trabajo
{
    public class RepuestoOrden : INotifyPropertyChanged
    {
        public int Numero { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public int ProductoID { get; set; }

        private bool _incluido = true;
        public bool Incluido
        {
            get => _incluido;
            set
            {
                _incluido = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Incluido)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public partial class OrdenWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
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

            txtPrecioServicio.TextChanged += (s, e) => RecalcularPrecios();

            txtPrecioServicio.LostFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                if (decimal.TryParse(texto,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal valor))
                {
                    txtPrecioServicio.Text = $"L {valor:N2}";
                }
                else
                {
                    txtPrecioServicio.Text = "L 0.00";
                }
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
                _conexion.Abrir();

                string sqlOrden = @"
                    SELECT o.Cliente_DNI, o.Vehiculo_Placa, o.Estado,
                           o.Fecha, o.Fecha_Entrega, o.Observaciones,
                           o.Servicio_Precio, o.OrdenPrecio_Total,
                           o.Adjuntos_Fotos,
                           c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                           c.Cliente_TelefonoPrincipal, c.Cliente_Email,
                           v.Vehiculo_Marca + ' ' + v.Vehiculo_Modelo AS NombreVehiculo,
                           v.Vehiculo_Tipo + ' · ' + CAST(v.Vehiculo_Año AS VARCHAR) AS TipoAño
                    FROM   Orden_Trabajo o
                    INNER JOIN Cliente  c ON o.Cliente_DNI    = c.Cliente_DNI
                    INNER JOIN Vehiculo v ON o.Vehiculo_Placa = v.Vehiculo_Placa
                    WHERE  o.Orden_ID = @OrdenID";

                using (SqlCommand cmd = new SqlCommand(sqlOrden, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@OrdenID", ordenID);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            _clienteDNI = rd["Cliente_DNI"].ToString();
                            _vehiculoPlaca = rd["Vehiculo_Placa"].ToString();

                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderClienteInfo.Visibility = Visibility.Visible;

                            txtVehiculoNombre.Text = rd["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = rd["NombreCompleto"].ToString();
                            borderVehiculoInfo.Visibility = Visibility.Visible;

                            dpFecha.SelectedDate = rd["Fecha"] as DateTime?;
                            dpEntrega.SelectedDate = rd["Fecha_Entrega"] as DateTime?;

                            txtObservaciones.Text = rd["Observaciones"].ToString();

                            decimal servicioPrecio = Convert.ToDecimal(rd["Servicio_Precio"]);
                            txtPrecioServicio.Text = $"L {servicioPrecio:N2}";

                            string foto = rd["Adjuntos_Fotos"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(foto) && System.IO.File.Exists(foto))
                            {
                                _rutaFoto = foto;
                                imgFoto.Source = new BitmapImage(new Uri(foto));
                                imgFoto.Visibility = Visibility.Visible;
                                txtFotoPlaceholder.Visibility = Visibility.Collapsed;
                            }

                            string estado = rd["Estado"].ToString();
                            foreach (ComboBoxItem item in cmbEstado.Items)
                            {
                                if (item.Content.ToString() == estado)
                                {
                                    cmbEstado.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }

                string sqlRepuestos = @"
                    SELECT r.Producto_ID,
                           r.Repuesto_Nombre,
                           r.Repuesto_Cantidad,
                           r.Repuesto_Precio
                    FROM   Orden_Repuesto r
                    WHERE  r.Orden_ID = @OrdenID";

                using (SqlCommand cmd2 = new SqlCommand(sqlRepuestos, _conexion.SqlC))
                {
                    cmd2.Parameters.AddWithValue("@OrdenID", ordenID);
                    using (SqlDataReader rd2 = cmd2.ExecuteReader())
                    {
                        int numero = 1;
                        while (rd2.Read())
                        {
                            var repuesto = new RepuestoOrden
                            {
                                Numero = numero++,
                                ProductoID = Convert.ToInt32(rd2["Producto_ID"]),
                                Nombre = rd2["Repuesto_Nombre"].ToString(),
                                Cantidad = Convert.ToInt32(rd2["Repuesto_Cantidad"]),
                                Precio = Convert.ToDecimal(rd2["Repuesto_Precio"]),
                                Incluido = true
                            };
                            repuesto.PropertyChanged += (s, e) => RecalcularPrecios();
                            _repuestos.Add(repuesto);
                        }
                    }
                }

                // Prioridad Normal por defecto al editar
                foreach (ComboBoxItem item in cmbPrioridad.Items)
                {
                    if (item.Content.ToString() == "Normal")
                    {
                        cmbPrioridad.SelectedItem = item;
                        break;
                    }
                }

                // Deshabilitar Añadir al editar
                btnAñadir.IsEnabled = false;
                btnAñadir.Opacity = 0.4;

                RecalcularPrecios();
            }
            catch (Exception ex) { MostrarError("Error al cargar la orden: " + ex.Message); }
            finally { _conexion.Cerrar(); }
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
                _conexion.Abrir();
                string sqlCliente = @"
                    SELECT Cliente_Nombres + ' ' + Cliente_Apellidos AS NombreCompleto,
                           Cliente_TelefonoPrincipal, Cliente_Email
                    FROM   Cliente WHERE Cliente_DNI = @DNI";

                bool encontrado = false;
                using (SqlCommand cmd = new SqlCommand(sqlCliente, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            encontrado = true;
                            _clienteDNI = dni;
                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderClienteInfo.Visibility = Visibility.Visible;
                        }
                    }
                }

                if (!encontrado) { MostrarError($"No existe cliente con DNI '{dni}'."); return; }

                string sqlVehiculo = @"
                    SELECT TOP 1
                           Vehiculo_Marca + ' ' + Vehiculo_Modelo AS NombreVehiculo,
                           Vehiculo_Tipo + ' · ' + CAST(Vehiculo_Año AS VARCHAR) AS TipoAño,
                           Vehiculo_Placa
                    FROM   Vehiculo WHERE Cliente_DNI = @DNI ORDER BY Vehiculo_Placa";

                using (SqlCommand cmd2 = new SqlCommand(sqlVehiculo, _conexion.SqlC))
                {
                    cmd2.Parameters.AddWithValue("@DNI", dni);
                    using (SqlDataReader rd2 = cmd2.ExecuteReader())
                    {
                        if (rd2.Read())
                        {
                            _vehiculoPlaca = rd2["Vehiculo_Placa"].ToString();
                            txtVehiculoNombre.Text = rd2["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd2["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = txtClienteNombre.Text;
                            borderVehiculoInfo.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception ex) { MostrarError("Error: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        private void BuscarPorPlaca(string placa)
        {
            try
            {
                _conexion.Abrir();
                string sql = @"
                    SELECT v.Vehiculo_Placa,
                           v.Vehiculo_Marca + ' ' + v.Vehiculo_Modelo AS NombreVehiculo,
                           v.Vehiculo_Tipo + ' · ' + CAST(v.Vehiculo_Año AS VARCHAR) AS TipoAño,
                           c.Cliente_DNI,
                           c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                           c.Cliente_TelefonoPrincipal, c.Cliente_Email
                    FROM   Vehiculo v
                    INNER JOIN Cliente c ON v.Cliente_DNI = c.Cliente_DNI
                    WHERE  v.Vehiculo_Placa = @Placa";

                using (SqlCommand cmd = new SqlCommand(sql, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Placa", placa);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            _vehiculoPlaca = placa;
                            _clienteDNI = rd["Cliente_DNI"].ToString();
                            txtVehiculoNombre.Text = rd["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = rd["NombreCompleto"].ToString();
                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderVehiculoInfo.Visibility = Visibility.Visible;
                            borderClienteInfo.Visibility = Visibility.Visible;
                        }
                        else { MostrarError($"No existe vehículo con placa '{placa}'."); }
                    }
                }
            }
            catch (Exception ex) { MostrarError("Error: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_clienteDNI) || string.IsNullOrEmpty(_vehiculoPlaca))
            {
                MostrarError("Busca un cliente o vehículo antes de guardar.");
                return;
            }

            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            string precioTexto = txtPrecioServicio.Text
                .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();

            decimal.TryParse(precioTexto,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal precioServicio);

            decimal total = totalRepuestos + precioServicio;
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";
            int productoID = _repuestos.Count > 0 ? _repuestos[0].ProductoID : 0;

            try
            {
                _conexion.Abrir();

                string queryOrden = @"
                    INSERT INTO Orden_Trabajo
                        (Cliente_DNI, Vehiculo_Placa, Producto_ID, Estado,
                         Fecha, Fecha_Entrega, Observaciones,
                         Servicio_Precio, OrdenPrecio_Total, Adjuntos_Fotos)
                    VALUES
                        (@ClienteDNI, @Placa, @ProductoID, @Estado,
                         @Fecha, @FechaEntrega, @Observaciones,
                         @ServicioPrecio, @Total, @Foto);
                    SELECT SCOPE_IDENTITY();";

                int ordenID;
                using (SqlCommand cmd = new SqlCommand(queryOrden, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@ClienteDNI", _clienteDNI);
                    cmd.Parameters.AddWithValue("@Placa", _vehiculoPlaca);
                    cmd.Parameters.AddWithValue("@ProductoID", productoID);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@Fecha", dpFecha.SelectedDate ?? DateTime.Today);
                    cmd.Parameters.AddWithValue("@FechaEntrega", dpEntrega.SelectedDate.HasValue
                                                                    ? (object)dpEntrega.SelectedDate.Value
                                                                    : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                                                    ? (object)DBNull.Value
                                                                    : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(_rutaFoto)
                                                                    ? (object)DBNull.Value : _rutaFoto);
                    ordenID = Convert.ToInt32(Convert.ToDecimal(cmd.ExecuteScalar()));
                }

                foreach (var rep in _repuestos)
                {
                    if (!rep.Incluido) continue;
                    using (SqlCommand cmdRep = new SqlCommand("sp_AgregarRepuestoOrden", _conexion.SqlC))
                    {
                        cmdRep.CommandType = CommandType.StoredProcedure;
                        cmdRep.Parameters.AddWithValue("@OrdenID", ordenID);
                        cmdRep.Parameters.AddWithValue("@ProductoID", rep.ProductoID);
                        cmdRep.Parameters.AddWithValue("@Cantidad", rep.Cantidad);
                        cmdRep.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Orden guardada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                this.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Error de base de datos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
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

            decimal.TryParse(precioTexto,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal precioServicio);

            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            decimal total = totalRepuestos + precioServicio;
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";

            try
            {
                _conexion.Abrir();

                string sqlUpdate = @"
                    UPDATE Orden_Trabajo SET
                        Estado            = @Estado,
                        Fecha             = @Fecha,
                        Fecha_Entrega     = @FechaEntrega,
                        Observaciones     = @Observaciones,
                        Servicio_Precio   = @ServicioPrecio,
                        OrdenPrecio_Total = @Total,
                        Adjuntos_Fotos    = @Foto
                    WHERE Orden_ID = @OrdenID";

                using (SqlCommand cmd = new SqlCommand(sqlUpdate, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@Fecha", dpFecha.SelectedDate ?? DateTime.Today);
                    cmd.Parameters.AddWithValue("@FechaEntrega", dpEntrega.SelectedDate.HasValue
                                                                    ? (object)dpEntrega.SelectedDate.Value
                                                                    : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                                                    ? (object)DBNull.Value
                                                                    : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                    cmd.Parameters.AddWithValue("@Total", total);
                    cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(_rutaFoto)
                                                                    ? (object)DBNull.Value : _rutaFoto);
                    cmd.Parameters.AddWithValue("@OrdenID", _ordenIDEditar);
                    cmd.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM Orden_Repuesto WHERE Orden_ID = @OrdenID", _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@OrdenID", _ordenIDEditar);
                    cmd.ExecuteNonQuery();
                }

                foreach (var rep in _repuestos)
                {
                    if (!rep.Incluido) continue;
                    using (SqlCommand cmdRep = new SqlCommand("sp_AgregarRepuestoOrden", _conexion.SqlC))
                    {
                        cmdRep.CommandType = CommandType.StoredProcedure;
                        cmdRep.Parameters.AddWithValue("@OrdenID", _ordenIDEditar);
                        cmdRep.Parameters.AddWithValue("@ProductoID", rep.ProductoID);
                        cmdRep.Parameters.AddWithValue("@Cantidad", rep.Cantidad);
                        cmdRep.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("✅ Orden actualizada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
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

            decimal.TryParse(precioTexto,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal servicio);

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
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
        }
    }
}