using Login.Clases;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace InterfazInventario
{
    public partial class InventarioWindow : Window
    {
        private readonly clsConexion _conexion = new clsConexion();
        private int _productoIdSeleccionado = -1;

        public InventarioWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        private void BtnSumar_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int val))
                txtCantidad.Text = (val + 1).ToString();
            else
                txtCantidad.Text = "1";
        }

        private void BtnRestar_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int val) && val > 0)
                txtCantidad.Text = (val - 1).ToString();
            else
                txtCantidad.Text = "0";
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;
            btnActualizar.IsEnabled = false;

            if (!ObtenerValores(out decimal precio, out int cantidad))
            {
                btnAgregar.IsEnabled = true;
                btnActualizar.IsEnabled = false;
                return;
            }

            try
            {
                _conexion.Abrir();
                string query = @"
                    INSERT INTO Producto
                        (Producto_Nombre, Producto_Categoria, Producto_Marca,
                         Producto_Modelo, Producto_Precio,
                         Producto_Cantidad_Actual, Producto_Stock_Minimo)
                    VALUES
                        (@Nombre, @Categoria, @Marca, @Modelo,
                         @Precio, @Cantidad, 10)";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Categoria", (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Marca", txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo",
                        string.IsNullOrWhiteSpace(txtModelo.Text)
                            ? (object)DBNull.Value
                            : txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Producto agregado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnAgregar.IsEnabled = true;
            }
            finally { _conexion.Cerrar(); }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;
            btnActualizar.IsEnabled = false;

            if (_productoIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un producto del inventario para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnActualizar.IsEnabled = true;
                return;
            }

            if (!ObtenerValores(out decimal precio, out int cantidadAgregar))
            {
                btnActualizar.IsEnabled = true;
                return;
            }

            try
            {
                _conexion.Abrir();
                string query = @"
                    UPDATE Producto SET
                        Producto_Nombre          = @Nombre,
                        Producto_Categoria       = @Categoria,
                        Producto_Marca           = @Marca,
                        Producto_Modelo          = @Modelo,
                        Producto_Precio          = @Precio,
                        Producto_Cantidad_Actual = Producto_Cantidad_Actual + @Cantidad
                    WHERE Producto_ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                    cmd.Parameters.AddWithValue("@Categoria", (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Marca", txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo",
                        string.IsNullOrWhiteSpace(txtModelo.Text)
                            ? (object)DBNull.Value
                            : txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Precio", precio);
                    cmd.Parameters.AddWithValue("@Cantidad", cantidadAgregar);
                    cmd.Parameters.AddWithValue("@ID", _productoIdSeleccionado);
                    cmd.ExecuteNonQuery();
                }

                string msg = cantidadAgregar > 0
                    ? $"Producto actualizado.\n+{cantidadAgregar} unidades agregadas al stock."
                    : "Producto actualizado sin cambios en el stock.";

                MessageBox.Show(msg, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnActualizar.IsEnabled = true;
            }
            finally { _conexion.Cerrar(); }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        public void CargarProductoParaEditar(Repuesto producto)
        {
            _productoIdSeleccionado = producto.Producto_ID;
            txtNombre.Text = producto.Producto_Nombre;
            txtMarca.Text = producto.Producto_Marca;
            txtModelo.Text = producto.Producto_Modelo == "—" ? "" : producto.Producto_Modelo;
            txtPrecio.Text = producto.Producto_Precio.ToString("N2");

            txtCantidad.Text = "0";
            txtStockActual.Text = producto.Producto_Cantidad_Actual.ToString();

            foreach (ComboBoxItem item in cmbCategoria.Items)
            {
                if (item.Content.ToString() == producto.Producto_Categoria)
                {
                    cmbCategoria.SelectedItem = item;
                    break;
                }
            }

            btnAgregar.IsEnabled = false;
            btnAgregar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;

            Title = "Inventario - Editar Producto";
        }

        private bool ObtenerValores(out decimal precio, out int cantidad)
        {
            precio = 0;
            cantidad = 0;

            if (string.IsNullOrWhiteSpace(txtNombre.Text))
            {
                MessageBox.Show("⚠ El nombre del producto es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNombre.Focus();
                return false;
            }

            if (cmbCategoria.SelectedItem == null)
            {
                MessageBox.Show("⚠ Debes seleccionar una categoría.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbCategoria.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtMarca.Text))
            {
                MessageBox.Show("⚠ La marca del producto es obligatoria.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMarca.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtPrecio.Text))
            {
                string precioTexto = txtPrecio.Text.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();

                if (!decimal.TryParse(precioTexto, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out precio) || precio < 0)
                {
                    MessageBox.Show("⚠ El precio debe ser un número válido mayor o igual a 0.\nEjemplo: 150.00",
                        "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPrecio.Focus();
                    return false;
                }
            }

            if (!int.TryParse(txtCantidad.Text, out cantidad) || cantidad < 0)
            {
                MessageBox.Show("⚠ La cantidad debe ser un número entero mayor o igual a 0.",
                    "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void txtPrecio_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}