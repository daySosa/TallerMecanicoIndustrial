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

namespace Vehículos
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
            txtEstadoLabel.Text = "El vehículo está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            !int.TryParse(txtAnio.Text, out int año) ||
             cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("Complete todos los campos Obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }


        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            !int.TryParse(txtAnio.Text, out int anio) ||
            cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("Complete to dos los campos obligatorios", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            MenúPrincipalVehículos ventana = new MenúPrincipalVehículos();
            ventana.Show();
            this.Close();
        }

        private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}