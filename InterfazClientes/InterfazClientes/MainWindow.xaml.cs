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

    public partial class MainWindow : Window
    {
        // DNI del cliente cargado para editar. -1 = modo nuevo.
        private int _dniEditando = -1;

        public Cliente ClienteResultado { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════
        // CARGAR DATOS PARA EDITAR
        // ═══════════════════════════════════════════
        public void CargarClienteParaEditar(Cliente c)
        {
            _dniEditando = int.TryParse(c.Cliente_DPI, out int dni) ? dni : -1;
            txtDPI.Text = c.Cliente_DPI;
            txtDPI.IsReadOnly = true;   // El DNI es PK, no se edita
            txtNombre.Text = c.Cliente_Nombre;
            txtApellido.Text = c.Cliente_Apellido;
            txtTelefono.Text = c.Cliente_Telefono;
            txtCorreo.Text = c.Cliente_Correo;
            txtDireccion.Text = c.Cliente_Direccion;
            toggleActivo.IsChecked = c.Cliente_Activo;
        }

        // ═══════════════════════════════════════════
        // TOGGLE ESTADO
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // CANCELAR — solo cierra
        // ═══════════════════════════════════════════
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // ═══════════════════════════════════════════
        // AGREGAR — guarda y cierra con DialogResult=true
        // ═══════════════════════════════════════════
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

            // DialogResult = true hace que ShowDialog() devuelva true
            this.DialogResult = true;
            this.Close();
        }

        // ═══════════════════════════════════════════
        // ACTUALIZAR — UPDATE en BD
        // ═══════════════════════════════════════════
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos()) return;

            if (_dniEditando == -1)
            {
                MessageBox.Show("No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = new clsConexión();
                db.Abrir();

                string sql = @"
                    UPDATE Cliente SET
                        Cliente_Nombres          = @Nombres,
                        Cliente_Apellidos        = @Apellidos,
                        Cliente_TelefonoPrincipal= @Telefono,
                        Cliente_Email            = @Email,
                        Cliente_Direccion        = @Direccion
                    WHERE Cliente_DNI = @DNI";

                SqlCommand cmd = new SqlCommand(sql, db.SqlC);
                cmd.Parameters.AddWithValue("@Nombres", txtNombre.Text.Trim());
                cmd.Parameters.AddWithValue("@Apellidos", txtApellido.Text.Trim());
                cmd.Parameters.AddWithValue("@Telefono", txtTelefono.Text.Trim());
                cmd.Parameters.AddWithValue("@Email", txtCorreo.Text.Trim());
                cmd.Parameters.AddWithValue("@Direccion", txtDireccion.Text.Trim());
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

        // ═══════════════════════════════════════════
        // VALIDAR CAMPOS
        // ═══════════════════════════════════════════
        private bool ValidarCampos()
        {
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