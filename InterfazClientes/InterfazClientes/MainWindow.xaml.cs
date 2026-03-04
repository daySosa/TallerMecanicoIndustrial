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

namespace InterfazClientes
{
    public partial class MainWindow : Window
    {
        public Cliente ClienteResultado { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            txtEstadoLabel.Text = "El cliente está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            txtEstadoLabel.Text = "El cliente está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtDPI.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("Por favor completa los campos obligatorios:\nNombre, Apellido, DPI y Teléfono.",
                    "Campos requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClienteResultado = new Cliente
            {
                Cliente_DPI = txtDPI.Text.Trim(),
                Cliente_Nombre = txtNombre.Text.Trim(),
                Cliente_Apellido = txtApellido.Text.Trim(),
                Cliente_Telefono = txtTelefono.Text.Trim(),
                Cliente_Correo = txtCorreo.Text.Trim(),
                Cliente_Direccion = txtDireccion.Text.Trim(),
                Cliente_Activo = toggleActivo.IsChecked == true
            };
        }

            private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtDPI.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("Por favor completa los campos obligatorios:\nNombre, Apellido, DPI y Teléfono.",
                    "Campos requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        }
    }
}
