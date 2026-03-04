using Microsoft.Data.SqlClient;
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
   
    public partial class MainWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
        
        // Guarda el ID del producto seleccionado para Actualizar
        private int _productoIdSeleccionado = -1;

        

        public MainWindow()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════
        // 1. AGREGAR → INSERT
        // ═══════════════════════════════════════════
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out decimal precio)) return;

            try
            {
                _conexion.Abrir();

                string query = @"
                    INSERT INTO Producto
                        (Producto_Nombre, Producto_Categoria, Producto_Marca,
                         Producto_Modelo, Producto_Precio,
                         Producto_Cantidad_Actual, Producto_Stock_Minimo)
                    VALUES
                        (@Nombre, @Categoria, @Marca, @Modelo, @Precio, 0, 10)";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Nombre",
                        txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Categoria",
                        (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Marca",
                        txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo",
                        string.IsNullOrWhiteSpace(txtModelo.Text)
                            ? (object)DBNull.Value
                            : txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("✅ Producto agregado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LimpiarFormulario();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // 2. ACTUALIZAR → UPDATE
        // ═══════════════════════════════════════════
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (_productoIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un producto del inventario para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidarCampos(out decimal precio)) return;

            try
            {
                _conexion.Abrir();

                string query = @"
                    UPDATE Producto SET
                        Producto_Nombre    = @Nombre,
                        Producto_Categoria = @Categoria,
                        Producto_Marca     = @Marca,
                        Producto_Modelo    = @Modelo,
                        Producto_Precio    = @Precio
                    WHERE Producto_ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Nombre",
                        txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Categoria",
                        (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Marca",
                        txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo",
                        string.IsNullOrWhiteSpace(txtModelo.Text)
                            ? (object)DBNull.Value
                            : txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.Parameters.AddWithValue("@ID", _productoIdSeleccionado);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("✅ Producto actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                LimpiarFormulario();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // 3. CANCELAR
        // ═══════════════════════════════════════════
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarFormulario();
            this.Close();
        }

        // ═══════════════════════════════════════════
        // 4. TOGGLE ESTADO
        // ═══════════════════════════════════════════
        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El producto está activo";
            txtEstadoLabel.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline;
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El producto está inactivo";
            txtEstadoLabel.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Foreground =
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));
            iconEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        // ═══════════════════════════════════════════
        // 5. CARGAR DATOS PARA EDITAR (llamado desde el menú)
        // ═══════════════════════════════════════════
        public void CargarProductoParaEditar(Repuesto producto)
        {
            _productoIdSeleccionado = producto.Producto_ID;

            txtNombre.Text = producto.Producto_Nombre;
            txtMarca.Text = producto.Producto_Marca;      // ← Marca incluida
            txtModelo.Text = producto.Producto_Modelo == "—" ? "" : producto.Producto_Modelo;
            txtPrecio.Text = producto.Producto_Precio.ToString("N2");

            foreach (ComboBoxItem item in cmbCategoria.Items)
            {
                if (item.Content.ToString() == producto.Producto_Categoria)
                {
                    cmbCategoria.SelectedItem = item;
                    break;
                }
            }
        }

        // ═══════════════════════════════════════════
        // 6. VALIDACIÓN CENTRALIZADA
        // ═══════════════════════════════════════════
        private bool ValidarCampos(out decimal precio)
        {
            precio = 0;

            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                cmbCategoria.SelectedItem == null ||
                string.IsNullOrWhiteSpace(txtMarca.Text) ||
                string.IsNullOrWhiteSpace(txtPrecio.Text))
            {
                MessageBox.Show("Por favor completa: Nombre, Categoría, Marca y Precio.",
                    "Campos requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(txtPrecio.Text, out precio) || precio < 0)
            {
                MessageBox.Show("El precio debe ser un número válido mayor o igual a 0.",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ═══════════════════════════════════════════
        // 7. LIMPIAR FORMULARIO
        // ═══════════════════════════════════════════
        private void LimpiarFormulario()
        {
            txtNombre.Clear();
            txtMarca.Clear();
            txtModelo.Clear();
            txtPrecio.Clear();
            cmbCategoria.SelectedIndex = -1;
            toggleActivo.IsChecked = true;
            _productoIdSeleccionado = -1;
        }
    }
}