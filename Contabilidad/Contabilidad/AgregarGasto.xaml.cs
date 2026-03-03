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
    /// Lógica de interacción para AgregarGasto.xaml
    /// </summary>
    public partial class AgregarGasto : Window
    {
        public AgregarGasto()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombreGasto.Text) ||
                string.IsNullOrWhiteSpace(txtObservaciones.Text) ||
                !double.TryParse(txtPrecio.Text, out double precio) ||
                cmbTipoGasto.SelectedItem == null)
            {
                MessageBox.Show("Complete todos los campos Obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
    }
}
