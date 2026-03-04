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
    /// Lógica de interacción para DetalleOrdenTrabajo.xaml
    /// </summary>
    public partial class DetalleOrdenTrabajo : Window
    {
        public DetalleOrdenTrabajo()
        {
            InitializeComponent();
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (panelBasicos == null) return;

            var rb = sender as RadioButton;
            if (rb == null) return;

            panelBasicos.Visibility = Visibility.Collapsed;
            panelDescripcion.Visibility = Visibility.Collapsed;
            panelHistorial.Visibility = Visibility.Collapsed;

            switch (rb.Tag?.ToString())
            {
                case "Basicos":
                    panelBasicos.Visibility = Visibility.Visible;
                    break;
                case "Descripcion":
                    panelDescripcion.Visibility = Visibility.Visible;
                    break;
                case "Historial":
                    panelHistorial.Visibility = Visibility.Visible;
                    break;
            }
        }
    }
}
