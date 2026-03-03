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
    /// Lógica de interacción para ActualizarPago.xaml
    /// </summary>
    public partial class ActualizarPago : Window
    {
        public ActualizarPago()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreCliente.Text) ||
                string.IsNullOrWhiteSpace(txtFecha.Text) ||
                !int.TryParse(txtDNI.Text, out int dni) ||
                !int.TryParse(txtOrdenID.Text, out int ordenID) ||
                !double.TryParse(txtPrecio.Text, out double precio) )
            {
                MessageBox.Show("Complete todos los campos Obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }
}
