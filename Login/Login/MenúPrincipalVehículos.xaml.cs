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

namespace Vehículos
{
    public partial class MenúPrincipalVehículos : Window
    {
        private readonly clsConsultasBD _db = new();
        private readonly ObservableCollection<Vehiculo> _listaVehiculos = new();
        private ICollectionView? _vistaVehiculos;
        private string _filtroPlaca = string.Empty;
        private string _filtroModelo = string.Empty;

        public MenúPrincipalVehículos()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
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

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalOrdenes());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalIngresos());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Navegar(() => new Login.MainWindow());
        }

        // ── DATOS ────────────────────────────────────────────────────

        private void CargarDatosDesdeDB()
        {
            _listaVehiculos.Clear();
            try
            {
                foreach (var v in _db.ObtenerVehiculos())
                    _listaVehiculos.Add(v);

                _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                _vistaVehiculos.Filter = AplicarFiltros;
                dgVehiculos.ItemsSource = _vistaVehiculos;
                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar vehículos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not Vehiculo v) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? string.Empty;
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (v.Vehiculo_Placa ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Marca ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Modelo ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Tipo ?? "").ToLower().Contains(texto) ||
                    (v.Cliente_NombreCompleto ?? "").ToLower().Contains(texto);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroPlaca) &&
                !(v.Vehiculo_Placa ?? "").ToLower().Contains(_filtroPlaca)) return false;

            if (!string.IsNullOrEmpty(_filtroModelo) &&
                !(v.Vehiculo_Modelo ?? "").ToLower().Contains(_filtroModelo)) return false;

            return true;
        }

        private void ActualizarContador()
        {
            int total = _vistaVehiculos?.Cast<object>().Count() ?? 0;
            tbTotalVehiculos.Text = $"{total} vehículo{(total != 1 ? "s" : "")}";
        }

        // ── BÚSQUEDA Y FILTROS ───────────────────────────────────────

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroPlaca = txtFiltroPlaca.Text?.Trim().ToLower() ?? string.Empty;
            _filtroModelo = txtFiltroModelo.Text?.Trim().ToLower() ?? string.Empty;
            popupFiltros.IsOpen = false;
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroPlaca.Clear();
            txtFiltroModelo.Clear();
            _filtroPlaca = string.Empty;
            _filtroModelo = string.Empty;
            popupFiltros.IsOpen = false;
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        // ── DATAGRID ─────────────────────────────────────────────────

        private void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVehiculos.SelectedItem is Vehiculo seleccionado)
            {
                var ventana = new VehiWindow();
                ventana.CargarVehiculoParaEditar(seleccionado);
                ventana.ShowDialog();
                dgVehiculos.SelectedItem = null;
                CargarDatosDesdeDB();
            }
        }

        private void BtnNuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            new VehiWindow().ShowDialog();
            CargarDatosDesdeDB();
        }

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
                txtContadorNotificaciones.Text = cantidad.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
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
                        Foreground = Pincel("#FFFFFF"),
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

                txtContadorPopup.Text = dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString() ?? string.Empty;
                    string msg = row["Mensaje"].ToString() ?? string.Empty;
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
                Foreground = Pincel("#FFFFFF"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17
            });

            var btnLeida = new Button
            {
                Content = "✓",
                Foreground = Pincel("#6B7280"),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
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

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Vehiculos").ShowDialog();

        // ── HELPER ───────────────────────────────────────────────────

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}