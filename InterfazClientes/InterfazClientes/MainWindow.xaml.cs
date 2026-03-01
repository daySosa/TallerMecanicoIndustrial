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
        }

            // Guardar en base de datos:
            // using (var db = new TuContextoDB())
            // {
            //     var cliente = new Cliente
            //     {
            //         Cliente_Nombre   = txtNombre.Text,
            //         Cliente_Apellido = txtApellido.Text,
            //         Cliente_DPI      = txtDPI.Text,
            //         Cliente_Telefono = txtTelefono.Text,
            //         Cliente_Correo   = txtCorreo.Text,
            //         Cliente_Direccion = txtDireccion.Text,
            //         Cliente_Activo   = toggleActivo.IsChecked == true
            //     };
            //     db.Clientes.Add(cliente);
            //     db.SaveChanges();
            // }

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

            // Actualizar en base de datos:
            // using (var db = new TuContextoDB())
            // {
            //     var cliente = db.Clientes.Find(_clienteId);
            //     cliente.Cliente_Nombre    = txtNombre.Text;
            //     cliente.Cliente_Apellido  = txtApellido.Text;
            //     cliente.Cliente_DPI       = txtDPI.Text;
            //     cliente.Cliente_Telefono  = txtTelefono.Text;
            //     cliente.Cliente_Correo    = txtCorreo.Text;
            //     cliente.Cliente_Direccion = txtDireccion.Text;
            //     cliente.Cliente_Activo    = toggleActivo.IsChecked == true;
            //     db.SaveChanges();
            // }

            MessageBox.Show("Cliente actualizado correctamente.", "Éxito",
                MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
