using Login.Clases;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace Órdenes_de_Trabajo
{
    // ═══════════════════════════════════════════════════════════════
    // MODELO — producto del inventario para el ComboBox
    // ═══════════════════════════════════════════════════════════════
    public class ProductoInventario
    {
        public int Producto_ID { get; set; }
        public string Producto_Nombre { get; set; }
        public string Producto_Categoria { get; set; }
        public int Producto_Cantidad_Actual { get; set; }
        public decimal Producto_Precio { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    // CODE-BEHIND — AgregarRepuesto
    // ═══════════════════════════════════════════════════════════════
    public partial class AgregarRepuesto : Window
    {
        private clsConexion _conexion = new clsConexion();

        // ✔ OrdenWindow lee esta propiedad después del ShowDialog().
        //   Si el usuario canceló, queda en null.
        public RepuestoOrden RepuestoResultado { get; private set; } = null;

        // Producto actualmente seleccionado en el ComboBox
        private ProductoInventario _productoSeleccionado = null;

        public AgregarRepuesto()
        {
            InitializeComponent();
            CargarProductos();
        }

        // ═══════════════════════════════════════════
        // 1. CARGAR PRODUCTOS DESDE BD
        //    Usa sp_ObtenerProductosInventario que
        //    solo devuelve productos con stock > 0.
        // ═══════════════════════════════════════════
        private void CargarProductos()
        {
            try
            {
                _conexion.Abrir();

                var lista = new List<ProductoInventario>();

                using (SqlCommand cmd = new SqlCommand("sp_ObtenerProductosInventario", _conexion.SqlC))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Busqueda", DBNull.Value);

                    using (SqlDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            lista.Add(new ProductoInventario
                            {
                                Producto_ID = rd.GetInt32(rd.GetOrdinal("Producto_ID")),
                                Producto_Nombre = rd["Producto_Nombre"].ToString(),
                                Producto_Categoria = rd["Producto_Categoria"].ToString(),
                                Producto_Cantidad_Actual = rd.GetInt32(rd.GetOrdinal("Producto_Cantidad_Actual")),
                                Producto_Precio = rd.GetDecimal(rd.GetOrdinal("Producto_Precio"))
                            });
                        }
                    }
                }

                cmbProducto.ItemsSource = lista;
                cmbProducto.DisplayMemberPath = "Producto_Nombre";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // 2. SELECCIÓN EN COMBOBOX
        //    Rellena categoría, precio y stock
        //    automáticamente al elegir un producto.
        // ═══════════════════════════════════════════
        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _productoSeleccionado = cmbProducto.SelectedItem as ProductoInventario;
            if (_productoSeleccionado == null) return;

            txtCategoria.Text = _productoSeleccionado.Producto_Categoria;
            txtPrecioUnitario.Text = $"S/ {_productoSeleccionado.Producto_Precio:N2}";
            txtStock.Text = _productoSeleccionado.Producto_Cantidad_Actual.ToString();

            // Alerta visual si el stock es bajo
            borderStock.BorderBrush = _productoSeleccionado.Producto_Cantidad_Actual <= 5
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3f5c"));

            CalcularSubtotal();
        }

        // ═══════════════════════════════════════════
        // 3. CAMBIO DE CANTIDAD → recalcula subtotal
        // ═══════════════════════════════════════════
        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularSubtotal();
        }

        // ═══════════════════════════════════════════
        // 4. CALCULAR SUBTOTAL (interno)
        // ═══════════════════════════════════════════
        private void CalcularSubtotal()
        {
            if (_productoSeleccionado == null) return;

            if (!int.TryParse(txtCantidad.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                txtSubtotal.Text = "S/ 0.00";
                return;
            }

            txtSubtotal.Text = $"S/ {_productoSeleccionado.Producto_Precio * cantidad:N2}";
        }

        // ═══════════════════════════════════════════
        // 5. BOTÓN AGREGAR REPUESTO
        //    Valida y guarda el resultado en la
        //    propiedad RepuestoResultado para que
        //    OrdenWindow lo lea tras el ShowDialog().
        // ═══════════════════════════════════════════
        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Selecciona un producto del inventario.",
                    "Producto requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidad.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("La cantidad debe ser un número entero mayor a 0.",
                    "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cantidad > _productoSeleccionado.Producto_Cantidad_Actual)
            {
                MessageBox.Show(
                    $"Stock insuficiente. Solo hay {_productoSeleccionado.Producto_Cantidad_Actual} unidades disponibles.",
                    "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Guarda el resultado — OrdenWindow lo leerá después del ShowDialog()
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

        // ═══════════════════════════════════════════
        // 6. BOTÓN CANCELAR
        // ═══════════════════════════════════════════
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            RepuestoResultado = null;
            this.Close();
        }
    }
}