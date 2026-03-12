using System.Windows;
using System.Windows.Controls;

namespace Órdenes_de_Trabajo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void RecalcularTotal()
        {
            if (txtPrecioRepuesto == null || txtPrecioServicio == null || txtCostoTotal == null)
                return;

            double repuesto = ObtenerValor(txtPrecioRepuesto.Text);
            double servicio = ObtenerValor(txtPrecioServicio.Text);
            txtCostoTotal.Text = $"S/ {repuesto + servicio:N2}";
        }

        private double ObtenerValor(string texto)
        {
            texto = texto.Replace("S/", "").Trim();
            return double.TryParse(texto, out double v) ? v : 0;
        }

        private void txtPrecioServicio_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalcularTotal();
        }

        private void txtPrecioRepuesto_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecalcularTotal();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AgregarRepuesto ventana = new AgregarRepuesto();
            ventana.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MenúPrincipalOrdenes ventana = new MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();
        }
    }
}