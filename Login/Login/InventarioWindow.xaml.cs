#nullable enable
using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace InterfazInventario
{
    /// <summary>
    /// Ventana para agregar o editar un producto del inventario.
    /// Incluye transiciones de entrada/salida (fade + scale) y validación
    /// centralizada a través de <see cref="ValidacionesGenerales"/>.
    /// </summary>
    public partial class InventarioWindow : Window
    {
        #region Constantes

        private const int DuracionAnimacionMs = 220;
        private const double EscalaInicial = 0.92;
        private const double OpacidadCampoBloqueado = 0.55;
        private const int LongitudMaximaNombre = 100;
        private const int LongitudMaximaMarca = 50;

        #endregion

        #region Campos

        private readonly RepositorioSql _db = new();
        private int _productoIdSeleccionado = -1;

        #endregion

        public InventarioWindow()
        {
            InitializeComponent();
            ConfigurarModoAgregar();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>
        /// Ejecuta la animación de entrada (fade-in + escala) al cargar la ventana.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            var duracion = new Duration(TimeSpan.FromMilliseconds(DuracionAnimacionMs));

            var fadeIn = new DoubleAnimation(0, 1, duracion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(EscalaInicial, 1, duracion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(EscalaInicial, 1, duracion) { EasingFunction = easing };

            RootBorder.BeginAnimation(OpacityProperty, fadeIn);
            RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        /// <summary>
        /// Ejecuta la animación de salida (fade-out + escala) y cierra la ventana
        /// una vez finalizada, evitando el cierre abrupto.
        /// </summary>
        private void CerrarConAnimacion()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };
            var duracion = new Duration(TimeSpan.FromMilliseconds(DuracionAnimacionMs));

            var fadeOut = new DoubleAnimation(1, 0, duracion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(1, EscalaInicial, duracion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(1, EscalaInicial, duracion) { EasingFunction = easing };

            fadeOut.Completed += (_, _) => Close();

            RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            RootBorder.BeginAnimation(OpacityProperty, fadeOut);
        }

        private void BtnSumar_Click(object sender, RoutedEventArgs e)
        {
            int valorActual = ObtenerCantidadActual();
            txtCantidad.Text = (valorActual + 1).ToString();
        }

        private void BtnRestar_Click(object sender, RoutedEventArgs e)
        {
            int valorActual = ObtenerCantidadActual();
            txtCantidad.Text = Math.Max(0, valorActual - 1).ToString();
        }

        private int ObtenerCantidadActual() =>
            int.TryParse(txtCantidad.Text, out int valor) ? valor : 0;

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            btnAgregar.IsEnabled = false;

            if (!ValidarFormulario(cantidadDebeSerPositiva: true, out decimal precio, out int cantidad))
            {
                btnAgregar.IsEnabled = true;
                return;
            }

            try
            {
                _db.AgregarProducto(
                    txtNombre.Text.Trim(),
                    ObtenerCategoriaSeleccionada(),
                    txtMarca.Text.Trim(),
                    string.Empty,
                    precio,
                    cantidad);

                _db.RegistrarBitacora(SesionActual.Email, "Inventario", "Agregar",
                    $"Producto {txtNombre.Text.Trim()} - {cantidad} unidades");

                MessageBox.Show("Producto agregado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                CerrarConAnimacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al agregar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnAgregar.IsEnabled = true;
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            btnActualizar.IsEnabled = false;

            if (_productoIdSeleccionado == -1)
            {
                MessageBox.Show("Selecciona un producto del inventario para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnActualizar.IsEnabled = true;
                return;
            }

            if (!ValidarFormulario(cantidadDebeSerPositiva: false, out decimal precio, out int cantidadAgregar))
            {
                btnActualizar.IsEnabled = true;
                return;
            }

            try
            {
                _db.ActualizarProducto(
                    _productoIdSeleccionado,
                    txtNombre.Text.Trim(),
                    ObtenerCategoriaSeleccionada(),
                    txtMarca.Text.Trim(),
                    string.Empty,
                    precio,
                    cantidadAgregar);

                _db.RegistrarBitacora(SesionActual.Email, "Inventario", "Actualizar",
                    $"Producto {txtNombre.Text.Trim()} (ID {_productoIdSeleccionado})");

                string mensaje = cantidadAgregar > 0
                    ? $"Producto actualizado.\n+{cantidadAgregar} unidades agregadas al stock."
                    : "Producto actualizado sin cambios en el stock.";

                MessageBox.Show(mensaje, "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                CerrarConAnimacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar producto:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnActualizar.IsEnabled = true;
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => CerrarConAnimacion();

        /// <summary>
        /// Precarga el formulario con los datos de un producto existente
        /// y habilita el modo edición (usado desde el doble-click en el listado).
        /// </summary>
        public void CargarProductoParaEditar(ValidadorInventario producto)
        {
            _productoIdSeleccionado = producto.Producto_ID;

            txtNombre.Text = producto.Producto_Nombre;
            txtMarca.Text = producto.Producto_Marca;
            txtPrecio.Text = "L " + producto.Producto_Precio.ToString("N2");
            txtCantidad.Text = "0";
            txtStockActual.Text = producto.Producto_Cantidad_Actual.ToString();
            txtCantidadMinima.Text = producto.Producto_Cantidad_Minima.ToString();

            SeleccionarCategoria(producto.Producto_Categoria);
            ConfigurarModoEditar();
        }

        private void SeleccionarCategoria(string? categoria)
        {
            foreach (ComboBoxItem item in cmbCategoria.Items)
            {
                if (item.Content.ToString() == categoria)
                {
                    cmbCategoria.SelectedItem = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Configura la ventana para crear un producto nuevo.
        /// "Cantidad Mínima" queda editable porque aún no existe un umbral definido.
        /// </summary>
        private void ConfigurarModoAgregar()
        {
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;

            txtCantidadMinima.IsReadOnly = false;
            txtCantidadMinima.Opacity = 1;
        }

        /// <summary>
        /// Configura la ventana para actualizar stock de un producto existente.
        /// "Cantidad Mínima" queda bloqueada: el umbral ya fue definido al crear
        /// el producto y no debe modificarse accidentalmente al solo agregar stock.
        /// </summary>
        private void ConfigurarModoEditar()
        {
            btnAgregar.IsEnabled = false;
            btnAgregar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
            Title = "Inventario - Editar Producto";

            txtCantidadMinima.IsReadOnly = true;
            txtCantidadMinima.Opacity = OpacidadCampoBloqueado;
        }

        /// <summary>
        /// Valida todos los campos del formulario (nombre, categoría, marca,
        /// precio, cantidad mínima y cantidad a agregar) en un único
        /// punto, evitando duplicar la lógica entre Agregar y Actualizar.
        /// </summary>
        /// <param name="cantidadDebeSerPositiva">
        /// true para "Agregar" (debe ingresar al menos 1 unidad y define la cantidad mínima);
        /// false para "Actualizar" (0 es válido, cantidad mínima ya está bloqueada).
        /// </param>
        private bool ValidarFormulario(bool cantidadDebeSerPositiva, out decimal precio, out int cantidad)
        {
            precio = 0;
            cantidad = 0;

            if (!ValidacionesGenerales.ValidarFormularioVacio(txtNombre.Text, txtMarca.Text, txtPrecio.Text))
                return false;

            if (!ValidarCampoTexto(txtNombre, "nombre del producto", LongitudMaximaNombre)) return false;
            if (!ValidarCampoTexto(txtMarca, "marca", LongitudMaximaMarca)) return false;

            if (!ValidacionesGenerales.ValidarPrecio(txtPrecio.Text, out precio))
            {
                txtPrecio.Focus();
                return false;
            }

            if (cantidadDebeSerPositiva && !ValidarCantidadMinima())
                return false;

            if (!ValidarCantidad(cantidadDebeSerPositiva, out cantidad))
                return false;

            return true;
        }

        /// <summary>
        /// Aplica el conjunto estándar de validaciones de texto
        /// (requerido, no numérico, inicia con letra, sin repetición, longitud).
        /// </summary>
        private static bool ValidarCampoTexto(TextBox campo, string nombreCampo, int longitudMaxima)
        {
            bool esValido =
                ValidacionesGenerales.ValidarTextoRequerido(campo.Text, nombreCampo) &&
                ValidacionesGenerales.ValidarNoEsSoloNumeros(campo.Text, nombreCampo) &&
                ValidacionesGenerales.ValidarIniciaConLetra(campo.Text, nombreCampo) &&
                ValidacionesGenerales.ValidarSinRepeticionExcesiva(campo.Text, nombreCampo) &&
                ValidacionesGenerales.ValidarLongitudMaxima(campo.Text, longitudMaxima, nombreCampo);

            if (!esValido)
                campo.Focus();

            return esValido;
        }

        private bool ValidarCantidad(bool debeSerPositiva, out int cantidad)
        {
            if (!int.TryParse(txtCantidad.Text, out cantidad) || cantidad < 0)
            {
                MessageBox.Show("⚠ La cantidad debe ser un número entero mayor o igual a 0.",
                    "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (debeSerPositiva && cantidad <= 0)
            {
                MessageBox.Show("⚠ Debes ingresar una cantidad mayor a 0 para agregar el producto.",
                    "Cantidad inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool ValidarCantidadMinima()
        {
            if (!int.TryParse(txtCantidadMinima.Text, out int minima) || minima < 0)
            {
                MessageBox.Show("⚠ La cantidad mínima debe ser un número entero mayor o igual a 0.",
                    "Cantidad mínima inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCantidadMinima.Focus();
                return false;
            }

            return true;
        }

        private string? ObtenerCategoriaSeleccionada() =>
            (cmbCategoria.SelectedItem as ComboBoxItem)?.Content.ToString();
    }
}