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

namespace Órdenes_de_Trabajo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnCalcular_Click(object sender, RoutedEventArgs e)
        {
            // Tu lógica para calcular total
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AgregarRepuesto ventana = new AgregarRepuesto();
            ventana.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MenúPrincipalOrdenes ventana = new MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();
        }
    }
}