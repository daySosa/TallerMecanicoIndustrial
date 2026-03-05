using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InterfazClientes
{
    public class Cliente
    {
        public string Cliente_DPI { get; set; }
        public string Cliente_Nombre { get; set; }
        public string Cliente_Apellido { get; set; }
        public string Cliente_Telefono { get; set; }
        public string Cliente_Correo { get; set; }
        public string Cliente_Direccion { get; set; }
        public bool Cliente_Activo { get; set; } = true;
    }

    public partial class ClientesWindow : Window
    {
        private string _dniEditando = string.Empty;
        public Cliente ClienteResultado { get; private set; }

        public ClientesWindow()
        {
            InitializeComponent();
        }

        public void CargarClienteParaEditar(Cliente c)
        {
            _dniEditando = c.Cliente_DPI;

            txtDPI.Text = c.Cliente_DPI;
            txtDPI.IsReadOnly = false;

            txtNombre.Text = c.Cliente_Nombre;
            txtNombre.IsReadOnly = false;

            txtApellido.Text = c.Cliente_Apellido;
            txtApellido.IsReadOnly = false;

            txtTelefono.Text = c.Cliente_Telefono;
            txtTelefono.IsReadOnly = false;

            txtCorreo.Text = c.Cliente_Correo;
            txtCorreo.IsReadOnly = false;

            txtDireccion.Text = c.Cliente_Direccion;
            txtDireccion.IsReadOnly = false;

            toggleActivo.IsChecked = c.Cliente_Activo;
        }

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El cliente está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

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

            this.DialogResult = true;
            this.Close();
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

            if (string.IsNullOrEmpty(_dniEditando))
            {
                MessageBox.Show("No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = new clsConexiónClie();
                db.Abrir();

                string sql = @"
                    UPDATE Cliente SET
                        Cliente_Nombres           = @Nombres,
                        Cliente_Apellidos         = @Apellidos,
                        Cliente_TelefonoPrincipal = @Telefono,
                        Cliente_Email             = @Email,
                        Cliente_Direccion         = @Direccion,
                        Cliente_Activo            = @Activo
                    WHERE Cliente_DNI = @DNI";

                SqlCommand cmd = new SqlCommand(sql, db.SqlC);
                cmd.Parameters.AddWithValue("@Nombres", txtNombre.Text.Trim());
                cmd.Parameters.AddWithValue("@Apellidos", txtApellido.Text.Trim());
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text.Trim());
                cmd.Parameters.AddWithValue("@Email", txtCorreo.Text.Trim());
                cmd.Parameters.AddWithValue("@Direccion", txtDireccion.Text.Trim());
                cmd.Parameters.AddWithValue("@Activo", toggleActivo.IsChecked == true ? 1 : 0);
                cmd.Parameters.AddWithValue("@DNI", _dniEditando);

                cmd.ExecuteNonQuery();
                db.Cerrar();

                MessageBox.Show("✅ Cliente actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(txtDPI.Text))
            {
                MessageBox.Show("El DNI es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtApellido.Text) ||
                string.IsNullOrWhiteSpace(txtTelefono.Text))
            {
                MessageBox.Show("Completa los campos obligatorios: Nombre, Apellido y Teléfono.",
                    "Campos requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}