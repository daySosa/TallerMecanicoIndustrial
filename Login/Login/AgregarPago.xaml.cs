using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
            txtMonto.Text = "L " + monto.ToString("N2");

            BuscarCliente(dni);
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
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
                    }
                    else
                    {
                        txtNombre.Text = "";
                        MostrarMensaje("No se encontró ningún cliente con ese DNI.");
                    }
                }
            catch (Exception ex)
            {
                MostrarMensaje("Error al buscar cliente: " + ex.Message);
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
                    string query = "SELECT OrdenPrecio_Total FROM Orden_Trabajo WHERE Orden_ID = @OrdenID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    txtMonto.Text = (result != null && result != DBNull.Value)
                        ? "L " + Convert.ToDecimal(result).ToString("N2")
                        : "L 0.00";
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
            string montoStr = txtMonto.Text.Replace("L", "").Replace(" ", "").Trim();

            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ Busca un cliente válido antes de guardar.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "⚠ Busca un cliente válido antes de guardar.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarEntero(ordenStr, out int ordenId, "⚠ El ID de la orden debe ser un número.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarPrecio(montoStr, out decimal monto, MostrarMensaje)) return;

            try
            {
                    if (!_esEdicion)
                    {
                        SqlCommand cmd = new SqlCommand("sp_RegistrarPago", conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ClienteDNI", dni);
                        cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                        cmd.Parameters.AddWithValue("@Monto", monto);
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
                        cmd.Parameters.AddWithValue("@Monto", monto);
                        cmd.Parameters.AddWithValue("@PagoID", _pagoId);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("✅ ¡Pago actualizado correctamente!", "Éxito",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                _menuRef.CargarPago();
                this.Close();
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