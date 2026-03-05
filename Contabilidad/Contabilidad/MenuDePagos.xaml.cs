using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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

    public partial class MenuDePagos : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        public MenuDePagos()
        {
            InitializeComponent();
            CargarPago();
            CargarNotificaciones();
        }

        public void CargarPago(string busqueda = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT 
                            Pago_ID,
                            Cliente_DNI,
                            Cliente_Nombres,
                            Orden_ID,
                            Precio_Pago,
                            Fecha_Pago
                        FROM Vista_Pagos_Completos
                        WHERE (@Busqueda IS NULL
                               OR CAST(Pago_ID AS VARCHAR) LIKE '%' + @Busqueda + '%'
                               OR Cliente_Nombres        LIKE '%' + @Busqueda + '%'
                               OR Cliente_Apellidos      LIKE '%' + @Busqueda + '%')
                        ORDER BY Fecha_Pago DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Busqueda", (object)busqueda ?? DBNull.Value);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);

                    dgPagos.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar pagos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarPago(string.IsNullOrEmpty(texto) ? null : texto);
        }


        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            AgregarPago ventana = new AgregarPago(this);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dgPagos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un pago del registro para actualizarlo.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataRowView fila = (DataRowView)dgPagos.SelectedItem;

            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            string dniStr = fila["Cliente_DNI"].ToString();
            int ordenId = Convert.ToInt32(fila["Orden_ID"]);
            decimal monto = Convert.ToDecimal(fila["Precio_Pago"]);
            DateTime fecha = Convert.ToDateTime(fila["Fecha_Pago"]);

            ActualizarPago ventana = new ActualizarPago(this, pagoId, dniStr, ordenId, monto, fecha);
            ventana.Owner = this;
            ventana.ShowDialog();
        }


        private void btnMostrarComprobantes_Click(object sender, RoutedEventArgs e)
        {
            if (dgPagos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un pago del registro para ver su comprobante.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            DataRowView fila = (DataRowView)dgPagos.SelectedItem;
            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            ComprobanteDePago ventana = new ComprobanteDePago(pagoId);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            PanelNotificaciones ventana = new PanelNotificaciones(onCerrar: () => CargarNotificaciones());
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void CargarNotificaciones()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "SELECT COUNT(*) FROM Notificaciones WHERE Leida = 0";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    conn.Open();
                    int cantidad = (int)cmd.ExecuteScalar();

                    badgeNotificaciones.Visibility = cantidad > 0
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                    txtContadorNotificaciones.Text = cantidad.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }
    }
}
