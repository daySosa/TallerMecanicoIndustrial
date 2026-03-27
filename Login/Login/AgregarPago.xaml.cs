using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;
using Login.Clases;

namespace Contabilidad
{
    public partial class AgregarPago : Window
    {
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        private MenuDePagos _menuRef;
        private bool _esEdicion = false;
        private int _pagoId = 0;

        public AgregarPago(MenuDePagos menuRef)
        {
            InitializeComponent();
            _menuRef = menuRef;
        }

        public AgregarPago(MenuDePagos menuRef, int pagoId, string dni, int ordenId, decimal monto)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _esEdicion = true;
            _pagoId = pagoId;

            Title = "Actualizar Pago";
            txtDNI.Text = dni;
            txtOrdenID.Text = ordenId.ToString();

            BuscarCliente(dni);
        }

        private void txtDNI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtNombre.Text = "";
            panelOrdenes.Visibility = Visibility.Collapsed;
            dgOrdenes.ItemsSource = null;
            txtOrdenID.Text = "";
            txtMonto.Text = "";
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();
            if (!clsValidaciones.ValidarDNIHondureño(txtDNI.Text.Trim())) return;
            BuscarCliente(txtDNI.Text.Trim());
        }

        private void BuscarCliente(string dni)
        {
            OcultarMensaje();
            if (!clsValidaciones.ValidarTextoRequerido(dni, "Ingresa un DNI primero.", MostrarMensaje)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    string query = "SELECT Cliente_Nombres, Cliente_Apellidos FROM Cliente WHERE Cliente_DNI = @DNI";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        txtNombre.Text = reader["Cliente_Nombres"].ToString() + " " + reader["Cliente_Apellidos"].ToString();
                        txtNombre.Foreground = System.Windows.Media.Brushes.White;
                        reader.Close();
                        CargarOrdenesCliente(dni);
                    }
                    else
                    {
                        txtNombre.Text = "";
                        panelOrdenes.Visibility = Visibility.Collapsed;
                        MostrarMensaje("No se encontró ningún cliente con ese DNI.");
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al buscar cliente: " + ex.Message);
            }
        }

        private void CargarOrdenesCliente(string dni)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    string query = @"
                        SELECT ot.Orden_ID, ot.Vehiculo_Placa, ot.Estado,
                               ot.OrdenPrecio_Total, ot.Fecha
                        FROM Orden_Trabajo ot
                        WHERE ot.Cliente_DNI = @DNI
                            AND ot.Estado = 'Finalizado'
                            AND NOT EXISTS (
                                SELECT 1 FROM Contabilidad_Pago cp
                                WHERE cp.Orden_ID = ot.Orden_ID
                                AND (@PagoID = -1 OR cp.Pago_ID <> @PagoID)
                            )
                        ORDER BY ot.Fecha DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    cmd.Parameters.AddWithValue("@PagoID", _esEdicion ? _pagoId : -1);
                    conn.Open();

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    if (dt.Rows.Count > 0)
                    {
                        dgOrdenes.ItemsSource = dt.DefaultView;
                        panelOrdenes.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        dgOrdenes.ItemsSource = null;
                        panelOrdenes.Visibility = Visibility.Collapsed;
                        MostrarMensaje("Este cliente no tiene órdenes finalizadas sin pago registrado.");
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al cargar órdenes: " + ex.Message);
            }
        }

        private void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is DataRowView row)
            {
                txtOrdenID.Text = row["Orden_ID"].ToString();
            }
        }

        private void txtOrdenID_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarMensaje();

            if (!int.TryParse(txtOrdenID.Text.Trim(), out int ordenId))
            {
                txtMonto.Text = "L 0.00";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    string query = @"
                        SELECT COALESCE(cp.Precio_Pago, ot.OrdenPrecio_Total)
                        FROM Orden_Trabajo ot
                        LEFT JOIN Contabilidad_Pago cp 
                            ON cp.Orden_ID = ot.Orden_ID
                            AND (@PagoID = -1 OR cp.Pago_ID = @PagoID)
                        WHERE ot.Orden_ID = @OrdenID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                    cmd.Parameters.AddWithValue("@PagoID", _esEdicion ? _pagoId : -1);
                    conn.Open();
                    object result = cmd.ExecuteScalar();

                    txtMonto.Text = (result != null && result != DBNull.Value)
                        ? "L " + Convert.ToDecimal(result).ToString("N2")
                        : "L 0.00";
                }
            }
            catch
            {
                txtMonto.Text = "L 0.00";
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();

            if (!clsValidaciones.ValidarDNIHondureño(dni)) return;
            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ Busca un cliente válido antes de guardar.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "⚠ Busca un cliente válido antes de guardar.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarEntero(ordenStr, out int ordenId, "⚠ El ID de la orden debe ser un número.", MostrarMensaje)) return;

            try
            {
                decimal montoActualizado = 0;
                using (SqlConnection connCheck = new SqlConnection(conexion))
                {
                    string qMonto = @"
                        SELECT COALESCE(cp.Precio_Pago, ot.OrdenPrecio_Total)
                        FROM Orden_Trabajo ot
                        LEFT JOIN Contabilidad_Pago cp
                            ON cp.Orden_ID = ot.Orden_ID
                            AND (@PagoID = -1 OR cp.Pago_ID = @PagoID)
                        WHERE ot.Orden_ID = @OID";

                    SqlCommand cmdMonto = new SqlCommand(qMonto, connCheck);
                    cmdMonto.Parameters.AddWithValue("@OID", ordenId);
                    cmdMonto.Parameters.AddWithValue("@PagoID", _esEdicion ? _pagoId : -1);
                    connCheck.Open();
                    object res = cmdMonto.ExecuteScalar();

                    if (res == null || res == DBNull.Value)
                    {
                        MostrarMensaje("⚠ No se encontró la orden especificada.");
                        return;
                    }
                    montoActualizado = Convert.ToDecimal(res);
                }

                txtMonto.Text = "L " + montoActualizado.ToString("N2");

                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    conn.Open();

                    if (!_esEdicion)
                    {
                        SqlCommand cmd = new SqlCommand("sp_RegistrarPago", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClienteDNI", dni);
                        cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                        cmd.Parameters.AddWithValue("@Monto", montoActualizado);
                        cmd.ExecuteNonQuery();

                        MessageBox.Show("✅ ¡Pago registrado correctamente!", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string updateQuery = @"
                            UPDATE Contabilidad_Pago
                            SET Cliente_DNI = @DNI,
                                Orden_ID    = @OrdenID,
                                Precio_Pago = @Monto
                            WHERE Pago_ID = @PagoID";

                        SqlCommand cmd = new SqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@DNI", dni);
                        cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                        cmd.Parameters.AddWithValue("@Monto", montoActualizado);
                        cmd.Parameters.AddWithValue("@PagoID", _pagoId);
                        cmd.ExecuteNonQuery();

                        MessageBox.Show("✅ ¡Pago actualizado correctamente!", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                _menuRef.CargarPago();
                this.Close();
            }
            catch (SqlException sqlEx)
            {
                MostrarMensaje("⚠ " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                MostrarMensaje("⚠ Error inesperado: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MostrarMensaje(string msg)
        {
            txtMensaje.Text = msg;
            txtMensaje.Visibility = Visibility.Visible;
        }

        private void OcultarMensaje()
        {
            txtMensaje.Visibility = Visibility.Collapsed;
        }
    }
}