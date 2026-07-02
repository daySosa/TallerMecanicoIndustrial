using Login.Clases;
using System.Windows;

namespace Contabilidad
{
    /// <summary>
    /// Ventana que muestra el comprobante de un pago registrado.
    /// </summary>
    public partial class ComprobantePago : Window
    {
        private readonly RepositorioSql _db = new RepositorioSql();

        public ComprobantePago(int pagoId)
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
                    lblPagoID.Text = "#" + row["Pago_ID"];
                    lblDNI.Text = row["Cliente_DNI"].ToString();
                    lblOrdenID.Text = "#" + row["Orden_ID"];

                    string nombres = row["Cliente_Nombres"].ToString();
                    string apellidos = row["Cliente_Apellidos"].ToString();
                    string inicial = apellidos.Length > 0 ? apellidos[0] + "." : "";
                    lblNombre.Text = $"{nombres} {inicial}";

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

        private void btnCerrar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}