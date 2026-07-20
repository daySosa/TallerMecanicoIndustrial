#nullable enable
using Contabilidad;
using Dasboard_Prueba;
using InterfazClientes;
using Login.Clases;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Órdenes_de_Trabajo
{
    /// <summary>Representa un evento individual registrado en la bitácora del sistema.</summary>
    public class BitacoraItem
    {
        public DateTime Bitacora_Fecha { get; set; }
        public string Bitacora_Usuario { get; set; } = string.Empty;
        public string Bitacora_Rol { get; set; } = string.Empty;
        public string Bitacora_Modulo { get; set; } = string.Empty;
        public string Bitacora_Accion { get; set; } = string.Empty;
        public string Bitacora_Descripcion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Ventana principal del módulo de Bitácora. Muestra el historial de acciones
    /// realizadas en el sistema, con estadísticas rápidas, filtros combinables
    /// (usuario, módulo, rango de fechas, búsqueda libre) y notificaciones pendientes.
    /// </summary>
    public partial class MenúPrincipalBitacora : Window
    {
        #region Constantes y caché estática

        /// <summary>Duración de las transiciones de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        /// <summary>Caché de pinceles ya congelados, para no crear un SolidColorBrush nuevo en cada render.</summary>
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        private const string TituloError = "Error";

        #endregion

        #region Estado interno

        // La conexión se abre de forma asíncrona en el Loaded para no bloquear el hilo
        // de UI justo cuando Navegar<T> está creando esta ventana (antes de Show() y
        // del fade-in). Así la navegación se siente instantánea.
        private RepositorioSql? _db;
        private List<BitacoraItem> _listaCompleta = new();
        private readonly CancellationTokenSource _cts = new();

        /// <summary>Debounce para no re-filtrar en cada tecla mientras se escribe en los buscadores.</summary>
        private readonly DispatcherTimer _debounceFiltros;

        private bool _navegando;

        /// <summary>
        /// Indica si la ventana ya fue cerrada. Se usa para evitar tocar controles
        /// de UI desde continuaciones async que terminan después del cierre.
        /// </summary>
        private volatile bool _ventanaCerrada;

        /// <summary>Evita el reingreso a Window_Closing mientras se reproduce el fade-out.</summary>
        private bool _cerrandoConAnimacion;

        #endregion

        public MenúPrincipalBitacora()
        {
            InitializeComponent();

            AplicarPermisos();
            CargarInfoUsuario();
            _debounceFiltros = ConfigurarDebounce();

            Loaded += MenúPrincipalBitacora_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida y transición de entrada/salida

        private async void MenúPrincipalBitacora_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _db = await Task.Run(() => new RepositorioSql(), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _db = null;
                if (!_ventanaCerrada)
                    MessageBox.Show(
                        "No se pudo conectar con la base de datos. Algunas funciones estarán deshabilitadas.\n\n" + ex.Message,
                        "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (_ventanaCerrada) return;

            await CargarBitacoraAsync();
            await CargarNotificacionesAsync();
        }

        /// <summary>Aplica un fade-in suave al mostrar la ventana (entra con Opacity="0" desde XAML).</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Intercepta el cierre para reproducir un fade-out antes de cerrar de verdad.
        /// La primera vez cancela el cierre y dispara la animación; al terminar, se
        /// vuelve a llamar a Close() con la bandera activada para cerrar sin interrupciones.
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_cerrandoConAnimacion) return;

            e.Cancel = true;
            _cerrandoConAnimacion = true;

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>Libera los recursos propios (token de cancelación, repositorio, timer) al cerrar la ventana.</summary>
        private void LiberarRecursos()
        {
            try
            {
                _debounceFiltros.Stop();
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            {
                // Liberación best-effort al cerrar la ventana; no debe interrumpir el cierre.
            }
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
                // DragMove() puede lanzar si el botón ya no está presionado al momento
                // de procesarse el evento; se ignora intencionalmente.
            }
        }

        private DispatcherTimer ConfigurarDebounce()
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                AplicarFiltros();
            };
            return timer;
        }

        /// <summary>
        /// Navega a otra ventana con un crossfade real: la ventana nueva se crea y
        /// se muestra de inmediato (con su propio fade-in), mientras esta ventana
        /// hace fade-out en paralelo y recién se cierra al terminar su animación.
        /// </summary>
        private void Navegar<T>(Func<T> crear) where T : Window
        {
            if (_navegando) return;
            _navegando = true;

            try
            {
                var ventana = crear();
                ventana.Show();

                var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
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
                    TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Permisos según rol

        private void AplicarPermisos()
        {
            if (!SesionActual.EsAdministrador)
            {
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnBitacora.Visibility = Visibility.Collapsed;
                expanderContabilidad.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Info del usuario logueado

        /// <summary>Carga en el sidebar el nombre completo, el rol y las iniciales del usuario con sesión activa.</summary>
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

                string rol = !string.IsNullOrWhiteSpace(SesionActual.Rol)
                    ? SesionActual.Rol.Trim()
                    : SesionActual.EsAdministrador ? "Administrador" : "Empleado";

                string iniciales = string.IsNullOrEmpty(apellido)
                    ? nombre[..Math.Min(2, nombre.Length)].ToUpperInvariant()
                    : $"{nombre[0]}{apellido[0]}".ToUpperInvariant();

                txtNombreUsuario.Text = nombreCompleto;
                txtRolUsuario.Text = rol;
                txtIniciales.Text = iniciales;
            }
            catch (Exception ex)
            {
                txtNombreUsuario.Text = "Usuario";
                txtRolUsuario.Text = "—";
                txtIniciales.Text = "US";
                System.Diagnostics.Debug.WriteLine("Error al cargar info del usuario: " + ex.Message);
            }
        }

        #endregion

        #region Carga de datos

        private async Task CargarBitacoraAsync()
        {
            if (_db is null) return;
            try
            {
                List<BitacoraItem> datos = await Task.Run(() => _db.ObtenerBitacora(), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                _listaCompleta = datos;
                AplicarFiltros();
                ActualizarEstadisticas(_listaCompleta);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al cargar bitácora:\n" + ex.Message,
                        TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Filtra la lista completa según usuario, módulo, rango de fechas y texto de búsqueda combinados.</summary>
        private void AplicarFiltros()
        {
            string usuario = txtFiltroUsuario.Text.Trim().ToLowerInvariant();
            string modulo = (cmbFiltroModulo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            DateTime? desde = dtpDesde.SelectedDate;
            DateTime? hasta = dtpHasta.SelectedDate?.AddDays(1);
            string buscar = txtBuscar.Text.Trim().ToLowerInvariant();

            var filtrada = _listaCompleta.Where(b =>
            {
                if (!string.IsNullOrEmpty(usuario) &&
                    !b.Bitacora_Usuario.ToLowerInvariant().Contains(usuario)) return false;

                if (modulo != "Todos" &&
                    !b.Bitacora_Modulo.Equals(modulo, StringComparison.OrdinalIgnoreCase)) return false;

                if (desde.HasValue && b.Bitacora_Fecha < desde.Value) return false;
                if (hasta.HasValue && b.Bitacora_Fecha > hasta.Value) return false;

                if (!string.IsNullOrEmpty(buscar) &&
                    !b.Bitacora_Usuario.ToLowerInvariant().Contains(buscar) &&
                    !b.Bitacora_Modulo.ToLowerInvariant().Contains(buscar) &&
                    !b.Bitacora_Accion.ToLowerInvariant().Contains(buscar) &&
                    !b.Bitacora_Descripcion.ToLowerInvariant().Contains(buscar)) return false;

                return true;
            }).ToList();

            dgBitacora.ItemsSource = filtrada;
            tbTotalEventos.Text = $"{filtrada.Count} evento{(filtrada.Count != 1 ? "s" : "")}";
        }

        /// <summary>Recalcula las tarjetas de estadísticas rápidas (total, hoy, usuarios activos, módulo más activo).</summary>
        private void ActualizarEstadisticas(List<BitacoraItem> lista)
        {
            tbTotalRegistros.Text = lista.Count.ToString();

            tbRegistrosHoy.Text = lista
                .Count(b => b.Bitacora_Fecha.Date == DateTime.Today)
                .ToString();

            tbUsuariosActivos.Text = lista
                .Select(b => b.Bitacora_Usuario)
                .Distinct()
                .Count()
                .ToString();

            tbModuloActivo.Text = lista
                .GroupBy(b => b.Bitacora_Modulo)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "—";
        }

        #endregion

        #region Búsqueda y filtros

        // ── Ambos campos de texto usan debounce para no re-filtrar en cada tecla ──
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceFiltros.Stop();
            _debounceFiltros.Start();
        }

        private void txtFiltroUsuario_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceFiltros.Stop();
            _debounceFiltros.Start();
        }

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e) => AplicarFiltros();

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroUsuario.Clear();
            cmbFiltroModulo.SelectedIndex = 0;
            dtpDesde.SelectedDate = null;
            dtpHasta.SelectedDate = null;
            txtBuscar.Clear();
            AplicarFiltros();
        }

        #endregion

        #region Notificaciones

        public async Task CargarNotificacionesAsync()
        {
            if (_db is null) return;
            try
            {
                int cantidad = await Task.Run(() => _db.ContarNotificacionesPendientes(), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad > 99 ? "99+" : cantidad.ToString();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private async void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                await CargarNotificacionesEnPopupAsync();

            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        private async Task CargarNotificacionesEnPopupAsync()
        {
            if (_db is null) return;
            panelNotificaciones.Children.Clear();
            try
            {
                DataTable dt = await Task.Run(() => _db.ObtenerNotificacionesPendientes(), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                if (dt.Rows.Count == 0)
                {
                    MostrarSinNotificaciones();
                    return;
                }

                txtContadorPopup.Text = dt.Rows.Count > 99 ? "99+" : dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                    panelNotificaciones.Children.Add(CrearTarjeta(
                        Convert.ToInt32(row["Notificacion_ID"]),
                        row["Tipo_Notificacion"].ToString() ?? string.Empty,
                        row["Mensaje"].ToString() ?? string.Empty));
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error notificaciones: " + ex.Message,
                    TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarSinNotificaciones()
        {
            panelNotificaciones.Children.Add(new TextBlock
            {
                Text = "Sin notificaciones pendientes",
                Foreground = Pincel("#6B7280"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            });
            badgeContadorPopup.Visibility = Visibility.Collapsed;
            btnMarcarTodas.Visibility = Visibility.Collapsed;
        }

        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            var badge = new Border
            {
                Background = Pincel(colorBorde + "33"),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5),
                Child = new TextBlock
                {
                    Text = labelTipo,
                    Foreground = Pincel(colorBorde),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold
                }
            };

            var contenido = new StackPanel();
            contenido.Children.Add(badge);
            contenido.Children.Add(new TextBlock
            {
                Text = mensaje,
                Foreground = Brushes.White,
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
            btnLeida.Click += async (s, _) =>
            {
                if (_db is null) return;
                try
                {
                    int notifId = (int)((Button)s).Tag;
                    await Task.Run(() => _db.MarcarNotificacionLeida(notifId));
                    if (_ventanaCerrada) return;

                    await CargarNotificacionesEnPopupAsync();
                    await CargarNotificacionesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al marcar notificación: " + ex.Message,
                        TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

        private async void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null) return;
            try
            {
                await Task.Run(() => _db.MarcarNotificacionLeida(null));
                if (_ventanaCerrada) return;

                await CargarNotificacionesEnPopupAsync();
                await CargarNotificacionesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al marcar notificaciones: " + ex.Message,
                    TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Navegación del sidebar

        private void btnHome_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenuPrincipal());

        private void btnInventario_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazInventario.MenúPrincipalInventario());

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Vehículos.MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalOrdenes());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalIngresos());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Devuelve un <see cref="SolidColorBrush"/> congelado para el color hex indicado,
        /// reutilizándolo desde caché en vez de crear una instancia nueva en cada llamada.
        /// </summary>
        private static SolidColorBrush Pincel(string hex)
        {
            if (_cachePinceles.TryGetValue(hex, out var existente))
                return existente;

            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            _cachePinceles[hex] = brush;
            return brush;
        }

        #endregion
    }
}