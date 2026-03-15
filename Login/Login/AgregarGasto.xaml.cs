using Microsoft.Data.SqlClient;
using System.Windows;

namespace Contabilidad
{
    public partial class AgregarGasto : Window


    {

        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";
        public AgregarGasto()
        {
            InitializeComponent();
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
                MessageBox.Show("Selecciona un tipo de gasto.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombreGasto.Text))
            {
                MessageBox.Show("Escribe el nombre del gasto.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0)
            {
                MessageBox.Show("Ingresa un precio válido.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(conexion))
                {
                    string query = @"
                        INSERT INTO Contabilidad_Gastos 
                            (Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto)
                        VALUES 
                            (@TipoGasto, @NombreGasto, @Observaciones, @Precio, GETDATE())";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TipoGasto", ((System.Windows.Controls.ComboBoxItem)cmbTipoGasto.SelectedItem).Content.ToString());
                    cmd.Parameters.AddWithValue("@NombreGasto", txtNombreGasto.Text.Trim());
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text) ? (object)DBNull.Value : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Gasto guardado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el gasto: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
