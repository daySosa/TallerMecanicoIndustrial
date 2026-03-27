using Contabilidad;
using Dasboard_Prueba;
using InterfazClientes;
using Login;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace InterfazInventario
{
    /// <summary>
    /// Representa un repuesto dentro del inventario e implementa notificación de cambios de propiedades.
    /// </summary>
    public class Repuesto : INotifyPropertyChanged
    {
        /// <summary>
        /// Cantidad actual del producto.
        /// </summary>
        private int _producto_Cantidad_Actual;

        /// <summary>
        /// Obtiene o establece el identificador del producto.
        /// </summary>
        public int Producto_ID { get; set; }

        /// <summary>
        /// Obtiene o establece el nombre del producto.
        /// </summary>
        public string? Producto_Nombre { get; set; }

        /// <summary>
        /// Obtiene o establece la categoría del producto.
        /// </summary>
        public string? Producto_Categoria { get; set; }

        /// <summary>
        /// Obtiene o establece la marca del producto.
        /// </summary>
        public string? Producto_Marca { get; set; }

        /// <summary>
        /// Obtiene o establece el modelo del producto.
        /// </summary>
        public string? Producto_Modelo { get; set; }

        /// <summary>
        /// Obtiene o establece la cantidad mínima del producto.
        /// </summary>
        public int Producto_Cantidad_Minima { get; set; }

        /// <summary>
        /// Obtiene o establece el precio del producto.
        /// </summary>
        public decimal Producto_Precio { get; set; }

        /// <summary>
        /// Obtiene o establece la cantidad actual del producto.
        /// Notifica cambios y evalúa si el stock es bajo.
        /// </summary>
        public int Producto_Cantidad_Actual
        {
            get => _producto_Cantidad_Actual;
            set
            {
                _producto_Cantidad_Actual = value;
                OnPropertyChanged(nameof(Producto_Cantidad_Actual));
                OnPropertyChanged(nameof(StockBajo));
            }
        }

        /// <summary>
        /// Indica si el stock del producto está por debajo del mínimo.
        /// </summary>
        public bool StockBajo => Producto_Cantidad_Actual < Producto_Cantidad_Minima;

        /// <summary>
        /// Evento que se dispara cuando una propiedad cambia.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica que una propiedad ha cambiado.
        /// </summary>
        /// <param name="name">Nombre de la propiedad.</param>
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Ventana principal del módulo de inventario.
    /// Permite visualizar, filtrar, agregar y editar productos, así como gestionar notificaciones.
    /// </summary>
    public partial class MenúPrincipalInventario : Window
    {
        /// <summary>
        /// Instancia de acceso a base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Lista observable de repuestos.
        /// </summary>
        private ObservableCollection<Repuesto> _listaRepuestos = new ObservableCollection<Repuesto>();

        /// <summary>
        /// Vista filtrable de los repuestos.
        /// </summary>
        private ICollectionView? _vistaRepuestos;

        /// <summary>
        /// Filtro por categoría.
        /// </summary>
        private string? _filtroCategoria = null;

        /// <summary>
        /// Filtro de precio mínimo.
        /// </summary>
        private decimal _filtroPrecioMin = 0;

        /// <summary>
        /// Filtro de precio máximo.
        /// </summary>
        private decimal _filtroPrecioMax = decimal.MaxValue;

        /// <summary>
        /// Indica si se deben mostrar solo productos con stock bajo.
        /// </summary>
        private bool _filtroStockBajo = false;

        /// <summary>
        /// Inicializa una nueva instancia de la ventana principal de inventario.
        /// </summary>
        public MenúPrincipalInventario()
        {
            InitializeComponent();
            CargarDatos();
            CargarNotificaciones();
        }

        /// <summary>
        /// Carga los datos del inventario, incluyendo productos, categorías y configuración inicial de la vista.
        /// </summary>
        private void CargarDatos()
        {
            _listaRepuestos.Clear();
            try
            {
                var productos = _db.ObtenerProductos();
                foreach (var p in productos)
                    _listaRepuestos.Add(p);

                var categorias = _listaRepuestos
                    .Select(r => r.Producto_Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                categorias.Insert(0, "Todas");
                cmbCategoria.ItemsSource = categorias;
                cmbCategoria.SelectedIndex = 0;

                _vistaRepuestos = CollectionViewSource.GetDefaultView(_listaRepuestos);
                _vistaRepuestos.Filter = AplicarFiltros;
                dgInventario.ItemsSource = _vistaRepuestos;

                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Aplica los filtros definidos sobre un elemento del inventario.
        /// </summary>
        /// <param name="item">Elemento a evaluar.</param>
        /// <returns>True si el elemento cumple con los filtros; de lo contrario, false.</returns>
        private bool AplicarFiltros(object item)
        {
            if (item is not Repuesto r) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (r.Producto_Nombre ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Categoria ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Marca ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Modelo ?? "").ToLower().Contains(texto);
                if (!coincide) return false;
            }

            if (_filtroCategoria != null && _filtroCategoria != "Todas")
                if (r.Producto_Categoria != _filtroCategoria) return false;

            if (r.Producto_Precio < _filtroPrecioMin) return false;
            if (r.Producto_Precio > _filtroPrecioMax) return false;
            if (_filtroStockBajo && !r.StockBajo) return false;

            return true;
        }

        /// <summary>
        /// Maneja el evento TextChanged del control de búsqueda.
        /// </summary>
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        /// <summary>
        /// Muestra u oculta el panel de filtros.
        /// </summary>
        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        /// <summary>
        /// Aplica los filtros seleccionados por el usuario.
        /// </summary>
        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarRangoPrecios(txtPrecioMin.Text, txtPrecioMax.Text,
                    out decimal pMin, out decimal pMax))
                return;

            _filtroCategoria = cmbCategoria.SelectedItem?.ToString();
            _filtroPrecioMin = pMin;
            _filtroPrecioMax = pMax;
            _filtroStockBajo = chkStockBajo.IsChecked == true;
            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        /// <summary>
        /// Limpia todos los filtros aplicados.
        /// </summary>
        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbCategoria.SelectedIndex = 0;
            txtPrecioMin.Clear();
            txtPrecioMax.Clear();
            chkStockBajo.IsChecked = false;
            _filtroCategoria = null;
            _filtroPrecioMin = 0;
            _filtroPrecioMax = decimal.MaxValue;
            _filtroStockBajo = false;
            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        /// <summary>
        /// Actualiza el contador de elementos visibles en la vista.
        /// </summary>
        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaRepuestos != null)
                foreach (var _ in _vistaRepuestos) total++;
            tbTotalItems.Text = $"{total} item{(total != 1 ? "s" : "")}";
        }

        /// <summary>
        /// Maneja el evento de doble clic sobre un producto del inventario para editarlo.
        /// </summary>
        private void dgInventario_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgInventario.SelectedItem is Repuesto seleccionado)
            {
                var ventana = new InventarioWindow();
                ventana.CargarProductoParaEditar(seleccionado);
                ventana.ShowDialog();
                dgInventario.SelectedItem = null;
                CargarDatos();
                CargarNotificaciones();
            }
        }

        /// <summary>
        /// Abre la ventana para agregar un nuevo repuesto al inventario.
        /// </summary>
        private void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InventarioWindow();
            ventana.ShowDialog();
            CargarDatos();
            CargarNotificaciones();
        }

        /// <summary>
        /// Muestra u oculta el panel de notificaciones.
        /// </summary>
        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        /// <summary>
        /// Carga la cantidad de notificaciones pendientes.
        /// </summary>
        public void CargarNotificaciones()
        {
            try
            {
                int cantidad = _db.ContarNotificacionesPendientes();
                badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad > 99 ? "99+" : cantidad.ToString();
            }
            catch { }
        }

        /// <summary>
        /// Carga las notificaciones en el popup.
        /// </summary>
        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                DataTable dt = _db.ObtenerNotificacionesPendientes();

                if (dt.Rows.Count == 0)
                {
                    StackPanel vacio = new StackPanel
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 20)
                    };

                    vacio.Children.Add(new Label
                    {
                        Content = "🎉",
                        FontSize = 32,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Foreground = new SolidColorBrush(Colors.White),
                        Padding = new Thickness(0)
                    });

                    vacio.Children.Add(new TextBlock
                    {
                        Text = "Sin notificaciones pendientes",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    });

                    panelNotificaciones.Children.Add(vacio);
                    badgeContadorPopup.Visibility = Visibility.Collapsed;
                    btnMarcarTodas.Visibility = Visibility.Collapsed;
                    return;
                }

                txtContadorPopup.Text = dt.Rows.Count > 99 ? "99+" : dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString() ?? "";
                    string msg = row["Mensaje"].ToString() ?? "";
                    panelNotificaciones.Children.Add(CrearTarjetaNotificacion(id, tipo, msg));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crea una tarjeta de notificación.
        /// </summary>
        /// <param name="id">Identificador de la notificación.</param>
        /// <param name="tipo">Tipo de notificación.</param>
        /// <param name="mensaje">Mensaje de la notificación.</param>
        /// <returns>Un control Border que representa la tarjeta de notificación.</returns>
        private Border CrearTarjetaNotificacion(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorHex = esStock ? "#F0A500" : "#4CAF50";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";
            Color iconColor = (Color)ColorConverter.ConvertFromString(colorHex);

            Border card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(esStock ? "#1A1500" : "#0A2A0A")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel contenido = new StackPanel();
            Border badge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, iconColor.R, iconColor.G, iconColor.B)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            badge.Child = new TextBlock
            {
                Text = labelTipo,
                Foreground = new SolidColorBrush(iconColor),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            contenido.Children.Add(badge);
            contenido.Children.Add(new TextBlock
            {
                Text = mensaje,
                Foreground = Brushes.White,
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(contenido, 0);
            grid.Children.Add(contenido);

            Button btnLeida = new Button
            {
                Content = "✓",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = id
            };
            btnLeida.Click += (s, e) =>
            {
                _db.MarcarNotificacionLeida((int)((Button)s).Tag);
                CargarNotificacionesEnPopup();
                CargarNotificaciones();
            };
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(btnLeida);

            card.Child = grid;
            return card;
        }

        /// <summary>
        /// Marca todas las notificaciones como leídas.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            _db.MarcarNotificacionLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }

        /// <summary>
        /// Navega a la pantalla principal.
        /// </summary>
        private void btnHome_Click(object sender, RoutedEventArgs e) { new MenuPrincipal().Show(); this.Close(); }

        /// <summary>
        /// Navega al módulo de vehículos.
        /// </summary>
        private void btnVehiculos_Click(object sender, RoutedEventArgs e) { new Vehículos.MenúPrincipalVehículos().Show(); this.Close(); }

        /// <summary>
        /// Navega al módulo de clientes.
        /// </summary>
        private void btnClientes_Click(object sender, RoutedEventArgs e) { new MenúPrincipalClientes().Show(); this.Close(); }

        /// <summary>
        /// Navega al módulo de órdenes de trabajo.
        /// </summary>
        private void btnOrdenes_Click(object sender, RoutedEventArgs e) { new Órdenes_de_Trabajo.MenúPrincipalOrdenes().Show(); this.Close(); }

        /// <summary>
        /// Navega al módulo de egresos.
        /// </summary>
        private void btnEgresos_Click(object sender, RoutedEventArgs e) { new ContaWindow().Show(); this.Close(); }

        /// <summary>
        /// Navega al módulo de ingresos.
        /// </summary>
        private void btnIngresos_Click(object sender, RoutedEventArgs e) { new Contabilidad.MenuDePagos().Show(); this.Close(); }

        /// <summary>
        /// Cierra la sesión del usuario actual.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                new Login.MainWindow().Show();
                this.Close();
            }
        }
        /// <summary>
        /// Abre la ventana de reportes del inventario.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Inventario");
            ventana.ShowDialog();
        }
    }
}
