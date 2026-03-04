using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Contabilidad
{
    public partial class MainWindow : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        public MainWindow()
        {
            InitializeComponent();
            CargarEgreso();
            CargarNotificaciones();
        }

        public void CargarEgreso(string busqueda = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Gasto_ID, Tipo_Gasto, Nombre_Gasto, Precio_Gasto, Fecha_Gasto
                        FROM Contabilidad_Gastos
                        WHERE (@Busqueda IS NULL
                               OR Nombre_Gasto LIKE '%' + @Busqueda + '%'
                               OR Tipo_Gasto   LIKE '%' + @Busqueda + '%')
                        ORDER BY Fecha_Gasto DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Busqueda", (object)busqueda ?? DBNull.Value);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);

                    dgGastos.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gastos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarEgreso(string.IsNullOrEmpty(texto) ? null : texto);
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarGasto();
            ventana.Owner = this;
            if (ventana.ShowDialog() == true)
                CargarEgreso();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un gasto para actualizar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fila = (DataRowView)dgGastos.SelectedItem;

            var ventana = new ActualizarGasto(
                gastoId: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString(),
                nombre: fila["Nombre_Gasto"].ToString(),
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto") && fila["Observaciones_Gasto"] != DBNull.Value
                               ? fila["Observaciones_Gasto"].ToString()
                               : ""
            );

            ventana.Owner = this;
            if (ventana.ShowDialog() == true)
                CargarEgreso();
        }

        private void btnMostrarComprobante_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un gasto para ver el comprobante.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fila = (DataRowView)dgGastos.SelectedItem;

            var ventana = new MostrarComprobante(
                id: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString(),
                nombre: fila["Nombre_Gasto"].ToString(),
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto") && fila["Observaciones_Gasto"] != DBNull.Value
                               ? fila["Observaciones_Gasto"].ToString()
                               : ""
            );

            ventana.Owner = this;
            ventana.ShowDialog();
        }
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            CargarEgreso();
        }

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            // Aquí abres la ventana de ingresos cuando la tengas
        }

        private void btnPantallaPrincipal_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a la pantalla principal
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas al inventario
        }

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a vehículos
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a clientes
        }

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a órdenes
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Estás seguro que deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                this.Close();
            }
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

                    if (cantidad > 0)
                    {
                        badgeNotificaciones.Visibility = Visibility.Visible;
                        txtContadorNotificaciones.Text = cantidad.ToString();
                    }
                    else
                    {
                        badgeNotificaciones.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Crear VentanaNotificaciones
            MessageBox.Show("Próximamente.", "Info",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}

