#nullable enable

using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Órdenes_de_Trabajo
{
    /// <summary>
    /// Ventana modal para seleccionar un producto del inventario y agregarlo
    /// como repuesto a una orden de trabajo. Se abre desde <see cref="OrdenWindow"/>
    /// vía <c>ShowDialog()</c> y expone el resultado en <see cref="RepuestoResultado"/>.
    /// </summary>
    public partial class AgregarRepuesto : Window
    {
        // ── Constantes ──
        private const string PrecioCeroTexto = "L 0.00";
        private const int StockMinimoAlerta = 5;

        private static readonly SolidColorBrush BrushStockBajo =
            new(Color.FromRgb(0xf4, 0x43, 0x36));
        private static readonly SolidColorBrush BrushStockNormal =
            new(Color.FromRgb(0x3a, 0x3f, 0x5c));
        private static readonly SolidColorBrush BrushBordeAzul =
            new(Color.FromRgb(0x4f, 0x6e, 0xf7));

        private readonly RepositorioSql _db = new();

        /// <summary>Producto actualmente seleccionado en el ComboBox, o null si no hay selección.</summary>
        private ValidadorInventario? _productoSeleccionado;

        /// <summary>
        /// Repuesto construido al presionar "Agregar Repuesto". Es null si el
        /// usuario cancela o cierra la ventana sin confirmar — <see cref="OrdenWindow"/>
        /// debe validar esto antes de usarlo.
        /// </summary>
        public RepuestoOrden? RepuestoResultado { get; private set; }

        public AgregarRepuesto()
        {
            InitializeComponent();
            _ = CargarProductosAsync();
        }

        /// <summary>Anima la aparición de la ventana con un fade-in, consistente con el resto de TRAMADE.</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        /// <summary>
        /// Carga el catálogo de productos en segundo plano para no bloquear la UI
        /// mientras la consulta a la base de datos se resuelve.
        /// </summary>
        private async Task CargarProductosAsync()
        {
            try
            {
                var productos = await Task.Run(() => _db.ObtenerProductosInventario());
                cmbProducto.ItemsSource = productos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── SELECCIÓN DE PRODUCTO ────────────────────────────────────

        private void cmbProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _productoSeleccionado = cmbProducto.SelectedItem as ValidadorInventario;
            if (_productoSeleccionado == null) return;

            txtCategoria.Text = _productoSeleccionado.Producto_Categoria;
            txtPrecioUnitario.Text = $"L {_productoSeleccionado.Producto_Precio:N2}";
            txtStock.Text = _productoSeleccionado.Producto_Cantidad_Actual.ToString();

            bool stockBajo = _productoSeleccionado.Producto_Cantidad_Actual <= StockMinimoAlerta;
            borderStock.BorderBrush = stockBajo ? BrushStockBajo : BrushStockNormal;

            CalcularSubtotal();
        }

        // ── CANTIDAD Y SUBTOTAL ───────────────────────────────────────

        /// <summary>Restringe la entrada de texto en el campo Cantidad a solo dígitos.</summary>
        private void txtCantidad_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void txtCantidad_TextChanged(object sender, TextChangedEventArgs e)
            => CalcularSubtotal();

        /// <summary>
        /// Recalcula el subtotal según cantidad y precio unitario. Si la cantidad
        /// ingresada supera el stock disponible, resalta el borde del subtotal en
        /// rojo como advertencia visual temprana (la validación real y bloqueante
        /// ocurre igual en <see cref="btnAgregar_Click"/> vía <c>ValidacionesGenerales.ValidarStock</c>).
        /// </summary>
        private void CalcularSubtotal()
        {
            if (_productoSeleccionado == null) return;

            bool valido = int.TryParse(txtCantidad.Text.Trim(), out int cantidad) && cantidad > 0;

            if (!valido)
            {
                txtSubtotal.Text = PrecioCeroTexto;
                borderSubtotal.BorderBrush = BrushBordeAzul;
                return;
            }

            txtSubtotal.Text = $"L {_productoSeleccionado.Producto_Precio * cantidad:N2}";

            bool excedeStock = cantidad > _productoSeleccionado.Producto_Cantidad_Actual;
            borderSubtotal.BorderBrush = excedeStock ? BrushStockBajo : BrushBordeAzul;
        }

        // ── ACCIONES ─────────────────────────────────────────────────

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidacionesGenerales.ValidarComboSeleccionado(_productoSeleccionado, "producto del inventario")) return;
            if (!ValidacionesGenerales.ValidarEnteroPositivo(txtCantidad.Text, out int cantidad, "Cantidad inválida")) return;
            if (!ValidacionesGenerales.ValidarStock(cantidad, _productoSeleccionado!.Producto_Cantidad_Actual)) return;

            RepuestoResultado = new RepuestoOrden
            {
                ProductoID = _productoSeleccionado.Producto_ID,
                Nombre = _productoSeleccionado.Producto_Nombre,
                Cantidad = cantidad,
                Precio = _productoSeleccionado.Producto_Precio,
                Incluido = true
            };

            Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            RepuestoResultado = null;
            Close();
        }
    }
}