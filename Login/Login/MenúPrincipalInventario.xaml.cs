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
    public class Repuesto : INotifyPropertyChanged
    {
        private int _producto_Cantidad_Actual;

        public int Producto_ID { get; set; }
        public string? Producto_Nombre { get; set; }
        public string? Producto_Categoria { get; set; }
        public string? Producto_Marca { get; set; }
        public string? Producto_Modelo { get; set; }
        public int Producto_Cantidad_Minima { get; set; }
        public decimal Producto_Precio { get; set; }

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

        public bool StockBajo => Producto_Cantidad_Actual < Producto_Cantidad_Minima;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MenúPrincipalInventario : Window
    {
        private clsConsultasBD _db = new clsConsultasBD();
        private ObservableCollection<Repuesto> _listaRepuestos = new ObservableCollection<Repuesto>();
        private ICollectionView? _vistaRepuestos;

        private string? _filtroCategoria = null;
        private decimal _filtroPrecioMin = 0;
        private decimal _filtroPrecioMax = decimal.MaxValue;
        private bool _filtroStockBajo = false;

        public MenúPrincipalInventario()
        {
            InitializeComponent();
            CargarDatos();
            CargarNotificaciones();
        }

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

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;


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

        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaRepuestos != null)
                foreach (var _ in _vistaRepuestos) total++;
            tbTotalItems.Text = $"{total} item{(total != 1 ? "s" : "")}";
        }

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

        private void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InventarioWindow();
            ventana.ShowDialog();
            CargarDatos();
            CargarNotificaciones();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

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

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            _db.MarcarNotificacionLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }

        private void MarcarLeida(int? id)
        {
            try
            {
                _db.MarcarNotificacionLeida(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al marcar notificación:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnHome_Click(object sender, RoutedEventArgs e) { new MenuPrincipal().Show(); this.Close(); }
        private void btnVehiculos_Click(object sender, RoutedEventArgs e) { new Vehículos.MenúPrincipalVehículos().Show(); this.Close(); }
        private void btnClientes_Click(object sender, RoutedEventArgs e) { new MenúPrincipalClientes().Show(); this.Close(); }
        private void btnOrdenes_Click(object sender, RoutedEventArgs e) { new Órdenes_de_Trabajo.MenúPrincipalOrdenes().Show(); this.Close(); }
        private void btnEgresos_Click(object sender, RoutedEventArgs e) { new ContaWindow().Show(); this.Close(); }
        private void btnIngresos_Click(object sender, RoutedEventArgs e) { new Contabilidad.MenuDePagos().Show(); this.Close(); }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                new Login.MainWindow().Show();
                this.Close();
            }
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Inventario");
            ventana.ShowDialog();
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not Repuesto r)
                return false;

            if (!string.IsNullOrWhiteSpace(txtBuscar.Text) &&
                !(r.Producto_Nombre?.ToLower().Contains(txtBuscar.Text.ToLower()) ?? false))
                return false;

            if (!string.IsNullOrEmpty(_filtroCategoria) && _filtroCategoria != "Todas" &&
                r.Producto_Categoria != _filtroCategoria)
                return false;

            if (r.Producto_Precio < _filtroPrecioMin || r.Producto_Precio > _filtroPrecioMax)
                return false;

            if (_filtroStockBajo && !r.StockBajo)
                return false;

            return true;
        }
    }
}