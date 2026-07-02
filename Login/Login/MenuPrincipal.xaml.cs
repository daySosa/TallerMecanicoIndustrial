using Contabilidad;
using InterfazClientes;
using InterfazInventario;
using LiveCharts;
using Login.Clases;
using Órdenes_de_Trabajo;
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
        public string Cliente_NombreCompleto { get; set; } = string.Empty;
        public string Vehiculo_Placa { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Estado { get; set; } = string.Empty;
        public decimal OrdenPrecio_Total { get; set; }
    }

    public class NotificacionItem
    {
        public int Notificacion_ID { get; set; }
        public string Tipo_Notificacion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public bool Leida { get; set; }
    }

    public partial class MenuPrincipal : Window
    {
        // ── Mapas estáticos: evita duplicar switch/if en varios métodos ──
        private static readonly Dictionary<string, string> ColorPorEstado = new()
        {
            ["En Espera"] = "#F0A500",
            ["En Proceso"] = "#4f6ef7",
            ["Sin Empezar"] = "#505880",
            ["Finalizado"] = "#4CAF50",
        };
        private const string ColorEstadoDefault = "#8890b5";

        private static readonly Dictionary<string, (string Color, string Icono)> InfoPorTipoNotificacion = new()
        {
            ["STOCK_BAJO"] = ("#F0A500", "AlertCircle"),
            ["ORDEN_FINALIZADA"] = ("#4CAF50", "CheckCircle"),
        };
        private const string NotifColorDefault = "#4A9EFF";
        private const string NotifIconoDefault = "Bell";

        private static readonly string[] DiasSemana = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };

        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();
        private static readonly Dictionary<string, Color> _cacheColores = new();

        // ── Estado interno ──────────────────────────────────────────
        private RepositorioSql? _db;
        private readonly CancellationTokenSource _cts = new();
        private bool _navegando;

        private DateTime _mesActual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private List<NotificacionItem> _notificaciones = new();
        private List<OrdenReciente> _ordenes = new();
        private int _mesesRango = 6;

        // ── Bindings de las gráficas ─────────────────────────────────
        public ChartValues<double> BalanceValues { get; } = new();
        public ChartValues<double> OrderValues { get; } = new();
        public ChartValues<double> GastosValues { get; } = new();
        public ChartValues<double> IngresosSemanalValues { get; } = new();
        public string[] IngresosSemanalLabels { get; set; } = Array.Empty<string>();

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public MenuPrincipal()
        {
            DataContext = this;
            InitializeComponent();

            try
            {
                _db = new RepositorioSql();
            }
            catch (Exception ex)
            {
                _db = null;
                MessageBox.Show(
                    "No se pudo conectar con la base de datos. Algunas funciones estarán deshabilitadas.\n\n" + ex.Message,
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            AplicarPermisos();
            CargarInfoUsuario();

            if (cmbRango.Items.Count > 1)
                cmbRango.SelectedIndex = 1;

            GenerarEncabezadoCalendario();
            GenerarCalendario();

            Closed += (_, _) => LiberarRecursos();

            // Carga todo en segundo plano; la ventana ya queda pintada y responde de inmediato
            _ = InicializarDashboardAsync();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
            catch (InvalidOperationException)
            {
                // DragMove puede fallar si el estado del mouse cambió a mitad del gesto; se ignora.
            }
        }

        private void LiberarRecursos()
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            {
                // La ventana se está cerrando; no hay más que hacer con estos errores.
            }
        }

        // ════════════════════════════════════════════════════════════
        // PERMISOS SEGÚN ROL
        // ════════════════════════════════════════════════════════════

        private void AplicarPermisos()
        {
            try
            {
                if (!SesionActual.EsAdministrador)
                {
                    btnUsuarios.Visibility = Visibility.Collapsed;
                    btnBitacora.Visibility = Visibility.Collapsed;
                    expanderContabilidad.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al aplicar permisos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // INFO DEL USUARIO LOGUEADO
        // ════════════════════════════════════════════════════════════

        private void CargarInfoUsuario()
        {
            try
            {
                string nombre = string.IsNullOrWhiteSpace(SesionActual.Nombre) ? "Usuario" : SesionActual.Nombre.Trim();
                string rol = SesionActual.EsAdministrador ? "Administrador" : "Empleado";

                txtNombreUsuario.Text = nombre;
                txtRolUsuario.Text = rol;
                string primerNombre = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nombre;
                txtSaludoHeader.Text = $"Hola, {primerNombre}";
                txtInicialesAvatar.Text = ObtenerIniciales(nombre);
            }
            catch (Exception ex)
            {
                txtNombreUsuario.Text = "Usuario";
                txtRolUsuario.Text = "—";
                txtSaludoHeader.Text = "Hola";
                txtInicialesAvatar.Text = "US";
                System.Diagnostics.Debug.WriteLine("Error al cargar info del usuario: " + ex.Message);
            }
        }

        private static string ObtenerIniciales(string nombreCompleto)
        {
            var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length >= 2) return $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
            if (partes.Length == 1 && partes[0].Length >= 2) return partes[0][..2].ToUpperInvariant();
            return "US";
        }

        // ════════════════════════════════════════════════════════════
        // CARGA ASÍNCRONA DEL DASHBOARD
        // ════════════════════════════════════════════════════════════

        private async Task InicializarDashboardAsync()
        {
            if (_db is null) return;

            // Corren en paralelo; cada una maneja sus propios errores
            // para que una falla no tumbe a las demás.
            await Task.WhenAll(
                CargarDatosAsync(),
                CargarGraficasAsync(),
                CargarNotificacionesAsync());
        }

        private async Task CargarDatosAsync()
        {
            if (_db is null) return;
            try
            {
                var (ordenes, balanceTotal, gastosTotal) = await Task.Run(
                    () => _db.ObtenerDatosDashboard(), _cts.Token);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                _ordenes = ordenes ?? new List<OrdenReciente>();
                dgOrdenes.ItemsSource = _ordenes;
                tbTotalOrdenes.Text = $"{_ordenes.Count} órdenes";
                txtTotalOrdenes.Text = _ordenes.Count.ToString();
                txtBalanceTotal.Text = $"L {balanceTotal:N2}";
                txtGastosTotales.Text = $"L {gastosTotal:N2}";

                GenerarCalendario();
            }
            catch (OperationCanceledException)
            {
                // La ventana se cerró antes de terminar la carga; no es un error real.
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // GRÁFICAS
        // ════════════════════════════════════════════════════════════

        private async void cmbRango_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
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
                    await CargarGraficasAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar el rango de la gráfica:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CargarGraficasAsync()
        {
            if (_db is null) return;
            try
            {
                DateTime desde = _mesesRango > 0
                    ? DateTime.Today.AddMonths(-_mesesRango + 1).AddDays(1 - DateTime.Today.Day)
                    : new DateTime(2000, 1, 1);

                var balTask = Task.Run(() => _db.ObtenerDatosGraficaOrdenes(desde), _cts.Token);
                var ordTask = Task.Run(() => _db.ObtenerDatosGraficaCantidadOrdenes(desde), _cts.Token);
                var gasTask = Task.Run(() => _db.ObtenerDatosGraficaGastos(desde), _cts.Token);

                await Task.WhenAll(balTask, ordTask, gasTask);

                if (_cts.IsCancellationRequested || !IsLoaded) return;

                var (balVals, balLabels) = balTask.Result;
                var (ordVals, _) = ordTask.Result;
                var (gasVals, _) = gasTask.Result;

                Actualizar(BalanceValues, balVals);
                Actualizar(OrderValues, ordVals);
                Actualizar(GastosValues, gasVals);
                Actualizar(IngresosSemanalValues, balVals);

                IngresosSemanalLabels = (balLabels is { Count: > 0 }) ? balLabels.ToArray() : new[] { "-" };
            }
            catch (OperationCanceledException)
            {
                // Cancelado por cierre de ventana; no es un error real.
            }
            catch (Exception ex)
            {
                if (IsLoaded)
                    MessageBox.Show("Error al cargar gráficas:\n" + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            static void Actualizar(ChartValues<double> chart, List<double>? vals)
            {
                chart.Clear();
                if (vals != null)
                    foreach (var v in vals) chart.Add(v);
                if (chart.Count == 0) chart.Add(0);
            }
        }

        // ════════════════════════════════════════════════════════════
        // NOTIFICACIONES
        // ════════════════════════════════════════════════════════════

        private async Task CargarNotificacionesAsync()
        {
            if (_db is null) return;
            try
            {
                var resultado = await Task.Run(() => _db.ObtenerTodasNotificaciones(), _cts.Token);
                if (_cts.IsCancellationRequested || !IsLoaded) return;
                _notificaciones = resultado ?? new List<NotificacionItem>();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                _notificaciones = new List<NotificacionItem>();
            }

            if (IsLoaded)
                ActualizarPanelNotificaciones();
        }

        private async void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await CargarNotificacionesAsync();
                popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al abrir notificaciones:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null) return;
            try
            {
                await Task.Run(() => _db.MarcarNotificacionLeida(null), _cts.Token);
                foreach (var n in _notificaciones) n.Leida = true;
                ActualizarPanelNotificaciones();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show("Error al marcar notificaciones como leídas:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void MarcarComoLeida(int id)
        {
            if (_db is null) return;
            try
            {
                await Task.Run(() => _db.MarcarNotificacionLeida(id), _cts.Token);
                var n = _notificaciones.FirstOrDefault(x => x.Notificacion_ID == id);
                if (n != null) n.Leida = true;
                ActualizarPanelNotificaciones();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al marcar notificación como leída: " + ex.Message);
            }
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
                var (hex, icon) = InfoPorTipoNotificacion.TryGetValue(notif.Tipo_Notificacion ?? string.Empty, out var info)
                    ? info
                    : (NotifColorDefault, NotifIconoDefault);

                Color iconColor = ObtenerColor(hex);

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
                    Background = new SolidColorBrush(Color.FromArgb(40, iconColor.R, iconColor.G, iconColor.B)),
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
                    Text = (notif.Tipo_Notificacion ?? string.Empty).Replace("_", " "),
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

        private void GenerarEncabezadoCalendario()
        {
            gridDiasHeader.Children.Clear();
            foreach (string d in DiasSemana)
                gridDiasHeader.Children.Add(new TextBlock
                {
                    Text = d,
                    Foreground = Pincel("#8B9BB4"),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                });
        }

        private void btnAnterior_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mesActual = _mesActual.AddMonths(-1);
                GenerarCalendario();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar de mes:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mesActual = _mesActual.AddMonths(1);
                GenerarCalendario();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar de mes:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerarCalendario()
        {
            try
            {
                var cultura = new CultureInfo("es-HN");
                string titulo = _mesActual.ToString("MMMM, yyyy", cultura);
                txtMesAnio.Text = char.ToUpper(titulo[0]) + titulo[1..];
                gridDias.Children.Clear();

                // Limpia el detalle al cambiar de mes
                panelOrdenesDia.Children.Clear();
                txtTituloOrdenesDia.Text = "Selecciona un día con órdenes";

                // Órdenes del mes indexadas por día
                var ordenesPorDia = _ordenes
                    .Where(o => o.Fecha.Year == _mesActual.Year && o.Fecha.Month == _mesActual.Month)
                    .GroupBy(o => o.Fecha.Day)
                    .ToDictionary(g => g.Key, g => g.ToList());

                int primerDia = (int)_mesActual.DayOfWeek;
                primerDia = primerDia == 0 ? 6 : primerDia - 1;
                int diasMes = DateTime.DaysInMonth(_mesActual.Year, _mesActual.Month);
                var mesAnterior = _mesActual.AddMonths(-1);
                int diasMesAnt = DateTime.DaysInMonth(mesAnterior.Year, mesAnterior.Month);

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

                    gridDias.Children.Add(CrearCeldaDia(dia, esDelMes, esHoy, tieneOrdenes, cantOrdenes,
                        tieneOrdenes ? ordenesPorDia[dia] : null));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al generar el calendario:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crea la celda visual de un día del calendario. Si tiene órdenes, dibuja
        /// un badge circular en la esquina superior derecha del número (como un
        /// contador de notificaciones), con la cantidad de órdenes dentro.
        /// </summary>
        private Grid CrearCeldaDia(int dia, bool esDelMes, bool esHoy, bool tieneOrdenes,
            int cantOrdenes, List<OrdenReciente>? ordenesDelDia)
        {
            var celda = new Grid
            {
                Width = 40,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
                Cursor = tieneOrdenes ? Cursors.Hand : Cursors.Arrow
            };

            // Círculo principal del número del día
            var circulo = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = esHoy
                    ? Pincel("#FF4757")
                    : tieneOrdenes
                        ? Pincel("#1a2060")
                        : Brushes.Transparent,
                BorderThickness = new Thickness(tieneOrdenes && !esHoy ? 1.5 : 0),
                BorderBrush = tieneOrdenes && !esHoy ? Pincel("#4f6ef7") : Brushes.Transparent
            };

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
            celda.Children.Add(circulo);

            // Badge circular con el número de órdenes, estilo indicador de notificación
            if (tieneOrdenes)
            {
                var badge = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    Background = Pincel("#4f6ef7"),
                    BorderBrush = Pincel("#0f1117"),
                    BorderThickness = new Thickness(1.5),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, -2, -2, 0)
                };
                badge.Child = new TextBlock
                {
                    Text = cantOrdenes > 9 ? "9+" : cantOrdenes.ToString(),
                    Foreground = Brushes.White,
                    FontSize = 8,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                celda.Children.Add(badge);

                if (ordenesDelDia != null)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var o in ordenesDelDia)
                        sb.AppendLine($"#{o.Orden_ID} — {o.Cliente_NombreCompleto} ({o.Estado})");
                    celda.ToolTip = sb.ToString().TrimEnd();

                    var ordenesCapturadas = ordenesDelDia;
                    celda.MouseLeftButtonDown += (s, e) => MostrarOrdenesDelDia(dia, ordenesCapturadas);
                }
            }

            return celda;
        }

        // ════════════════════════════════════════════════════════════
        // DETALLE DE ÓRDENES DEL DÍA SELECCIONADO
        // ════════════════════════════════════════════════════════════

        private void MostrarOrdenesDelDia(int dia, List<OrdenReciente> ordenesDelDia)
        {
            try
            {
                panelOrdenesDia.Children.Clear();

                var cultura = new CultureInfo("es-HN");
                string tituloMes = _mesActual.ToString("MMMM", cultura);
                txtTituloOrdenesDia.Text =
                    $"Órdenes del {dia} de {char.ToUpper(tituloMes[0]) + tituloMes[1..]} ({ordenesDelDia.Count})";

                foreach (var o in ordenesDelDia.OrderBy(x => x.Fecha))
                {
                    string hex = ColorPorEstado.TryGetValue(o.Estado ?? string.Empty, out var c) ? c : ColorEstadoDefault;
                    Color colorEstado = ObtenerColor(hex);

                    var card = new Border
                    {
                        Background = Pincel("#0f1117"),
                        BorderBrush = Pincel("#1e2235"),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12, 8, 12, 8),
                        Margin = new Thickness(0, 0, 0, 6),
                        Cursor = Cursors.Hand
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var texto = new StackPanel();
                    texto.Children.Add(new TextBlock
                    {
                        Text = $"#{o.Orden_ID} — {o.Cliente_NombreCompleto}",
                        Foreground = Brushes.White,
                        FontSize = 12,
                        FontWeight = FontWeights.Medium
                    });
                    texto.Children.Add(new TextBlock
                    {
                        Text = $"{o.Vehiculo_Placa}  ·  L {o.OrdenPrecio_Total:N2}",
                        Foreground = Pincel("#8890b5"),
                        FontSize = 11,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                    Grid.SetColumn(texto, 0);

                    var badgeEstado = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, colorEstado.R, colorEstado.G, colorEstado.B)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    badgeEstado.Child = new TextBlock
                    {
                        Text = o.Estado,
                        Foreground = new SolidColorBrush(colorEstado),
                        FontSize = 10,
                        FontWeight = FontWeights.SemiBold
                    };
                    Grid.SetColumn(badgeEstado, 1);

                    grid.Children.Add(texto);
                    grid.Children.Add(badgeEstado);
                    card.Child = grid;

                    card.MouseLeftButtonDown += (s, e) => Navegar(() => new MenúPrincipalOrdenes());

                    panelOrdenesDia.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al mostrar las órdenes del día:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ════════════════════════════════════════════════════════════

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            if (_navegando) return; // evita abrir dos ventanas por doble clic accidental
            _navegando = true;
            try
            {
                var ventana = crear();
                ventana.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                _navegando = false;
                MessageBox.Show("No se pudo abrir la ventana:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
            try
            {
                if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SesionActual.CerrarSesion();
                    Navegar(() => new Login.MainWindow());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cerrar sesión:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════════════

        private static SolidColorBrush Pincel(string hex)
        {
            if (_cachePinceles.TryGetValue(hex, out var existente))
                return existente;

            var brush = new SolidColorBrush(ObtenerColor(hex));
            brush.Freeze(); // WPF puede compartirlo entre hilos y se salta el chequeo de cambios en cada frame
            _cachePinceles[hex] = brush;
            return brush;
        }

        private static Color ObtenerColor(string hex)
        {
            if (_cacheColores.TryGetValue(hex, out var existente))
                return existente;

            Color color;
            try
            {
                color = (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                color = Colors.Gray; // valor seguro por si llega un hex inválido de la BD
            }
            _cacheColores[hex] = color;
            return color;
        }

        private static MaterialDesignThemes.Wpf.PackIconKind ParseIconKind(string nombre)
            => Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(nombre, out var k)
               ? k : MaterialDesignThemes.Wpf.PackIconKind.Bell;
    }
}