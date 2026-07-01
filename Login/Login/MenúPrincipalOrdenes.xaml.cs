using Dasboard_Prueba;
using Login;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public class OrdenTrabajo
    {
        public int Orden_ID { get; set; }
        public string Cliente_DNI { get; set; }
        public string? Cliente_NombreCompleto { get; set; }
        public string? Vehiculo_Placa { get; set; }
        public int? Producto_ID { get; set; }
        public string? Producto_Nombre { get; set; }
        public string? Producto_Categoria { get; set; }
        public string? Estado { get; set; }
        public DateTime Fecha { get; set; }
        public DateTime? Fecha_Entrega { get; set; }
        public string? Observaciones { get; set; }
        public decimal Servicio_Precio { get; set; }
        public decimal OrdenPrecio_Total { get; set; }
    }

    public partial class MenúPrincipalOrdenes : Window
    {
        private readonly clsConsultasBD _db = new();
        private readonly ObservableCollection<OrdenTrabajo> _listaOrdenes = new();
        private ICollectionView? _vistaOrdenes;
        private string _filtroCliente = "";
        private string _filtroEstado = "Todos";

        public MenúPrincipalOrdenes()
        {
            InitializeComponent();
            AplicarPermisos();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        // ════════════════════════════════════════════════════════════
        // PERMISOS SEGÚN ROL
        // ════════════════════════════════════════════════════════════

        private void AplicarPermisos()
        {
            if (!Login.Clases.clsSesion.EsAdministrador)
            {
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnBitacora.Visibility = Visibility.Collapsed;
                expanderContabilidad.Visibility = Visibility.Collapsed;
            }
        }

        // ── NAVEGACIÓN ───────────────────────────────────────────────

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            crear().Show();
            this.Close();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenuPrincipal());

        private void btnInventario_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazInventario.MenúPrincipalInventario());

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Vehículos.MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalOrdenes());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalBitacora());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalIngresos());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Login.Clases.clsSesion.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }
        // ── DATOS ────────────────────────────────────────────────────

        private void CargarDatosDesdeDB()
        {
            _listaOrdenes.Clear();
            try
            {
                foreach (var o in _db.ObtenerOrdenes())
                    _listaOrdenes.Add(o);

                if (_vistaOrdenes == null)
                {
                    _vistaOrdenes = CollectionViewSource.GetDefaultView(_listaOrdenes);
                    _vistaOrdenes.Filter = AplicarFiltros;
                    dgOrdenes.ItemsSource = _vistaOrdenes;
                }
                else
                {
                    _vistaOrdenes.Refresh();
                }

                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar órdenes:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not OrdenTrabajo o) return false;

            if (_filtroEstado != "Finalizado" && o.Estado == "Finalizado") return false;

            string busqueda = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(busqueda))
            {
                bool coincide =
                    (o.Cliente_NombreCompleto ?? "").ToLower().Contains(busqueda) ||
                    (o.Vehiculo_Placa ?? "").ToLower().Contains(busqueda) ||
                    (o.Producto_Nombre ?? "").ToLower().Contains(busqueda) ||
                    (o.Estado ?? "").ToLower().Contains(busqueda) ||
                    o.Orden_ID.ToString().Contains(busqueda);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroCliente) &&
                !(o.Cliente_NombreCompleto ?? "").ToLower().Contains(_filtroCliente))
                return false;

            if (_filtroEstado != "Todos" && o.Estado != _filtroEstado)
                return false;

            return true;
        }

        private void ActualizarContador()
        {
            int total = _vistaOrdenes?.Cast<object>().Count() ?? 0;
            tbTotalOrdenes.Text = $"{total} orden{(total != 1 ? "es" : "")}";
        }

        // ── BÚSQUEDA Y FILTROS ───────────────────────────────────────

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroCliente = txtFiltroCliente.Text?.Trim().ToLower() ?? "";
            _filtroEstado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroCliente.Clear();
            cmbFiltroEstado.SelectedIndex = 0;
            _filtroCliente = "";
            _filtroEstado = "Todos";
            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        // ── DATAGRID ─────────────────────────────────────────────────

        private async void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is not OrdenTrabajo seleccionada) return;
            dgOrdenes.SelectedItem = null;

            var ventana = new OrdenWindow();
            ventana.Closed += (s, args) =>
            {
                CargarDatosDesdeDB();
                CargarNotificaciones();
            };
            ventana.Show();
            await ventana.CargarOrdenParaEditar(seleccionada.Orden_ID);
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new OrdenWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Ordenes").ShowDialog();

        // ── NOTIFICACIONES ───────────────────────────────────────────

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
                    var vacio = new StackPanel
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
                        Foreground = Pincel("#6B7280"),
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
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            var contenido = new StackPanel();
            var badge = new Border
            {
                Background = Pincel(colorBorde + "33"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            badge.Child = new TextBlock
            {
                Text = labelTipo,
                Foreground = Pincel(colorBorde),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            contenido.Children.Add(badge);
            contenido.Children.Add(new TextBlock
            {
                Text = mensaje,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17
            });

            var btnLeida = new Button
            {
                Content = "✓",
                Foreground = Pincel("#6B7280"),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Marcar como leída",
                Tag = id
            };
            btnLeida.Click += (s, _) =>
            {
                _db.MarcarNotificacionLeida((int)((Button)s).Tag);
                CargarNotificacionesEnPopup();
                CargarNotificaciones();
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(contenido, 0);
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(contenido);
            grid.Children.Add(btnLeida);

            return new Border
            {
                Background = Pincel(colorFondo),
                BorderBrush = Pincel(colorBorde),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10),
                Child = grid
            };
        }

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            _db.MarcarNotificacionLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}