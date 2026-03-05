using Login;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public partial class OrdenWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
        private string _clienteDNI = string.Empty;
        private string _vehiculoPlaca = string.Empty;
        private bool _buscarPorDNI = true; // true = DNI, false = Placa

        public OrdenWindow()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════
        // TABS DNI / PLACA
        // ═══════════════════════════════════════════
        private void TabDNI_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = true;

            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));

            // Texto del tab placa
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
        // BOTÓN BUSCAR — decide qué buscar según tab
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
        // BUSCAR POR DNI → jala cliente + su vehículo
        // ═══════════════════════════════════════════
        private void BuscarPorDNI(string dni)
        {
            try
            {
                _conexion.Abrir();

                // 1. Buscar cliente
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

                // 2. Buscar vehículos de ese cliente
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
            catch (Exception ex)
            {
                MostrarError("Error: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // BUSCAR POR PLACA → jala vehículo + cliente
        // ═══════════════════════════════════════════
        private void BuscarPorPlaca(string placa)
        {
            try
            {
                _conexion.Abrir();

                string sql = @"
                    SELECT v.Vehiculo_Placa,
                           v.Vehiculo_Marca + ' ' + v.Vehiculo_Modelo   AS NombreVehiculo,
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
                            // Vehículo
                            _vehiculoPlaca = placa;
                            txtVehiculoNombre.Text = rd["NombreVehiculo"].ToString();
                            txtVehiculoTipo.Text = rd["TipoAño"].ToString();
                            txtVehiculoPropietario.Text = rd["NombreCompleto"].ToString();
                            borderVehiculoInfo.Visibility = Visibility.Visible;

                            // Cliente
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
            catch (Exception ex)
            {
                MostrarError("Error: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
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

        // ═══════════════════════════════════════════
        // CALCULAR TOTAL
        // ═══════════════════════════════════════════
        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            decimal.TryParse(
                txtPrecioRepuesto.Text.Replace("S/", "").Replace(",", "").Trim(),
                out decimal repuesto);
            decimal.TryParse(
                txtPrecioServicio.Text.Replace("S/", "").Replace(",", "").Trim(),
                out decimal servicio);

            txtCostoTotal.Text = $"S/ {(repuesto + servicio):N2}";
        }
    }
}