#nullable enable
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
using System.Windows.Media.Animation;
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

    /// <summary>
    /// Indica qué ventana originó la apertura de <see cref="MenuPrincipal"/>,
    /// para decidir qué tipo de transición de entrada corresponde:
    /// <list type="bullet">
    /// <item><see cref="CorreoYContrasena"/>: MenuPrincipal anima su propio fade-in simple.</item>
    /// <item><see cref="ReconocimientoFacial"/>: la ventana de origen ya precarga MenuPrincipal
    /// oculto (Opacity = 0) y controla un crossfade manual; MenuPrincipal NO debe animarse
    /// a sí mismo para evitar una doble animación.</item>
    /// </list>
    /// </summary>
    public enum OrigenIngreso
    {
        CorreoYContrasena,
        ReconocimientoFacial
    }

    public partial class MenuPrincipal : Window
    {
        #region Mapas y constantes estáticas (evitan duplicar switch/if en varios métodos)

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

        private static readonly string[] DiasSemana = ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"];

        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();
        private static readonly Dictionary<string, Color> _cacheColores = new();

        /// <summary>Duración del fade-in/fade-out de entrada y salida de esta ventana.</summary>
        private static readonly Duration DuracionFadeEntrada = new(TimeSpan.FromMilliseconds(220));

        #endregion

        #region Estado interno

        private RepositorioSql? _db;
        private readonly CancellationTokenSource _cts = new();
        private bool _navegando;

        /// <summary>
        /// Reemplaza el uso de <see cref="Window.IsLoaded"/> para saber si la ventana
        /// sigue "viva" y puede recibir actualizaciones de UI. IsLoaded solo se vuelve
        /// true después de que la ventana pasa por su ciclo de carga visual (que solo
        /// ocurre tras Show() o al agregarse al árbol visual). Cuando esta ventana se
        /// precarga oculta (Opacity = 0) sin Show() inmediato -como hace
        /// ReconocimientoFacial-, IsLoaded se queda en false mientras los datos de la
        /// BD ya llegaron, provocando que se descarten silenciosamente. Esta bandera,
        /// en cambio, solo se activa cuando la ventana realmente se cierra.
        /// </summary>
        private volatile bool _ventanaCerrada;

        private DateTime _mesActual = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        private List<NotificacionItem> _notificaciones = [];
        private List<OrdenReciente> _ordenes = [];
        private int _mesesRango = 6;

        /// <summary>Origen que abrió esta ventana; determina el tipo de transición de entrada.</summary>
        private readonly OrigenIngreso _origenIngreso;

        #endregion

        #region Bindings de las gráficas

        public ChartValues<double> BalanceValues { get; } = new();
        public ChartValues<double> OrderValues { get; } = new();
        public ChartValues<double> GastosValues { get; } = new();
        public ChartValues<double> IngresosSemanalValues { get; } = new();
        public string[] IngresosSemanalLabels { get; set; } = [];

        #endregion

        #region Constructor y ciclo de vida

        /// <summary>Constructor por defecto: asume acceso por correo y contraseña.</summary>
        public MenuPrincipal() : this(OrigenIngreso.CorreoYContrasena)
        {
        }

        /// <param name="origenIngreso">
        /// Ventana que originó la apertura. Controla si <see cref="MenuPrincipal"/> anima su
        /// propio fade-in de entrada (correo y contraseña) o si permanece pasivo porque la
        /// ventana de origen ya está controlando un crossfade externo (reconocimiento facial).
        /// </param>
        public MenuPrincipal(OrigenIngreso origenIngreso)
        {
            _origenIngreso = origenIngreso;

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

            Loaded += Window_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };

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
            }
        }

        #endregion

        #region Transición de entrada

        /// <summary>
        /// Aplica el fade-in de entrada solo cuando corresponde. Si el acceso fue por
        /// reconocimiento facial, la ventana de origen ya precargó esta instancia oculta
        /// (Opacity = 0) y controla su propio crossfade; animar aquí también duplicaría
        /// la transición y podría verse entrecortada.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_origenIngreso != OrigenIngreso.CorreoYContrasena)
                return;

            var fadeIn = new DoubleAnimation(0d, 1d, DuracionFadeEntrada)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        #endregion

        #region Permisos según rol

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

        #endregion

        #region Info del usuario logueado

        /// <summary>
        /// Carga en el sidebar y en el header el nombre completo (nombre + apellido)
        /// y el rol real de la sesión activa.
        /// </summary>
        private void CargarInfoUsuario()
        {
            try
            {
                string nombre = string.IsNullOrWhiteSpace(SesionActual.Nombre)
                    ? "Usuario" : SesionActual.Nombre.Trim();

                string apellido = string.IsNullOrWhiteSpace(SesionActual.Apellido)
                    ? string.Empty : SesionActual.Apellido.Trim();

                string nombreCompleto = string.IsNullOrEmpty(apellido)
                    ? nombre
                    : $"{nombre} {apellido}";

                string rol = string.IsNullOrWhiteSpace(SesionActual.Rol)
                    ? (SesionActual.EsAdministrador ? "Administrador" : "Empleado")
                    : SesionActual.Rol.Trim();

                txtNombreUsuario.Text = nombreCompleto;
                txtRolUsuario.Text = rol;
                txtSaludoHeader.Text = $"Hola, {nombre}";
                txtInicialesAvatar.Text = ObtenerIniciales(nombreCompleto);
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

        #endregion

        #region Carga asíncrona del dashboard

        private async Task InicializarDashboardAsync()
        {
            if (_db is null) return;

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

                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                _ordenes = ordenes ?? [];
                dgOrdenes.ItemsSource = _ordenes;
                tbTotalOrdenes.Text = $"{_ordenes.Count} órdenes";
                txtTotalOrdenes.Text = _ordenes.Count.ToString();
                txtBalanceTotal.Text = $"L {balanceTotal:N2}";
                txtGastosTotales.Text = $"L {gastosTotal:N2}";

                GenerarCalendario();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Gráficas

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

                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                var (balVals, balLabels) = balTask.Result;
                var (ordVals, _) = ordTask.Result;
                var (gasVals, _) = gasTask.Result;

                Actualizar(BalanceValues, balVals);
                Actualizar(OrderValues, ordVals);
                Actualizar(GastosValues, gasVals);
                Actualizar(IngresosSemanalValues, balVals);

                IngresosSemanalLabels = (balLabels is { Count: > 0 }) ? [.. balLabels] : ["-"];
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
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

        #endregion

        #region Notificaciones

        private async Task CargarNotificacionesAsync()
        {
            if (_db is null) return;
            try
            {
                var resultado = await Task.Run(() => _db.ObtenerTodasNotificaciones(), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;
                _notificaciones = resultado ?? [];
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                _notificaciones = [];
            }

            if (!_ventanaCerrada)
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
                n?.Leida = true;
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
                    Margin = new Thickness(0, 2, 8, 0),
                    Child = new MaterialDesignThemes.Wpf.PackIcon
                    {
                        Kind = ParseIconKind(icon),
                        Foreground = new SolidColorBrush(iconColor),
                        Width = 16,
                        Height = 16,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
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

        #endregion

        #region Calendario con órdenes

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

                panelOrdenesDia.Children.Clear();
                txtTituloOrdenesDia.Text = "Selecciona un día con órdenes";

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
                BorderBrush = tieneOrdenes && !esHoy ? Pincel("#4f6ef7") : Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = dia.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = tieneOrdenes || esHoy ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = esHoy ? Brushes.White
                                : esDelMes ? Brushes.White
                                : Pincel("#4A5568")
                }
            };
            celda.Children.Add(circulo);

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
                    Margin = new Thickness(0, -2, -2, 0),
                    Child = new TextBlock
                    {
                        Text = cantOrdenes > 9 ? "9+" : cantOrdenes.ToString(),
                        Foreground = Brushes.White,
                        FontSize = 8,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
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

        #endregion

        #region Detalle de órdenes del día seleccionado

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
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = o.Estado,
                            Foreground = new SolidColorBrush(colorEstado),
                            FontSize = 10,
                            FontWeight = FontWeights.SemiBold
                        }
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

        #endregion

        #region Navegación

        /// <summary>
        /// Navega a otra ventana con un crossfade real: la ventana nueva se crea y
        /// se muestra de inmediato (con su propio fade-in), mientras esta ventana
        /// hace fade-out en paralelo y recién se cierra al terminar su animación.
        /// Así ambas transiciones corren al mismo tiempo y se ven fluidas, sin
        /// pausa entre el cierre de una y la aparición de la otra.
        /// </summary>
        private void Navegar<T>(Func<T> crear) where T : Window
        {
            if (_navegando) return;
            _navegando = true;

            try
            {
                var ventana = crear();
                ventana.Show();

                var fadeOut = new DoubleAnimation(1d, 0d, DuracionFadeEntrada)
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                };
                fadeOut.Completed += (_, _) => Close();
                BeginAnimation(OpacityProperty, fadeOut);
            }
            catch (Exception ex)
            {
                _navegando = false;
                BeginAnimation(OpacityProperty, null);
                Opacity = 1;
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

        #endregion

        #region Helpers

        private static SolidColorBrush Pincel(string hex)
        {
            if (_cachePinceles.TryGetValue(hex, out var existente))
                return existente;

            var brush = new SolidColorBrush(ObtenerColor(hex));
            brush.Freeze();
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
                color = Colors.Gray;
            }
            _cacheColores[hex] = color;
            return color;
        }

        private static MaterialDesignThemes.Wpf.PackIconKind ParseIconKind(string nombre)
            => Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(nombre, out var k)
               ? k : MaterialDesignThemes.Wpf.PackIconKind.Bell;

        #endregion
    }
}