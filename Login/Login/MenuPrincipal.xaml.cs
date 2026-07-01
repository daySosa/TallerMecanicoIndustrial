using Contabilidad;
using InterfazClientes;
using InterfazInventario;
using LiveCharts;
using Login.Clases;
using Órdenes_de_Trabajo;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vehículos;

namespace Dasboard_Prueba
{
    public class OrdenReciente
    {
        public int Orden_ID { get; set; }
        public string Cliente_NombreCompleto { get; set; }
        public string Vehiculo_Placa { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public decimal OrdenPrecio_Total { get; set; }
    }

    public class NotificacionItem
    {
        public int Notificacion_ID { get; set; }
        public string Tipo_Notificacion { get; set; }
        public string Mensaje { get; set; }
        public bool Leida { get; set; }
    }

    public partial class MenuPrincipal : Window
    {
        private readonly clsConsultasBD _db = new clsConsultasBD();

        private DateTime _mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private List<NotificacionItem> _notificaciones = new();
        private List<OrdenReciente> _ordenes = new();
        private int _mesesRango = 6;

        // ── Chart bindings ───────────────────────────────────────────
        public ChartValues<double> BalanceValues { get; set; }
        public ChartValues<double> OrderValues { get; set; }
        public ChartValues<double> GastosValues { get; set; }
        public ChartValues<double> IngresosSemanalValues { get; set; }
        public string[] IngresosSemanalLabels { get; set; }

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public MenuPrincipal()
        {
            BalanceValues = new ChartValues<double>();
            OrderValues = new ChartValues<double>();
            GastosValues = new ChartValues<double>();
            IngresosSemanalValues = new ChartValues<double>();
            IngresosSemanalLabels = Array.Empty<string>();

            DataContext = this;
            InitializeComponent();
            AplicarPermisos();

            cmbRango.SelectedIndex = 1;

            CargarDatos();
            CargarGraficas();
            CargarNotificaciones();
            GenerarCalendario();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
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
        // ════════════════════════════════════════════════════════════
        // DATOS
        // ════════════════════════════════════════════════════════════

        private void CargarDatos()
        {
            try
            {
                var (ordenes, balanceTotal, gastosTotal) = _db.ObtenerDatosDashboard();
                _ordenes = ordenes;

                dgOrdenes.ItemsSource = ordenes;
                tbTotalOrdenes.Text = $"{ordenes.Count} órdenes";
                txtTotalOrdenes.Text = ordenes.Count.ToString();
                txtBalanceTotal.Text = $"L {balanceTotal:N2}";
                txtGastosTotales.Text = $"L {gastosTotal:N2}";

                GenerarCalendario(); // recarga con órdenes ya disponibles
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // GRÁFICAS
        // ════════════════════════════════════════════════════════════

        private void cmbRango_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRango.SelectedItem is ComboBoxItem item)
            {
                _mesesRango = item.Tag?.ToString() switch
                {
                    "3" => 3,
                    "6" => 6,
                    "12" => 12,
                    _ => 0
                };
                CargarGraficas();
            }
        }

        private void CargarGraficas()
        {
            try
            {
                DateTime desde = _mesesRango > 0
                    ? DateTime.Today.AddMonths(-_mesesRango + 1).AddDays(1 - DateTime.Today.Day)
                    : new DateTime(2000, 1, 1);

                var (balVals, balLabels) = _db.ObtenerDatosGraficaOrdenes(desde);
                var (ordVals, ordLabels) = _db.ObtenerDatosGraficaCantidadOrdenes(desde);
                var (gasVals, gasLabels) = _db.ObtenerDatosGraficaGastos(desde);

                Actualizar(BalanceValues, balVals);
                Actualizar(OrderValues, ordVals);
                Actualizar(GastosValues, gasVals);
                Actualizar(IngresosSemanalValues, balVals);

                IngresosSemanalLabels = balLabels.Count > 0
                    ? balLabels.ToArray() : new[] { "-" };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gráficas:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            static void Actualizar(ChartValues<double> chart, List<double> vals)
            {
                chart.Clear();
                foreach (var v in vals) chart.Add(v);
                if (chart.Count == 0) chart.Add(0);
            }
        }

        // ════════════════════════════════════════════════════════════
        // NOTIFICACIONES
        // ════════════════════════════════════════════════════════════

        private void CargarNotificaciones()
        {
            _notificaciones.Clear();
            try { _notificaciones = _db.ObtenerTodasNotificaciones(); }
            catch { }
            ActualizarPanelNotificaciones();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            CargarNotificaciones();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            try { _db.MarcarNotificacionLeida(null); }
            catch { }
            foreach (var n in _notificaciones) n.Leida = true;
            ActualizarPanelNotificaciones();
        }

        private void MarcarComoLeida(int id)
        {
            try { _db.MarcarNotificacionLeida(id); }
            catch { }
            var n = _notificaciones.FirstOrDefault(x => x.Notificacion_ID == id);
            if (n != null) n.Leida = true;
            ActualizarPanelNotificaciones();
        }

        private void ActualizarPanelNotificaciones()
        {
            panelNotificaciones.Children.Clear();
            int noLeidas = _notificaciones.Count(n => !n.Leida);

            badgeNotificaciones.Visibility = noLeidas > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtContadorNotificaciones.Text = noLeidas > 99 ? "99+" : noLeidas.ToString();
            badgeContadorPopup.Visibility = noLeidas > 0 ? Visibility.Visible : Visibility.Collapsed;
            txtContadorPopup.Text = noLeidas > 99 ? "99+" : noLeidas.ToString();
            btnMarcarTodas.Visibility = noLeidas > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_notificaciones.Count == 0)
            {
                panelNotificaciones.Children.Add(new TextBlock
                {
                    Text = "Sin notificaciones pendientes",
                    Foreground = Pincel("#6B7280"),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
                return;
            }

            foreach (var notif in _notificaciones)
            {
                string hex = notif.Tipo_Notificacion == "STOCK_BAJO" ? "#F0A500"
                            : notif.Tipo_Notificacion == "ORDEN_FINALIZADA" ? "#4CAF50"
                            : "#4A9EFF";
                string icon = notif.Tipo_Notificacion == "STOCK_BAJO" ? "AlertCircle"
                            : notif.Tipo_Notificacion == "ORDEN_FINALIZADA" ? "CheckCircle"
                            : "Bell";

                Color iconColor = (Color)ColorConverter.ConvertFromString(hex);

                var card = new Border
                {
                    Background = notif.Leida ? Pincel("#1E2130") : Pincel("#1A2A3D"),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = Cursors.Hand
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var iconBorder = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Background = new SolidColorBrush(
                        Color.FromArgb(40, iconColor.R, iconColor.G, iconColor.B)),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 8, 0)
                };
                iconBorder.Child = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = ParseIconKind(icon),
                    Foreground = new SolidColorBrush(iconColor),
                    Width = 16,
                    Height = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconBorder, 0);

                var texto = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                texto.Children.Add(new TextBlock
                {
                    Text = notif.Tipo_Notificacion.Replace("_", " "),
                    Foreground = new SolidColorBrush(iconColor),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                texto.Children.Add(new TextBlock
                {
                    Text = notif.Mensaje,
                    Foreground = notif.Leida ? Pincel("#8B9BB4") : new SolidColorBrush(Colors.White),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetColumn(texto, 1);

                var punto = new Border
                {
                    Width = 8,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = Pincel("#3D7EFF"),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(8, 4, 0, 0),
                    Visibility = notif.Leida ? Visibility.Collapsed : Visibility.Visible
                };
                Grid.SetColumn(punto, 2);

                grid.Children.Add(iconBorder);
                grid.Children.Add(texto);
                grid.Children.Add(punto);
                card.Child = grid;

                int nid = notif.Notificacion_ID;
                card.MouseLeftButtonDown += (s, e) => MarcarComoLeida(nid);
                panelNotificaciones.Children.Add(card);
            }
        }

        // ════════════════════════════════════════════════════════════
        // CALENDARIO CON ÓRDENES
        // ════════════════════════════════════════════════════════════

        private void btnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(-1);
            GenerarCalendario();
        }

        private void btnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(1);
            GenerarCalendario();
        }

        private void GenerarCalendario()
        {
            var cultura = new CultureInfo("es-HN");
            string titulo = _mesActual.ToString("MMMM, yyyy", cultura);
            txtMesAnio.Text = char.ToUpper(titulo[0]) + titulo[1..];
            gridDias.Children.Clear();

            // Encabezados
            foreach (string d in new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" })
                gridDias.Children.Add(new TextBlock
                {
                    Text = d,
                    Foreground = Pincel("#8B9BB4"),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                });

            // Órdenes del mes indexadas por día
            var ordenesPorDia = _ordenes
                .Where(o => o.Fecha.Year == _mesActual.Year
                         && o.Fecha.Month == _mesActual.Month)
                .GroupBy(o => o.Fecha.Day)
                .ToDictionary(g => g.Key, g => g.ToList());

            int primerDia = (int)_mesActual.DayOfWeek;
            primerDia = primerDia == 0 ? 6 : primerDia - 1;
            int diasMes = DateTime.DaysInMonth(_mesActual.Year, _mesActual.Month);
            int diasMesAnt = DateTime.DaysInMonth(
                _mesActual.AddMonths(-1).Year, _mesActual.AddMonths(-1).Month);

            for (int i = 0; i < 42; i++)
            {
                int dia; bool esDelMes;
                if (i < primerDia) { dia = diasMesAnt - primerDia + 1 + i; esDelMes = false; }
                else if (i >= primerDia + diasMes) { dia = i - primerDia - diasMes + 1; esDelMes = false; }
                else { dia = i - primerDia + 1; esDelMes = true; }

                bool esHoy = esDelMes
                          && dia == DateTime.Today.Day
                          && _mesActual.Month == DateTime.Today.Month
                          && _mesActual.Year == DateTime.Today.Year;

                bool tieneOrdenes = esDelMes && ordenesPorDia.ContainsKey(dia);
                int cantOrdenes = tieneOrdenes ? ordenesPorDia[dia].Count : 0;

                // Contenedor del día
                var celda = new Grid
                {
                    Width = 34,
                    Height = 38,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2)
                };
                celda.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
                celda.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });

                // Círculo del número
                var circulo = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = esHoy
                        ? Pincel("#FF4757")
                        : tieneOrdenes && !esHoy
                            ? Pincel("#1a2060")
                            : Brushes.Transparent
                };

                if (tieneOrdenes && !esHoy)
                    circulo.BorderBrush = Pincel("#4f6ef7");
                circulo.BorderThickness = new Thickness(1.5);

                circulo.Child = new TextBlock
                {
                    Text = dia.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = tieneOrdenes || esHoy ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = esHoy ? Brushes.White
                                : esDelMes ? Brushes.White
                                : Pincel("#4A5568")
                };
                Grid.SetRow(circulo, 0);
                celda.Children.Add(circulo);

                // Punto indicador de órdenes
                if (tieneOrdenes && !esHoy)
                {
                    var dot = new Border
                    {
                        Width = cantOrdenes > 1 ? 14 : 6,
                        Height = 4,
                        CornerRadius = new CornerRadius(2),
                        Background = Pincel("#4f6ef7"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetRow(dot, 1);
                    celda.Children.Add(dot);

                    // Tooltip con info de órdenes
                    var sb = new System.Text.StringBuilder();
                    foreach (var o in ordenesPorDia[dia])
                        sb.AppendLine($"#{o.Orden_ID} — {o.Cliente_NombreCompleto} ({o.Estado})");
                    celda.ToolTip = sb.ToString().TrimEnd();
                }

                gridDias.Children.Add(celda);
            }
        }

        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ════════════════════════════════════════════════════════════

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            crear().Show();
            this.Close();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalInventario());

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalOrdenes());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalBitacora());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalIngresos());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Login.Clases.clsSesion.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════

        private static SolidColorBrush Pincel(string hex)
            => new((Color)ColorConverter.ConvertFromString(hex));

        private static MaterialDesignThemes.Wpf.PackIconKind ParseIconKind(string nombre)
            => Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(nombre, out var k)
               ? k : MaterialDesignThemes.Wpf.PackIconKind.Bell;
    }
}