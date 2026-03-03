using System;
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
    /// <summary>
    /// Lógica de interacción para AgregarPago.xaml
    /// </summary>
    public partial class AgregarPago : Window
    {
        public AgregarPago()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtDNI.Text) ||
            string.IsNullOrWhiteSpace(txtNombreCliente.Text) ||
            !int.TryParse(txtOrdenID.Text, out int nOrden) ||
            !double.TryParse(txtMonto.Text, out double monto))
            {
                MessageBox.Show("Complete todos los campos Obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }
}
