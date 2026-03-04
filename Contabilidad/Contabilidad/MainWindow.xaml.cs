using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Contabilidad
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CargarEgreso();
        }

        public void CargarEgreso()
        {
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarGasto();
            ventana.ShowDialog();
            CargarEgreso();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new ActualizarGasto();
            ventana.ShowDialog();
            CargarEgreso();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
                MessageBox.Show("Selecciona un gasto para ver el comprobante.", "Aviso",
            ventana.ShowDialog();
        }
    }
}