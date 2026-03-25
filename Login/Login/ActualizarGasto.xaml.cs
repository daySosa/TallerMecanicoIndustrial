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
    public partial class ActualizarGasto : Window
    {
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";
        private int _gastoId;
        private DateTime _fechaRegistro; // ← NUEVO

        public ActualizarGasto(int gastoId, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            _gastoId = gastoId;
            _fechaRegistro = fecha; // ← NUEVO
            CargarDatos(tipo, nombre, precio, fecha, observaciones);
            VerificarBloqueoEdicion(); // ← NUEVO
        }

        // ← NUEVO
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

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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