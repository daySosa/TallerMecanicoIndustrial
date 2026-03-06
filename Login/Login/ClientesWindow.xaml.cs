using Login.Clases;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
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

        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private bool ValidarDNIHondureño(string dni)
        {
            return Regex.IsMatch(dni, @"^\d{13}$");
        }

        public void CargarClienteParaEditar(Cliente c)
        {
            _dniEditando = c.Cliente_DPI;
            txtDPI.Text = c.Cliente_DPI;
            txtDPI.IsReadOnly = false;
            txtNombre.Text = c.Cliente_Nombre;
            txtApellido.Text = c.Cliente_Apellido;
            txtTelefono.Text = c.Cliente_Telefono;
            txtCorreo.Text = c.Cliente_Correo;
            txtDireccion.Text = c.Cliente_Direccion;
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

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

            try
            {
                var db = new clsConexion();
                db.Abrir();


                string sqlCheck = "SELECT COUNT(1) FROM Cliente WHERE Cliente_DNI = @DNI";
                using (SqlCommand chk = new SqlCommand(sqlCheck, db.SqlC))
                {
                    chk.Parameters.AddWithValue("@DNI", txtDPI.Text.Trim());
                    int existe = (int)chk.ExecuteScalar();
                    if (existe > 0)
                    {
                        MessageBox.Show("Ya existe un cliente con ese DNI.",
                            "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        db.Cerrar();
                        return;
                    }
                }

                string sql = @"
            INSERT INTO Cliente
                (Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                 Cliente_TelefonoPrincipal, Cliente_Email, Cliente_Direccion)
            VALUES
                (@DNI, @Nombres, @Apellidos, @Telefono, @Email, @Direccion)";

                using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
                {
                    cmd.Parameters.AddWithValue("@DNI", txtDPI.Text.Trim());
                    cmd.Parameters.AddWithValue("@Nombres", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Apellidos", txtApellido.Text.Trim());
                    cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtCorreo.Text)
                        ? (object)DBNull.Value : txtCorreo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(txtDireccion.Text)
                        ? (object)DBNull.Value : txtDireccion.Text.Trim());
                    cmd.ExecuteNonQuery();
                }

                db.Cerrar();

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

                MessageBox.Show("✅ Cliente agregado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar cliente:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var db = new clsConexion();
                db.Abrir();

                string sql = @"
                    UPDATE Cliente SET
                        Cliente_Nombres           = @Nombres,
                        Cliente_Apellidos         = @Apellidos,
                        Cliente_TelefonoPrincipal = @Telefono,
                        Cliente_Email             = @Email,
                        Cliente_Direccion         = @Direccion
                    WHERE Cliente_DNI = @DNI";

                using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Nombres", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Apellidos", txtApellido.Text.Trim());
                    cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text.Trim());
                    cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(txtCorreo.Text)
                        ? (object)DBNull.Value : txtCorreo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(txtDireccion.Text)
                        ? (object)DBNull.Value : txtDireccion.Text.Trim());
                    cmd.Parameters.AddWithValue("@DNI", _dniEditando);
                    cmd.ExecuteNonQuery();
                }

                db.Cerrar();

                MessageBox.Show("✅ Cliente actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidarCampos()
        {
            if (!ValidarDNIHondureño(txtDPI.Text.Trim()))
            {
                MessageBox.Show("El DNI debe tener exactamente 13 dígitos numéricos.\nEjemplo: 0801199012345",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDPI.Focus();
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