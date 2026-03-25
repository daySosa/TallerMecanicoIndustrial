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
using Login.Clases;

namespace Contabilidad
{
    public partial class ComprobanteDePago : Window
    {
        private clsConsultasBD _db = new clsConsultasBD();

        public ComprobanteDePago(int pagoId)
        {
            InitializeComponent();
            CargarComprobante(pagoId);
        }

        private void CargarComprobante(int pagoId)
        {
            try
            {
                var row = _db.ObtenerComprobantePago(pagoId);

                if (row != null)
                {
                    lblPagoID.Text = "#" + row["Pago_ID"].ToString();
                    string nombres = row["Cliente_Nombres"].ToString();
                    string apellidos = row["Cliente_Apellidos"].ToString();
                    string inicial = apellidos.Length > 0 ? apellidos[0] + "." : "";
                    lblNombre.Text = nombres + " " + inicial;
                    lblDNI.Text = row["Cliente_DNI"].ToString();
                    lblOrdenID.Text = "#" + row["Orden_ID"].ToString();

                    decimal monto = Convert.ToDecimal(row["Precio_Pago"]);
                    lblMonto.Text = "L " + monto.ToString("N2");

                    DateTime fecha = Convert.ToDateTime(row["Fecha_Pago"]);
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