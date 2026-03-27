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
    /// Ventana encargada de permitir la edición de un gasto registrado en el sistema.
    /// Incluye validaciones, carga de datos y restricción de edición según la fecha de registro.
    /// </summary>
    public partial class ActualizarGasto : Window
    {
        /// <summary>
        /// Cadena de conexión utilizada para acceder a la base de datos.
        /// </summary>
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        /// <summary>
        /// Identificador único del gasto que se va a actualizar.
        /// </summary>
        private int _gastoId;

        /// <summary>
        /// Fecha en la que fue registrado el gasto.
        /// Se utiliza para validar si aún es editable.
        /// </summary>
        private DateTime _fechaRegistro;

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ActualizarGasto"/>
        /// y carga los datos del gasto seleccionado.
        /// </summary>
        /// <param name="gastoId">Identificador del gasto.</param>
        /// <param name="tipo">Tipo de gasto.</param>
        /// <param name="nombre">Nombre del gasto.</param>
        /// <param name="precio">Monto del gasto.</param>
        /// <param name="fecha">Fecha de registro del gasto.</param>
        /// <param name="observaciones">Observaciones adicionales del gasto.</param>
        public ActualizarGasto(int gastoId, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            _gastoId = gastoId;
            _fechaRegistro = fecha; 
            CargarDatos(tipo, nombre, precio, fecha, observaciones);
            VerificarBloqueoEdicion(); 
        }


        /// <summary>
        /// Verifica si el gasto puede ser editado.
        /// Si ha pasado más de un día desde su registro, se bloquean los controles de edición.
        /// </summary>
        private void VerificarBloqueoEdicion()
        {
            if ((DateTime.Now - _fechaRegistro).TotalDays >= 1)
            {
                cmbTipoGasto.IsEnabled = false;
                txtNombre.IsEnabled = false;
                txtPrecio.IsEnabled = false;
                txtObservaciones.IsEnabled = false;
                btnGuardar.IsEnabled = false;

                MessageBox.Show(
                    "⚠ Este gasto ya no puede editarse porque tiene más de 1 día de haber sido registrado.",
                    "Edición bloqueada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Carga los datos del gasto en los controles de la interfaz para su visualización y edición.
        /// </summary>
        /// <param name="tipo">Tipo de gasto.</param>
        /// <param name="nombre">Nombre del gasto.</param>
        /// <param name="precio">Monto del gasto.</param>
        /// <param name="fecha">Fecha de registro.</param>
        /// <param name="observaciones">Observaciones del gasto.</param>
        private void CargarDatos(string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            foreach (ComboBoxItem item in cmbTipoGasto.Items)
            {
                if (item.Content.ToString() == tipo)
                {
                    cmbTipoGasto.SelectedItem = item;
                    break;
                }
            }

            txtNombre.Text = nombre;
            txtPrecio.Text = "L " + precio.ToString("N2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy HH:mm");
            txtObservaciones.Text = observaciones;
        }

        /// <summary>
        /// Cancela la operación de edición y cierra la ventana sin guardar cambios.
        /// </summary>       
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Formatea el valor del precio al perder el foco, aplicando el formato de moneda.
        /// </summary>
        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.FormatearPrecio(txtPrecio.Text);
        }

        /// <summary>
        /// Limpia el formato del precio al obtener el foco, permitiendo al usuario editar el valor numérico.
        /// </summary>
        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.LimpiarPrefijoPrecio(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        /// <summary>
        /// Valida los datos ingresados y actualiza el gasto en la base de datos.
        /// Si la operación es exitosa, muestra un mensaje de confirmación y cierra la ventana.
        /// </summary>
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarComboSeleccionado(cmbTipoGasto.SelectedItem, "tipo de gasto")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombre.Text, "nombre del gasto")) return;
            if (!clsValidaciones.ValidarPrecio(txtPrecio.Text, out decimal precio)) return;
            if (!clsValidaciones.ValidarFecha(txtFecha.Text, out DateTime fechaFinal)) return;

            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    string query = @"
                    UPDATE Contabilidad_Gastos SET
                        Tipo_Gasto          = @TipoGasto,
                        Nombre_Gasto        = @NombreGasto,
                        Observaciones_Gasto = @Observaciones,
                        Precio_Gasto        = @Precio,
                        Fecha_Gasto         = @Fecha
                    WHERE Gasto_ID = @GastoID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TipoGasto", ((ComboBoxItem)cmbTipoGasto.SelectedItem).Content.ToString());
                    cmd.Parameters.AddWithValue("@NombreGasto", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.Parameters.AddWithValue("@Fecha", fechaFinal);
                    cmd.Parameters.AddWithValue("@GastoID", _gastoId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Gasto actualizado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ Error al actualizar el gasto: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}