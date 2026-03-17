using System;
using Microsoft.Data.SqlClient;
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

namespace Contabilidad
{
    public partial class ComprobanteDePago : Window
    {
        // ✅ Cambiado de LocalDB a Azure SQL
        private string _conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        public ComprobanteDePago(int pagoId)
        {
            InitializeComponent();
            CargarComprobante(pagoId);
        }

        private void CargarComprobante(int pagoId)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_conexion))
                {
                    string query = @"
                        SELECT 
                            Pago_ID, Precio_Pago, Fecha_Pago,
                            Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                            Orden_ID
                        FROM Vista_Pagos_Completos
                        WHERE Pago_ID = @PagoID";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PagoID", pagoId);
                    conn.Open();

                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        lblPagoID.Text = "#" + reader["Pago_ID"].ToString();
                        string nombres = reader["Cliente_Nombres"].ToString();
                        string apellidos = reader["Cliente_Apellidos"].ToString();
                        string inicial = apellidos.Length > 0 ? apellidos[0] + "." : "";
                        lblNombre.Text = nombres + " " + inicial;
                        lblDNI.Text = reader["Cliente_DNI"].ToString();
                        lblOrdenID.Text = "#" + reader["Orden_ID"].ToString();

                        decimal monto = Convert.ToDecimal(reader["Precio_Pago"]);
                        lblMonto.Text = "L " + monto.ToString("N2");

                        DateTime fecha = Convert.ToDateTime(reader["Fecha_Pago"]);
                        lblFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                                        new System.Globalization.CultureInfo("es-ES"));
                    }
                    else
                    {
                        MessageBox.Show("No se encontró el comprobante.", "Aviso",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar comprobante: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}