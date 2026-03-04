using Microsoft.Data.SqlClient;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace InterfazInventario
{
    public partial class MainWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
        private int _productoIdSeleccionado = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        // ═══════════════════════════════════════════
        // BOTONES + Y - DE CANTIDAD
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // 1. AGREGAR → INSERT
        //    Guarda el producto con la cantidad inicial
        // ═══════════════════════════════════════════
        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCampos(out decimal precio, out int cantidad)) return;

            try
            {
                _conexion.Abrir();

                string query = @"
                    INSERT INTO Producto
                        (Producto_Nombre, Producto_Categoria, Producto_Marca,
                         Producto_Modelo, Producto_Precio,
                         Producto_Cantidad_Actual, Producto_Stock_Minimo)
                    VALUES
                        (@Nombre, @Categoria, @Marca, @Modelo, @Precio,
                         @Cantidad, 10)";

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
                    cmd.Parameters.AddWithValue("@Cantidad", cantidad);

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
        //    Stock nuevo = stock actual + cantidad ingresada
        //    Si cantidad = 0, el stock no cambia
        // ═══════════════════════════════════════════
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (_productoIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un producto del inventario para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidarCampos(out decimal precio, out int cantidad)) return;

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
                    cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                    cmd.Parameters.AddWithValue("@ID", _productoIdSeleccionado);

                    cmd.ExecuteNonQuery();
                }

                // Mensaje personalizado según si repuso stock o no
                string msg = cantidad > 0
                    ? $"✅ Producto actualizado.\n+{cantidad} unidades sumadas al stock."
                    : "✅ Producto actualizado sin cambios en el stock.";

                MessageBox.Show(msg, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

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
        // 5. CARGAR DATOS PARA EDITAR
        //    txtCantidad siempre arranca en 0
        //    El usuario escribe cuánto quiere SUMAR hoy
        // ═══════════════════════════════════════════
        public void CargarProductoParaEditar(Repuesto producto)
        {
            _productoIdSeleccionado = producto.Producto_ID;

            txtNombre.Text = producto.Producto_Nombre;
            txtMarca.Text = producto.Producto_Marca;
            txtModelo.Text = producto.Producto_Modelo == "—" ? "" : producto.Producto_Modelo;
            txtPrecio.Text = producto.Producto_Precio.ToString("N2");
            txtCantidad.Text = "0";  // siempre 0: solo se suma lo que entre ahora

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
        private bool ValidarCampos(out decimal precio, out int cantidad)
        {
            precio = 0;
            cantidad = 0;

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

            if (!int.TryParse(txtCantidad.Text, out cantidad) || cantidad < 0)
            {
                MessageBox.Show("La cantidad debe ser un número entero mayor o igual a 0.",
                    "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            txtCantidad.Text = "0";
            cmbCategoria.SelectedIndex = -1;
            _productoIdSeleccionado = -1;
        }
    }
}