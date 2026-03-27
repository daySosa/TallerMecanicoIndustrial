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
        /// Cadena de conexión utilizada para acceder a la base de datos.
        /// </summary>
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        /// <summary>
        /// Referencia al menú principal de pagos para actualizar la información mostrada.
        /// </summary>
        private MenuDePagos _menuRef;

        /// <summary>
        /// Identificador único del pago que se va a actualizar.
        /// </summary>
        private int _pagoId;

        /// <summary>
        /// Fecha en la que fue registrado el pago.
        /// Se utiliza para validar si aún puede ser editado.
        /// </summary>
        private DateTime _fechaRegistro;

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ActualizarPago"/>
        /// y carga los datos del pago seleccionado.
        /// </summary>
        /// <param name="menuRef">Referencia al menú de pagos.</param>
        /// <param name="pagoId">Identificador del pago.</param>
        /// <param name="dni">DNI del cliente.</param>
        /// <param name="ordenId">Identificador de la orden de trabajo.</param>
        /// <param name="monto">Monto del pago.</param>
        /// <param name="fecha">Fecha de registro del pago.</param>
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

        /// <summary>
        /// Verifica si el pago puede ser editado.
        /// Si ha pasado más de un día desde su registro, se bloquean los campos de edición.
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
        /// Evento que se ejecuta al modificar el DNI.
        /// Permite buscar dinámicamente el nombre del cliente.
        /// </summary>
        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuscarNombre(txtDNI.Text.Trim());
        }

        /// <summary>
        /// Busca y muestra el nombre del cliente en función del DNI ingresado.
        /// Si no existe, muestra un mensaje de advertencia.
        /// </summary>
        /// <param name="dni">DNI del cliente.</param>
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
                    cmd.Parameters.AddWithValue("@DNI", dni); // string, no int
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
        /// Evento que se ejecuta al modificar el ID de la orden.
        /// Obtiene automáticamente el monto correspondiente desde la base de datos.
        /// </summary>
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
                    // Trae el monto actualizado desde Contabilidad_Pago si existe,
                    // si no cae al total de la orden
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
        /// Formatea el precio al perder el foco, aplicando formato de moneda.
        /// </summary>
        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.FormatearPrecio(txtPrecio.Text);
        }

        /// <summary>
        /// Limpia el formato del precio al obtener el foco, permitiendo editar el valor.
        /// </summary>
        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.LimpiarPrefijoPrecio(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        /// <summary>
        /// Valida los datos ingresados y actualiza el pago en la base de datos.
        /// También actualiza la vista principal al finalizar.
        /// </summary>
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();
            string montoStr = txtPrecio.Text.Replace("L", "").Replace(" ", "").Trim();

            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ El DNI es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarSoloDigitos(dni, "⚠ El DNI solo debe contener números.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreCliente.Text, "⚠ Ingresa un DNI válido.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(ordenStr, "⚠ El ID de la orden es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarEntero(ordenStr, out int ordenId, "⚠ El ID de la orden debe ser un número entero.", MostrarMensaje)) return;
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

        // <summary>
        /// Cancela la operación y cierra la ventana sin guardar cambios.
        /// </summary>
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Muestra un mensaje de validación o error en la interfaz.
        /// </summary>
        private void MostrarMensaje(string msg)
        {
            txtMensajeDNI.Text = msg;
            txtMensajeDNI.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Oculta el mensaje de validación en la interfaz.
        /// </summary>
        private void OcultarMensaje()
        {
            txtMensajeDNI.Visibility = Visibility.Collapsed;
        }
    }
}