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
    /// <summary>
    /// Lógica de interacción para ActualizarGasto.xaml
    /// </summary>
    public partial class ActualizarGasto : Window
    {

        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        private int _gastoId;


        public ActualizarGasto(int gastoId, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            _gastoId = gastoId;
            CargarDatos(tipo, nombre, precio, fecha, observaciones);
        }

        private void CargarDatos(string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            // Seleccionar el tipo en el ComboBox
            foreach (ComboBoxItem item in cmbTipoGasto.Items)
            {
                if (item.Content.ToString() == tipo)
                {
                    cmbTipoGasto.SelectedItem = item;
                    break;
                }
            }

            txtNombre.Text = nombre;
            txtPrecio.Text = precio.ToString("F2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy HH:mm");
            txtObservaciones.Text = observaciones;
        }


        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbTipoGasto.SelectedItem == null)
            {
                MessageBox.Show("⚠ Selecciona un tipo de gasto.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("⚠ Escribe el nombre del gasto.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("⚠ Ingresa un precio válido mayor a 0.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateTime.TryParseExact(txtFecha.Text.Trim(), "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime fechaFinal))
            {
                MessageBox.Show("⚠ Formato de fecha inválido. Usa dd/MM/yyyy HH:mm\nEjemplo: 13/03/2026 14:30",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
