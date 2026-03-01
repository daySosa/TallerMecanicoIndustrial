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

namespace Vehículos
{
    /// <summary>
    /// Lógica de interacción para MenúPrincipalVehículos.xaml
    /// </summary>
    public partial class MenúPrincipalVehículos : Window
    {
        public MenúPrincipalVehículos()
        {
            InitializeComponent();
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            MainWindow ventana = new MainWindow();
            ventana.Show();
            this.Close();
        }
    }
}
