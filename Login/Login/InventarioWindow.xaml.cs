using Login.Clases;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace InterfazInventario
{
    /// <summary>
    /// Ventana encargada de gestionar el inventario de productos.
    /// Permite agregar nuevos productos, actualizar información existente
    /// y controlar el stock disponible.
    /// </summary>
    public partial class InventarioWindow : Window
    {
        /// <summary>
        /// Instancia utilizada para realizar operaciones en la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Identificador del producto seleccionado para edición.
        /// </summary>
        private int _productoIdSeleccionado = -1;

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="InventarioWindow"/>.
        /// </summary>
        public InventarioWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        /// <summary>
        /// Incrementa la cantidad ingresada en el campo de stock.
        /// </summary>
        private void BtnSumar_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int val))
                txtCantidad.Text = (val + 1).ToString();
            else
                txtCantidad.Text = "1";
        }

        /// <summary>
        /// Disminuye la cantidad ingresada en el campo de stock.
        /// </summary>
        private void BtnRestar_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtCantidad.Text, out int val) && val > 0)
                txtCantidad.Text = (val - 1).ToString();
            else
                txtCantidad.Text = "0";
        }

        /// <summary>
        /// Valida los datos ingresados y registra un nuevo producto en el inventario.
        /// </summary>
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
                _db.AgregarProducto(
                    txtNombre.Text.Trim(),
                    (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    txtMarca.Text.Trim(),
                    txtModelo.Text,
                    precio,
                    cantidad
                );

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
        }

        /// <summary>
        /// Valida los datos ingresados y actualiza la información de un producto existente,
        /// incluyendo la modificación del stock.
        /// </summary>
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
                _db.ActualizarProducto(
                    _productoIdSeleccionado,
                    txtNombre.Text.Trim(),
                    (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    txtMarca.Text.Trim(),
                    txtModelo.Text,
                    precio,
                    cantidadAgregar
                );

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
        }

        /// <summary>
        /// Cancela la operación actual y cierra la ventana.
        /// </summary>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Carga la información de un producto seleccionado para su edición.
        /// </summary>
        /// <param name="producto">Producto seleccionado del inventario.</param>
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

        /// <summary>
        /// Obtiene y valida los valores ingresados por el usuario,
        /// incluyendo nombre, categoría, marca, precio y cantidad.
        /// </summary>
        /// <param name="precio">Precio del producto.</param>
        /// <param name="cantidad">Cantidad a agregar o registrar.</param>
        /// <returns>True si los valores son válidos; de lo contrario, false.</returns>
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

        /// <summary>
        /// Evento reservado para futuras validaciones dinámicas del precio.
        /// </summary>
        private void txtPrecio_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}