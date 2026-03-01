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

namespace InterfazClientes
{
    /// <summary>
    /// Lógica de interacción para MenúPrincipalClientes.xaml
    /// </summary>
    public partial class MenúPrincipalClientes : Window
    {
        public MenúPrincipalClientes()
        {
            InitializeComponent();
            CargarClientes();
        }

        public void CargarClientes()
        {
            // Conecta con tu base de datos:
            // using (var db = new TuContextoDB())
            // {
            //     var clientes = db.Clientes.ToList();
            //     dgClientes.ItemsSource = clientes;
            //     tbTotalClientes.Text = $"{clientes.Count} clientes";
            // }
        }

        private void btnAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MainWindow();
            ventana.ShowDialog();
            CargarClientes();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // string filtro = txtBuscar.Text.ToLower();
            // dgClientes.ItemsSource = db.Clientes
            //     .Where(c => c.Cliente_Nombre.ToLower().Contains(filtro)
            //              || c.Cliente_Apellido.ToLower().Contains(filtro)).ToList();
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
