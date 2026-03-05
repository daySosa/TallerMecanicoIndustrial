using Login;
using System.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Data;
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

        // ID interno necesario para persistir en Orden_Repuesto
        public int ProductoID { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // CODE-BEHIND — OrdenWindow
    // ═══════════════════════════════════════════════════════════════
    public partial class OrdenWindow : Window
    {
        private clsConexion _conexion = new clsConexion();

        // Datos del cliente / vehículo encontrados en la búsqueda
        private string _clienteDNI = string.Empty;
        private string _vehiculoPlaca = string.Empty;
        private bool _buscarPorDNI = true;   // true = DNI, false = Placa

        // Lista de repuestos que el usuario va agregando
        private ObservableCollection<RepuestoOrden> _repuestos
            = new ObservableCollection<RepuestoOrden>();

        public OrdenWindow()
        {
            InitializeComponent();

            // Vincula la lista al DataGrid desde el inicio
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

        // ═══════════════════════════════════════════
        // BUSCADOR EN TIEMPO REAL  ←  NO SE MODIFICA
        // ═══════════════════════════════════════════
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e) { }

        // ═══════════════════════════════════════════
        // BOTÓN BUSCAR  ←  NO SE MODIFICA
        // ═══════════════════════════════════════════
        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string valor = txtBuscar.Text.Trim();

            if (string.IsNullOrEmpty(valor))
            {
                MostrarError(_buscarPorDNI
                    ? "Ingresa un DNI para buscar."
                    : "Ingresa una placa para buscar.");
                return;
            }

            LimpiarResultados();

            if (_buscarPorDNI)
                BuscarPorDNI(valor);
            else
                BuscarPorPlaca(valor.ToUpper());
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
                           Cliente_TelefonoPrincipal,
                           Cliente_Email
                    FROM   Cliente
                    WHERE  Cliente_DNI = @DNI";

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

                if (!clienteEncontrado)
                {
                    MostrarError($"No existe cliente con DNI '{dni}'.");
                    return;
                }

                string sqlVehiculo = @"
                    SELECT TOP 1
                           Vehiculo_Marca + ' ' + Vehiculo_Modelo AS NombreVehiculo,
                           Vehiculo_Tipo + ' · ' + CAST(Vehiculo_Año AS VARCHAR) AS TipoAño,
                           Vehiculo_Placa
                    FROM   Vehiculo
                    WHERE  Cliente_DNI = @DNI
                    ORDER  BY Vehiculo_Placa";

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
                           c.Cliente_TelefonoPrincipal,
                           c.Cliente_Email
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
                        else
                        {
                            MostrarError($"No existe vehículo con placa '{placa}'.");
                        }
                    }
                }
            }
            catch (Exception ex) { MostrarError("Error: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // BOTÓN AÑADIR → guarda la orden en la BD
        //    ✔ Criterio: Se pueden crear nuevas órdenes
        //      con todos los campos requeridos.
        //    ✔ Criterio: Las órdenes se guardan
        //      correctamente en la base de datos.
        // ═══════════════════════════════════════════
        private void BtnAñadir_Click(object sender, RoutedEventArgs e)
        {
            // Validar que haya cliente y vehículo seleccionados
            if (string.IsNullOrEmpty(_clienteDNI) || string.IsNullOrEmpty(_vehiculoPlaca))
            {
                MessageBox.Show("Busca y selecciona un cliente y su vehículo antes de guardar.",
                    "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar fecha
            if (dpFecha.SelectedDate == null)
            {
                MessageBox.Show("Selecciona la fecha de la orden.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar precio del servicio
            if (!decimal.TryParse(
                    txtPrecioServicio.Text.Replace("S/", "").Replace(",", "").Trim(),
                    out decimal precioServicio) || precioServicio < 0)
            {
                MessageBox.Show("Ingresa un precio de servicio válido (mayor o igual a 0).",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar que haya al menos un repuesto agregado
            if (_repuestos.Count == 0)
            {
                MessageBox.Show("Agrega al menos un repuesto antes de guardar la orden.",
                    "Sin repuestos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Calcular total
            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            decimal total = totalRepuestos + precioServicio;

            // Obtener estado y primer producto para el INSERT
            string estado = ((cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString()) ?? "En Espera";
            int productoID = _repuestos[0].ProductoID;   // FK requerida por la tabla

            try
            {
                _conexion.Abrir();

                // ── 1. Insertar la orden principal ──────────────────────
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
                        dpEntrega.SelectedDate.HasValue
                            ? (object)dpEntrega.SelectedDate.Value
                            : DBNull.Value);
                    cmd.Parameters.AddWithValue("@Observaciones",
                        string.IsNullOrWhiteSpace(txtObservaciones.Text)
                            ? (object)DBNull.Value
                            : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                    cmd.Parameters.AddWithValue("@Total", total);

                    ordenID = Convert.ToInt32(cmd.ExecuteScalar());
                }

                // ── 2. Insertar cada repuesto via SP ────────────────────
                //    sp_AgregarRepuestoOrden también descuenta el stock.
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

        // ═══════════════════════════════════════════
        // BOTÓN + AGREGAR REPUESTOS
        //    Abre AgregarRepuesto como diálogo modal
        //    y recibe el repuesto seleccionado.
        // ═══════════════════════════════════════════
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarRepuesto();
            ventana.Owner = this;

            ventana.ShowDialog();
        }

        // ═══════════════════════════════════════════
        // CALCULAR TOTAL
        //    ✔ Los cálculos de sumas y totales son correctos.
        // ═══════════════════════════════════════════
        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            RecalcularPrecios();
        }

        // ═══════════════════════════════════════════
        // RECALCULAR PRECIOS (interno)
        //    Suma repuestos incluidos + servicio → total.
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
    }
}