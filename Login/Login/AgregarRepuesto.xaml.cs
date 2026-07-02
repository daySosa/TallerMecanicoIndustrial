using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public partial class AgregarRepuesto : Window
    {
        private readonly RepositorioSql _db = new RepositorioSql();
        private clsProductoInventario _productoSeleccionado;

        public RepuestoOrden RepuestoResultado { get; private set; }

        public AgregarRepuesto()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private void CargarProductos()
        {
            try
            {
                cmbProducto.ItemsSource = _db.ObtenerProductosInventario();
                cmbProducto.DisplayMemberPath = "Producto_Nombre";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _productoSeleccionado = cmbProducto.SelectedItem as clsProductoInventario;
            if (_productoSeleccionado == null) return;

            txtCategoria.Text = _productoSeleccionado.Producto_Categoria;
            txtPrecioUnitario.Text = $"L {_productoSeleccionado.Producto_Precio:N2}";
            txtStock.Text = _productoSeleccionado.Producto_Cantidad_Actual.ToString();

            bool stockBajo = _productoSeleccionado.Producto_Cantidad_Actual <= 5;
            borderStock.BorderBrush = stockBajo
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3f5c"));

            CalcularSubtotal();
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
            => CalcularSubtotal();

        private void CalcularSubtotal()
        {
            if (_productoSeleccionado == null) return;

            bool valido = int.TryParse(txtCantidad.Text.Trim(), out int cantidad) && cantidad > 0;
            txtSubtotal.Text = valido
                ? $"L {_productoSeleccionado.Producto_Precio * cantidad:N2}"
                : "L 0.00";
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidacionesGenerales.ValidarComboSeleccionado(_productoSeleccionado, "producto del inventario")) return;
            if (!ValidacionesGenerales.ValidarEnteroPositivo(txtCantidad.Text, out int cantidad, "Cantidad inválida")) return;
            if (!ValidacionesGenerales.ValidarStock(cantidad, _productoSeleccionado.Producto_Cantidad_Actual)) return;

            RepuestoResultado = new RepuestoOrden
            {
                ProductoID = _productoSeleccionado.Producto_ID,
                Nombre = _productoSeleccionado.Producto_Nombre,
                Cantidad = cantidad,
                Precio = _productoSeleccionado.Producto_Precio,
                Incluido = true
            };

            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            RepuestoResultado = null;
            this.Close();
        }
    }
}