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
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        private void txtTelefono_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (txtTelefono == null) return;

            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            int caretAntes = txtTelefono.CaretIndex;
            string soloNumeros = Regex.Replace(txtTelefono.Text, @"\D", "");

            if (soloNumeros.Length > 8)
                soloNumeros = soloNumeros.Substring(0, 8);

            string formateado = soloNumeros.Length == 8
                ? soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4)
                : soloNumeros;

            txtTelefono.Text = formateado;
            txtTelefono.CaretIndex = formateado.Length; 

            txtTelefono.TextChanged += txtTelefono_TextChanged;
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

            btnAgregar.IsEnabled = false;
            btnAgregar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
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
            btnAgregar.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(txtDPI.Text) || !ValidarDNIHondureño(txtDPI.Text.Trim()))
            {
                MessageBox.Show("⚠ Ingrese un DNI válido de 13 dígitos.", "DNI inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                btnAgregar.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("⚠ Ingrese el nombre del cliente.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                btnAgregar.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show("⚠ Ingrese el apellido del cliente.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                btnAgregar.IsEnabled = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTelefono.Text) || txtTelefono.Text.Length < 9)
            {
                MessageBox.Show("⚠ Ingrese un teléfono válido (ej: 9999-9999).", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                btnAgregar.IsEnabled = true;
                return;
            }

            if (!txtNombre.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show("⚠ El nombre solo puede contener letras y espacios.",
                    "Nombre inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtApellido.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show("⚠ El apellido solo puede contener letras y espacios.",
                    "Apellido inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtCorreo.Text) && 
                    !System.Text.RegularExpressions.Regex.IsMatch(txtCorreo.Text.Trim(), @"^[^@]+@[^@]+\.[^@]+$"))
            {
                MessageBox.Show("⚠ El correo no tiene un formato válido.\nEjemplo: nombre@dominio.com",
                    "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = new clsConexion();
                db.Abrir();

                string sql = @"
                    IF NOT EXISTS (SELECT 1 FROM Cliente WHERE Cliente_DNI = @DNI)
                    BEGIN
                        INSERT INTO Cliente
                            (Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                             Cliente_TelefonoPrincipal, Cliente_Email,
                             Cliente_Direccion, Cliente_Activo)
                        VALUES
                            (@DNI, @Nombres, @Apellidos, @Telefono, @Email,
                             @Direccion, 1)
                        SELECT 1
                    END
                    ELSE
                        SELECT 0";

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

                    int resultado = Convert.ToInt32(cmd.ExecuteScalar());

                    if (resultado == 0)
                    {
                        MessageBox.Show("Ya existe un cliente con ese DNI.",
                            "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        db.Cerrar();
                        btnAgregar.IsEnabled = true;
                        return;
                    }
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
                    Cliente_Activo = true
                };

                MessageBox.Show("✅ Cliente guardado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                btnAgregar.IsEnabled = true;
                MessageBox.Show("⚠ Error al agregar cliente:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_dniEditando))
            {
                MessageBox.Show("⚠ No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("⚠ Ingrese el nombre del cliente.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MessageBox.Show("⚠ Ingrese el apellido del cliente.", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtTelefono.Text) || txtTelefono.Text.Length < 9)
            {
                MessageBox.Show("⚠ Ingrese un teléfono válido (ej: 9999-9999).", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtNombre.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show("⚠ El nombre solo puede contener letras y espacios.",
                    "Nombre inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!txtApellido.Text.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show("⚠ El apellido solo puede contener letras y espacios.",
                    "Apellido inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(txtCorreo.Text) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(txtCorreo.Text.Trim(), @"^[^@]+@[^@]+\.[^@]+$"))
            {
                MessageBox.Show("⚠ El correo no tiene un formato válido.\nEjemplo: nombre@dominio.com",
                    "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        Cliente_Direccion         = @Direccion,
                        Cliente_Activo            = @Activo
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
                    cmd.Parameters.AddWithValue("@Activo", toggleActivo.IsChecked == true ? 1 : 0);
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
    }
}