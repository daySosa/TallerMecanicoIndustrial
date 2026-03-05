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

        public void CargarPago(string busqueda = null)
        {
            string texto = txtBuscar.Text.Trim();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            AgregarPago ventana = new AgregarPago(this);
            ventana.ShowDialog();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dgPagos.SelectedItem == null)
            ventana.ShowDialog();
        }

        private void btnMostrarComprobantes_Click(object sender, RoutedEventArgs e)
        {
            if (dgPagos.SelectedItem == null)
            ventana.ShowDialog();
        }
    }
}
