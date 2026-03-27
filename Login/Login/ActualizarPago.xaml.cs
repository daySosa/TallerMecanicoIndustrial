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
using Login.Clases;

namespace Contabilidad
{
    /// <summary>
    /// Ventana encargada de actualizar la información de un pago registrado en el sistema.
    /// Incluye validaciones, carga dinámica de datos y restricción de edición según la fecha.
    /// </summary>
    public partial class ActualizarPago : Window
    {
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";
        private MenuDePagos _menuRef;
        private int _pagoId;
        private DateTime _fechaRegistro;

        public ActualizarPago(MenuDePagos menuRef, int pagoId, string dni, int ordenId, decimal monto, DateTime fecha)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _pagoId = pagoId;
            _fechaRegistro = fecha;

            txtDNI.Text = dni;
            txtOrdenID.Text = ordenId.ToString();
            txtPrecio.Text = "L " + monto.ToString("N2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                              new System.Globalization.CultureInfo("es-ES"));

            BuscarNombre(dni);

            txtOrdenID.TextChanged += txtOrdenID_TextChanged;
            txtDNI.TextChanged += txtDNI_TextChanged;

            VerificarBloqueoEdicion();
        }

        private void VerificarBloqueoEdicion()
        {
            if ((DateTime.Now - _fechaRegistro).TotalDays >= 1)
            {
                txtDNI.IsEnabled = false;
                txtOrdenID.IsEnabled = false;
                txtPrecio.IsEnabled = false;
                btnGuardar.IsEnabled = false;

                MessageBox.Show(
                    "⚠ Este pago ya no puede editarse porque tiene más de 1 día de haber sido registrado.",
                    "Edición bloqueada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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
                using (SqlConnection conn = new SqlConnection(conexion))
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
                txtPrecio.Text = "L 0.00";
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
                            AND cp.Pago_ID = @PagoID
                        WHERE ot.Orden_ID = @OrdenID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                    cmd.Parameters.AddWithValue("@PagoID", _pagoId);
                    conn.Open();
                    object result = cmd.ExecuteScalar();
                    txtPrecio.Text = (result != null && result != DBNull.Value)
                        ? "L " + Convert.ToDecimal(result).ToString("N2")
                        : "L 0.00";
                }
            }
            catch
            {
                txtPrecio.Text = "L 0.00";
            }
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.FormatearPrecio(txtPrecio.Text);
        }

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.LimpiarPrefijoPrecio(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();
            string montoStr = txtPrecio.Text.Replace("L", "").Replace(" ", "").Trim();

            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ El DNI es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarSoloDigitos(dni, "⚠ El DNI solo debe contener números.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreCliente.Text, "⚠ Ingresa un DNI válido.", MostrarMensaje)) return;
            if (!clsValidacionesContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;
            if (!clsValidaciones.ValidarTextoRequerido(montoStr, "⚠ El monto es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarPrecio(montoStr, out decimal monto, MostrarMensaje)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    conn.Open();
                    string query = @"
                        UPDATE Contabilidad_Pago
                        SET Cliente_DNI = @DNI,
                            Orden_ID    = @OrdenID,
                            Precio_Pago = @Monto
                        WHERE Pago_ID = @PagoID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@DNI", dni);
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
                MostrarMensaje("⚠ Error al actualizar: " + ex.Message);
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