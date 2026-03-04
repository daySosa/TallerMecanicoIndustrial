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

namespace Órdenes_de_Trabajo
{
    /// <summary>
    /// Lógica de interacción para AgregarRepuesto.xaml
    /// </summary>
    public partial class AgregarRepuesto : Window
    {
        public AgregarRepuesto()
        {
            InitializeComponent();
        }

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Tu lógica aquí
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Tu lógica aquí
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            MainWindow ventana = new MainWindow();
            ventana.Show();
            this.Close();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Tu lógica para agregar repuesto
        }
    }
}
