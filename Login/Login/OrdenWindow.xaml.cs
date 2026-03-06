using Login.Clases;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    // ═══════════════════════════════════════════════════════════════
    // MODELO — fila del DataGrid de repuestos en la orden
    // ═══════════════════════════════════════════════════════════════
    public class RepuestoOrden
    {
        public int Numero { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public bool Incluido { get; set; } = true;
        public int ProductoID { get; set; }
    }

    public partial class OrdenWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
        private string _clienteDNI = string.Empty;
        private string _vehiculoPlaca = string.Empty;
        private bool _buscarPorDNI = true;

        private ObservableCollection<RepuestoOrden> _repuestos
            = new ObservableCollection<RepuestoOrden>();

        public async Task CargarOrdenParaEditar(int ordenID)
        {
            try
            {
                _conexion.Abrir();

                // 1. Cargar datos principales de la orden
                string sqlOrden = @"
            SELECT o.Cliente_DNI, o.Vehiculo_Placa, o.Estado,
                   o.Fecha, o.Fecha_Entrega, o.Observaciones,
                   o.Servicio_Precio, o.OrdenPrecio_Total,
                   c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                   c.Cliente_TelefonoPrincipal, c.Cliente_Email,
                   v.Vehiculo_Marca + ' ' + v.Vehiculo_Modelo AS NombreVehiculo,
                   v.Vehiculo_Tipo + ' · ' + CAST(v.Vehiculo_Año AS VARCHAR) AS TipoAño
            FROM   Orden_Trabajo o
            INNER JOIN Cliente c ON o.Cliente_DNI = c.Cliente_DNI
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

                            // Cliente
                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderClienteInfo.Visibility = Visibility.Visible;

                            // Vehículo
                            txtVehiculoNombre.Text = rd["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = rd["NombreCompleto"].ToString();
                            borderVehiculoInfo.Visibility = Visibility.Visible;

                            // Datos de la orden
                            dpFecha.SelectedDate = rd["Fecha"] as DateTime?;
                            dpEntrega.SelectedDate = rd["Fecha_Entrega"] as DateTime?;
                            txtObservaciones.Text = rd["Observaciones"].ToString();
                            txtPrecioServicio.Text = $"S/ {rd["Servicio_Precio"]:N2}";

                            // Estado en el ComboBox
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

                // 2. Cargar repuestos de la orden
                string sqlRepuestos = @"
            SELECT p.Producto_ID, p.Producto_Nombre,
                   r.Cantidad, p.Producto_Precio
            FROM   Orden_Repuesto r
            INNER JOIN Producto p ON r.Producto_ID = p.Producto_ID
            WHERE  r.Orden_ID = @OrdenID";

                using (SqlCommand cmd2 = new SqlCommand(sqlRepuestos, _conexion.SqlC))
                {
                    cmd2.Parameters.AddWithValue("@OrdenID", ordenID);
                    using (SqlDataReader rd2 = cmd2.ExecuteReader())
                    {
                        int numero = 1;
                        while (rd2.Read())
                        {
                            _repuestos.Add(new RepuestoOrden
                            {
                                Numero = numero++,
                                ProductoID = Convert.ToInt32(rd2["Producto_ID"]),
                                Nombre = rd2["Producto_Nombre"].ToString(),
                                Cantidad = Convert.ToInt32(rd2["Cantidad"]),
                                Precio = Convert.ToDecimal(rd2["Producto_Precio"]),
                                Incluido = true
                            });
                        }
                    }
                }

                RecalcularPrecios();
            }
            catch (Exception ex)
            {
                MostrarError("Error al cargar la orden: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }


        public OrdenWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;
        }


        // ═══════════════════════════════════════════
        // TABS DNI / PLACA  ←  NO SE MODIFICAN
        // ═══════════════════════════════════════════
        private void TabDNI_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = true;
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            var tb = tabPlaca.Child as TextBlock;
            if (tb != null) tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7293"));
            lblBuscar.Text = "DNI del Cliente";
            txtBuscar.Text = string.Empty;
            LimpiarResultados();
        }

        private void TabPlaca_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = false;
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            var tb = tabPlaca.Child as TextBlock;
            if (tb != null) tb.Foreground = new SolidColorBrush(Colors.White);
            lblBuscar.Text = "Placa del Vehículo";
            txtBuscar.Text = string.Empty;
            LimpiarResultados();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e) { }

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

        // ═══════════════════════════════════════════
        // BUSCAR POR DNI  ←  NO SE MODIFICA
        // ═══════════════════════════════════════════
        private void BuscarPorDNI(string dni)
        {
            try
            {
                _conexion.Abrir();
                string sqlCliente = @"
                    SELECT Cliente_Nombres + ' ' + Cliente_Apellidos AS NombreCompleto,
                           Cliente_TelefonoPrincipal, Cliente_Email
                    FROM   Cliente WHERE Cliente_DNI = @DNI";

                bool clienteEncontrado = false;
                using (SqlCommand cmd = new SqlCommand(sqlCliente, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        if (rd.Read())
                        {
                            clienteEncontrado = true;
                            _clienteDNI = dni;
                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderClienteInfo.Visibility = Visibility.Visible;
                        }
                    }
                }

                if (!clienteEncontrado) { MostrarError($"No existe cliente con DNI '{dni}'."); return; }

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

        // ═══════════════════════════════════════════
        // BUSCAR POR PLACA  ←  NO SE MODIFICA
        // ═══════════════════════════════════════════
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
                            txtVehiculoNombre.Text = rd["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = rd["NombreCompleto"].ToString();
                            borderVehiculoInfo.Visibility = Visibility.Visible;
                            _clienteDNI = rd["Cliente_DNI"].ToString();
                            txtClienteNombre.Text = rd["NombreCompleto"].ToString();
                            txtClienteTelefono.Text = rd["Cliente_TelefonoPrincipal"].ToString();
                            txtClienteEmail.Text = rd["Cliente_Email"].ToString();
                            borderClienteInfo.Visibility = Visibility.Visible;
                        }
                        else { MostrarError($"No existe vehículo con placa '{placa}'."); }
                    }
                }
            }
            catch (Exception ex) { MostrarError("Error: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }


        // ═══════════════════════════════════════════
        // BOTÓN ACTUALIZAR
        //    Por implementar cuando se necesite editar
        //    órdenes existentes.
        // ═══════════════════════════════════════════
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Función de actualización próximamente.",
                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        // ═══════════════════════════════════════════
        // BOTÓN CANCELAR → limpia el formulario
        // ═══════════════════════════════════════════

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
        }

        // ═══════════════════════════════════════════
        // CALCULAR TOTAL
        // ═══════════════════════════════════════════
        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            RecalcularPrecios();
        }

        // ═══════════════════════════════════════════
        // RECALCULAR PRECIOS (interno)
        // ═══════════════════════════════════════════
        private void RecalcularPrecios()
        {
            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            txtPrecioRepuesto.Text = $"S/ {totalRepuestos:N2}";

            decimal.TryParse(
                txtPrecioServicio.Text.Replace("S/", "").Replace(",", "").Trim(),
                out decimal servicio);

            txtCostoTotal.Text = $"S/ {(totalRepuestos + servicio):N2}";
        }

        // ═══════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════
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
            txtPrecioRepuesto.Text = "S/ 0.00";
            txtPrecioServicio.Text = "S/ 0.00";
            txtCostoTotal.Text = "S/ 0.00";
            dpFecha.SelectedDate = null;
            dpEntrega.SelectedDate = null;
            if (txtObservaciones != null) txtObservaciones.Clear();
        }

        // ═══════════════════════════════════════════
        // BOTÓN + AGREGAR REPUESTOS
        //    Versión unificada — elimina el Button_Click_1
        //    duplicado que tenías antes.
        // ═══════════════════════════════════════════

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarRepuesto();
            ventana.Owner = this;
            ventana.ShowDialog();

            if (ventana.RepuestoResultado != null)
            {
                ventana.RepuestoResultado.Numero = _repuestos.Count + 1;
                _repuestos.Add(ventana.RepuestoResultado);
                RecalcularPrecios();
            }
        }

        // ═══════════════════════════════════════════
        // BOTÓN AÑADIR → INSERT orden en la BD
        //    ✔ Guarda orden + repuestos
        //    ✔ sp_AgregarRepuestoOrden descuenta stock
        // ═══════════════════════════════════════════

        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_clienteDNI) || string.IsNullOrEmpty(_vehiculoPlaca))
            {
                MessageBox.Show("Busca y selecciona un cliente y su vehículo antes de guardar.",
                    "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Selecciona la fecha de la orden.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(
                    txtPrecioServicio.Text.Replace("S/", "").Replace(",", "").Trim(),
                    out decimal precioServicio) || precioServicio < 0)
            {
                MessageBox.Show("Ingresa un precio de servicio válido (mayor o igual a 0).",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_repuestos.Count == 0)
            {
                MessageBox.Show("Agrega al menos un repuesto antes de guardar la orden.",
                    "Sin repuestos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            decimal total = totalRepuestos + precioServicio;
            string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "En Espera";
            int productoID = _repuestos[0].ProductoID;

            try
            {
                _conexion.Abrir();

                string queryOrden = @"
                    INSERT INTO Orden_Trabajo
                        (Cliente_DNI, Vehiculo_Placa, Producto_ID, Estado,
                         Fecha, Fecha_Entrega, Observaciones,
                         Servicio_Precio, OrdenPrecio_Total)
                    VALUES
                        (@ClienteDNI, @Placa, @ProductoID, @Estado,
                         @Fecha, @FechaEntrega, @Observaciones,
                         @ServicioPrecio, @Total);
                    SELECT SCOPE_IDENTITY();";

                int ordenID;
                using (SqlCommand cmd = new SqlCommand(queryOrden, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@ClienteDNI", int.Parse(_clienteDNI));
                    cmd.Parameters.AddWithValue("@Placa", _vehiculoPlaca);
                    cmd.Parameters.AddWithValue("@ProductoID", productoID);
                    cmd.Parameters.AddWithValue("@Estado", estado);
                    cmd.Parameters.AddWithValue("@Fecha", dpFecha.SelectedDate.Value);
                    cmd.Parameters.AddWithValue("@FechaEntrega",
                        dpEntrega.SelectedDate.HasValue ? (object)dpEntrega.SelectedDate.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Observaciones",
                        string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                    cmd.Parameters.AddWithValue("@Total", total);
                    ordenID = Convert.ToInt32(cmd.ExecuteScalar());
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

                MessageBox.Show($"✅ Orden #{ordenID} guardada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
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
    }
}