using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Contabilidad
{
    public partial class ActualizarPago : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        private MenuDePagos _menuRef;
        private int _pagoId;

        public ActualizarPago(MenuDePagos menuRef, int pagoId, string dni, int ordenId, decimal monto, DateTime fecha)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _pagoId = pagoId;

            // Pre-llenar campos
            txtDNI.Text = dni;
            txtOrdenID.Text = ordenId.ToString();
            txtPrecio.Text = monto.ToString("N2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                              new System.Globalization.CultureInfo("es-ES"));

            // Buscar nombre del cliente
            BuscarNombre(dni);

            // Actualizar monto al cambiar orden
            txtOrdenID.TextChanged += txtOrdenID_TextChanged;
            txtDNI.TextChanged += txtDNI_TextChanged;
        }

        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuscarNombre(txtDNI.Text.Trim());
        }

        private void BuscarNombre(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                txtNombreCliente.Text = "";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT Cliente_Nombres, Cliente_Apellidos FROM Cliente WHERE Cliente_DNI = @DNI";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {
                        txtNombreCliente.Text = reader["Cliente_Nombres"].ToString() + " " + reader["Cliente_Apellidos"].ToString();
                        txtNombreCliente.Foreground = System.Windows.Media.Brushes.White;
                        OcultarMensaje();
                    }
                    else
                    {
                        txtNombreCliente.Text = "";
                        MostrarMensaje("No se encontró ningún cliente con ese DNI.");
                    }
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error: " + ex.Message);
            }
        }

        private void txtOrdenID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(txtOrdenID.Text.Trim(), out int ordenId))
            {
                txtPrecio.Text = "0.00";
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT OrdenPrecio_Total FROM Orden_Trabajo WHERE Orden_ID = @OrdenID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    txtPrecio.Text = (result != null && result != DBNull.Value)
                        ? Convert.ToDecimal(result).ToString("N2")
                        : "0.00";
                }
            }
            catch
            {
                txtPrecio.Text = "0.00";
            }
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();
            string montoStr = txtPrecio.Text.Trim();

            if (string.IsNullOrEmpty(dni) || string.IsNullOrEmpty(txtNombreCliente.Text))
            {
                MostrarMensaje("Ingresa un DNI válido.");
                return;
            }
            if (!int.TryParse(ordenStr, out int ordenId))
            {
                MostrarMensaje("El ID de la orden debe ser un número.");
                return;
            }
            if (!decimal.TryParse(montoStr, out decimal monto) || monto <= 0)
            {
                MostrarMensaje("El monto no es válido.");
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"
                        UPDATE Contabilidad_Pago
                        SET Cliente_DNI = @DNI,
                            Orden_ID    = @OrdenID,
                            Precio_Pago = @Monto
                        WHERE Pago_ID = @PagoID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DNI", Convert.ToInt32(dni));
                    cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                    cmd.Parameters.AddWithValue("@Monto", monto);
                    cmd.Parameters.AddWithValue("@PagoID", _pagoId);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("¡Pago actualizado correctamente!", "Éxito",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                _menuRef.CargarPago();
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error al actualizar: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MostrarMensaje(string msg)
        {
            txtMensajeDNI.Text = msg;
            txtMensajeDNI.Visibility = Visibility.Visible;
        }

        private void OcultarMensaje()
        {
            txtMensajeDNI.Visibility = Visibility.Collapsed;
        }
    }
}
