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
        /// <summary>
        /// Cadena de conexión a la base de datos del sistema.
        /// </summary>
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        /// <summary>
        /// Referencia al menú de pagos que invocó esta ventana.
        /// </summary>
        private MenuDePagos _menuRef;

        /// <summary>
        /// Identificador único del pago que se está editando.
        /// </summary>
        private int _pagoId;

        /// <summary>
        /// Fecha en que fue registrado originalmente el pago.
        /// </summary>
        private DateTime _fechaRegistro;

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ActualizarPago"/>.
        /// Carga los datos del pago en los campos del formulario y aplica las validaciones iniciales.
        /// </summary>
        /// <param name="menuRef">Referencia al menú de pagos padre.</param>
        /// <param name="pagoId">Identificador del pago a actualizar.</param>
        /// <param name="dni">DNI del cliente asociado al pago.</param>
        /// <param name="ordenId">Identificador de la orden de trabajo vinculada.</param>
        /// <param name="monto">Monto registrado del pago.</param>
        /// <param name="fecha">Fecha en que fue registrado el pago.</param>
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

            txtDNI.IsEnabled = false;
            txtOrdenID.IsEnabled = false;

            BuscarNombre(dni);
            VerificarBloqueoEdicion();
        }

        /// <summary>
        /// Verifica si el pago supera el límite de tiempo permitido para edición (1 día).
        /// En caso afirmativo, deshabilita todos los campos del formulario y muestra un mensaje de advertencia.
        /// </summary>
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

        /// <summary>
        /// Maneja el evento de cambio de texto en el campo DNI.
        /// Actualiza el nombre del cliente mostrado según el DNI ingresado.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuscarNombre(txtDNI.Text.Trim());
        }

        /// <summary>
        /// Busca en la base de datos el nombre completo del cliente a partir de su DNI
        /// y lo muestra en el campo correspondiente. Muestra un mensaje de error si no se encuentra.
        /// </summary>
        /// <param name="dni">DNI del cliente a buscar.</param>
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

        /// <summary>
        /// Maneja el evento de cambio de texto en el campo de ID de orden.
        /// Consulta el monto asociado a la orden ingresada y lo muestra en el campo de precio.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
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

        /// <summary>
        /// Maneja el evento de pérdida de foco del campo de precio.
        /// Formatea el valor ingresado al formato de moneda establecido.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidacionesContabilidad.FormatearPrecioGasto(txtPrecio.Text);
        }

        /// <summary>
        /// Maneja el evento de obtención de foco del campo de precio.
        /// Limpia el formato de moneda para facilitar la edición del valor numérico.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidacionesContabilidad.LimpiarPrecioGasto(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        /// <summary>
        /// Maneja el evento Click del botón Guardar.
        /// Valida los datos del formulario y ejecuta la actualización del pago en la base de datos.
        /// Recarga el menú de pagos y cierra la ventana si la operación es exitosa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();

            if (!clsValidacionesContabilidad.ValidarFormularioVacio(
                  dni, ordenStr, txtPrecio.Text)) return;

            if (!clsValidacionesContabilidad.ValidarDNIPago(dni, MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreCliente.Text, "⚠ Ingresa un DNI válido.", MostrarMensaje)) return;
            if (!clsValidacionesContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;
            if (!clsValidacionesContabilidad.ValidarMontoPago(txtPrecio.Text, out decimal monto)) return;

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

        /// <summary>
        /// Maneja el evento Click del botón Cancelar.
        /// Cierra la ventana sin guardar ningún cambio.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Muestra un mensaje de validación o error en el panel de avisos de la ventana.
        /// </summary>
        /// <param name="msg">Texto del mensaje a mostrar.</param>
        private void MostrarMensaje(string msg)
        {
            txtMensajeDNI.Text = msg;
            txtMensajeDNI.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Oculta el panel de mensajes de validación o error.
        /// </summary>
        private void OcultarMensaje()
        {
            txtMensajeDNI.Visibility = Visibility.Collapsed;
        }
    }
}