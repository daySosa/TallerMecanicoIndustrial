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
    /// Lógica de interacción para ActualizarGasto.xaml
    /// </summary>
    public partial class ActualizarGasto : Window
    {
        public ActualizarGasto()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindow ventana = new MainWindow();
            ventana.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
            string.IsNullOrWhiteSpace(txtPrecio.Text) ||
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
