using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public partial class AgregarRepuesto : Window
    {
        public RepuestoSeleccionado RepuestoResultado { get; private set; }

        public AgregarRepuesto()
        {
            InitializeComponent();
            CargarProductos();
        }

        private void CargarProductos()
        {
            try
            {
                clsConexion con = new clsConexion();
                con.Abrir();

                SqlDataAdapter da = new SqlDataAdapter("EXEC sp_ObtenerProductosInventario", con.SqlC);
                DataTable dt = new DataTable();
                da.Fill(dt);
                con.Cerrar();

                cmbProducto.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProducto.SelectedItem is DataRowView row)
            {
                txtCategoria.Text = row["Producto_Categoria"]?.ToString() ?? "-";
                txtPrecioUnitario.Text = $"S/ {Convert.ToDecimal(row["Producto_Precio"]):F2}";

                int stock = Convert.ToInt32(row["Producto_Cantidad_Actual"]);
                txtStock.Text = stock.ToString();

                if (stock == 0)
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(239, 83, 80));
                    borderStock.BorderBrush = new SolidColorBrush(Color.FromRgb(239, 83, 80));
                }
                else if (stock <= 5)
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(255, 167, 38));
                    borderStock.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 167, 38));
                }
                else
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(102, 187, 106));
                    borderStock.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 63, 92));
                }

                CalcularSubtotal();
            }
            else
            {
                LimpiarCampos();
            }
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalcularSubtotal();
        }

        private void CalcularSubtotal()
        {
            if (txtSubtotal == null) return;

            if (cmbProducto.SelectedItem is DataRowView row &&
                int.TryParse(txtCantidad.Text, out int cantidad) && cantidad > 0)
            {
                decimal precio = Convert.ToDecimal(row["Producto_Precio"]);
                txtSubtotal.Text = $"S/ {precio * cantidad:F2}";
            }
            else
            {
                txtSubtotal.Text = "S/ 0.00";
            }
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProducto.SelectedItem is not DataRowView row)
            {
                MessageBox.Show("Selecciona un producto.", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingresa una cantidad válida (mayor a 0).", "Validación",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int stock = Convert.ToInt32(row["Producto_Cantidad_Actual"]);
            if (cantidad > stock)
            {
                MessageBox.Show($"Stock insuficiente. Disponible: {stock} unidades.", "Stock",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            RepuestoResultado = new RepuestoSeleccionado
            {
                ProductoID = Convert.ToInt32(row["Producto_ID"]),
                Nombre = row["Producto_Nombre"].ToString(),
                Categoria = row["Producto_Categoria"].ToString(),
                PrecioUnitario = Convert.ToDecimal(row["Producto_Precio"]),
                Cantidad = cantidad,
                Subtotal = Convert.ToDecimal(row["Producto_Precio"]) * cantidad
            };

            DialogResult = true;
            Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void LimpiarCampos()
        {
            txtCategoria.Text = "-";
            txtPrecioUnitario.Text = "S/ 0.00";
            txtStock.Text = "0";
            txtSubtotal.Text = "S/ 0.00";
            txtStock.Foreground = new SolidColorBrush(Color.FromRgb(108, 114, 147));
            borderStock.BorderBrush = new SolidColorBrush(Color.FromRgb(58, 63, 92));
        }
    }

    public class RepuestoSeleccionado
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
    }
}