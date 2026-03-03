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
    /// Lógica de interacción para MenuDePagos.xaml
    /// </summary>
    public partial class MenuDePagos : Window
    {
        public MenuDePagos()
        {
            InitializeComponent();
            CargarPago();
        }

        public void CargarPago()
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarPago();
            ventana.ShowDialog();
            CargarPago();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new ActualizarPago();
            ventana.ShowDialog();
            CargarPago();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var ventana = new ComprobanteDePago();
            ventana.ShowDialog();
            CargarPago();
        }
    }
}
