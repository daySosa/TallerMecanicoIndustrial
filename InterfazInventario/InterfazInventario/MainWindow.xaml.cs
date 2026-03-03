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

namespace InterfazInventario
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

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El producto está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El producto está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
            iconEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Tu lógica para agregar
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            // Tu lógica para actualizar
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            MenúPrincipalInventario ventana = new MenúPrincipalInventario();
            ventana.Show();
            this.Close();
        }
    }
}