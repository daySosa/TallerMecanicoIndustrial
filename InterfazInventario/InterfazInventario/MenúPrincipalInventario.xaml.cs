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

namespace InterfazInventario
{
    /// <summary>
    /// Lógica de interacción para MenúPrincipalInventario.xaml
    /// </summary>
    public partial class MenúPrincipalInventario : Window
    {
        public MenúPrincipalInventario()
        {
            InitializeComponent();
            CargarInventario();
        }

        public void CargarInventario()
        {
            // Conecta con tu base de datos:
            // using (var db = new TuContextoDB())
            // {
            //     var productos = db.Productos.ToList();
            //     dgInventario.ItemsSource = productos;
            //     tbTotalItems.Text = $"{productos.Count} items";
            // }
        }

        private void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MainWindow();
            ventana.ShowDialog();
            CargarInventario();
            
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // string filtro = txtBuscar.Text.ToLower();
            // dgInventario.ItemsSource = db.Productos
            //     .Where(p => p.Producto_Nombre.ToLower().Contains(filtro)).ToList();
        }

        private void dgInventario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
