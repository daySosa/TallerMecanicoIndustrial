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
    public class ProductoInventario
    {
        public int Producto_ID { get; set; }
        public string Producto_Nombre { get; set; }
        public string Producto_Categoria { get; set; }
        public int Producto_Cantidad_Actual { get; set; }
        public decimal Producto_Precio { get; set; }
    }

    public partial class AgregarRepuesto : Window
    {
        private clsConexion _conexion = new clsConexion();
        public RepuestoOrden RepuestoResultado { get; private set; } = null;
        private ProductoInventario _productoSeleccionado = null;

        public AgregarRepuesto()
        {
            InitializeComponent();
            CargarProductos();
        }

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

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _productoSeleccionado = cmbProducto.SelectedItem as ProductoInventario;
            if (_productoSeleccionado == null) return;

            txtCategoria.Text = _productoSeleccionado.Producto_Categoria;
            txtPrecioUnitario.Text = $"L {_productoSeleccionado.Producto_Precio:N2}";
            txtStock.Text = _productoSeleccionado.Producto_Cantidad_Actual.ToString();

            borderStock.BorderBrush = _productoSeleccionado.Producto_Cantidad_Actual <= 5
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3a3f5c"));

            CalcularSubtotal();
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularSubtotal();
        }

        private void CalcularSubtotal()
        {
            if (_productoSeleccionado == null) return;

            if (!int.TryParse(txtCantidad.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                txtSubtotal.Text = "L 0.00";
                return;
            }

            txtSubtotal.Text = $"L {_productoSeleccionado.Producto_Precio * cantidad:N2}";
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarComboSeleccionado(_productoSeleccionado, "producto del inventario")) return;
            if (!clsValidaciones.ValidarEnteroPositivo(txtCantidad.Text, out int cantidad, "Cantidad inválida")) return;
            if (!clsValidaciones.ValidarStock(cantidad, _productoSeleccionado.Producto_Cantidad_Actual)) return;

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