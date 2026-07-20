#nullable enable
using Dasboard_Prueba;
using Login;
using Login.Clases;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Vehículos;

namespace Contabilidad
{
    /// <summary>
    /// Ventana principal del módulo de Egresos. Muestra el registro de gastos
    /// del taller, permite buscarlos con filtro en memoria (rápido, sin ir a la
    /// BD por cada tecla), agregarlos, ver su comprobante y generar reportes,
    /// además de mostrar notificaciones pendientes del sistema.
    /// </summary>
    public partial class MenúPrincipalEgresos : Window
    {
        #region Constantes y caché estática

        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        private const string TituloError = "Error";

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private readonly CancellationTokenSource _cts = new();

        /// <summary>
        /// Copia local de los gastos ya cargados. El filtro de búsqueda se aplica
        /// sobre esta tabla en memoria (RowFilter), sin volver a consultar la BD
        /// en cada tecla — mucho más rápido que un debounce con query remota.
        /// </summary>
        private DataTable? _gastosCache;

        /// <summary>Debounce para no re-filtrar en cada tecla mientras se escribe en el buscador.</summary>
        private readonly DispatcherTimer _debounceBusqueda;

        private bool _navegando;
        private bool _cerrandoConAnimacion;
        private volatile bool _ventanaCerrada;

        #endregion

        public MenúPrincipalEgresos()
        {
            InitializeComponent();

            AplicarPermisos();
            CargarInfoUsuario();
            _debounceBusqueda = ConfigurarDebounce();

            Loaded += MenúPrincipalEgresos_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida y transición de entrada/salida

        private async void MenúPrincipalEgresos_Loaded(object sender, RoutedEventArgs e)
        {
            await CargarEgresoAsync();
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

        private void LiberarRecursos()
        {
            try
            {
                _debounceBusqueda.Stop();
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
                AplicarFiltro(txtBuscar.Text.Trim());
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

        #region Gastos

        /// <summary>Consulta todos los gastos en un hilo de fondo y refresca el DataGrid.</summary>
        private async Task CargarEgresoAsync()
        {
            try
            {
                DataTable dt = await Task.Run(() => _db.ObtenerGastos(null), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                _gastosCache = dt;
                dgGastos.ItemsSource = _gastosCache.DefaultView;
                ActualizarContador(_gastosCache.Rows.Count);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al cargar gastos: " + ex.Message, TituloError,
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Recarga los gastos desde la BD y, opcionalmente, aplica un filtro de
        /// búsqueda inmediatamente después. Pensado para invocarse desde fuera
        /// de la ventana (p.ej. al cerrar GestiónEgresos).
        /// </summary>
        public async void CargarEgreso(string? busqueda = null)
        {
            await CargarEgresoAsync();
            if (_ventanaCerrada) return;

            if (!string.IsNullOrWhiteSpace(busqueda))
                AplicarFiltro(busqueda);
        }

        /// <summary>Filtra la tabla en memoria por nombre o tipo de gasto, sin tocar la BD.</summary>
        private void AplicarFiltro(string texto)
        {
            if (_gastosCache == null) return;
            try
            {
                _gastosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                    ? string.Empty
                    : $"Nombre_Gasto LIKE '%{EscaparComillas(texto)}%' OR Tipo_Gasto LIKE '%{EscaparComillas(texto)}%'";

                ActualizarContador(_gastosCache.DefaultView.Count);
            }
            catch
            {
                _gastosCache.DefaultView.RowFilter = string.Empty;
            }
        }

        private static string EscaparComillas(string texto) => texto.Replace("'", "''");

        private void ActualizarContador(int cantidad)
            => tbTotalGastos.Text = cantidad == 1 ? "1 registro" : $"{cantidad} registros";

        // El buscador usa debounce: cada tecla reinicia el temporizador y solo se
        // aplica el filtro 250ms después de que el usuario deja de escribir.
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceBusqueda.Stop();
            _debounceBusqueda.Start();
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
            => new GestiónEgresos(this) { Owner = this }.ShowDialog();

        private void dgGastos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgGastos.SelectedItem is not DataRowView fila) return;

            var ventana = new GestiónEgresos(
                this,
                gastoId: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString() ?? string.Empty,
                nombre: fila["Nombre_Gasto"].ToString() ?? string.Empty,
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: ObtenerObservaciones(fila))
            {
                Owner = this
            };
            ventana.ShowDialog();

            dgGastos.SelectedItem = null;
            CargarEgreso();
        }

        private static string ObtenerObservaciones(DataRowView fila)
            => fila.Row.Table.Columns.Contains("Observaciones_Gasto") &&
               fila["Observaciones_Gasto"] != DBNull.Value
                ? fila["Observaciones_Gasto"].ToString() ?? string.Empty
                : string.Empty;

        private void btnMostrarComprobante_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.SelectedItem is not DataRowView fila)
            {
                MessageBox.Show("Selecciona un gasto para ver el comprobante.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            new ComprobanteEgresos(
                id: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString() ?? string.Empty,
                nombre: fila["Nombre_Gasto"].ToString() ?? string.Empty,
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: ObtenerObservaciones(fila))
            { Owner = this }.ShowDialog();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Egresos").ShowDialog();

        #endregion

        #region Notificaciones

        public async Task CargarNotificacionesAsync()
        {
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
            var vacio = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 20)
            };
            vacio.Children.Add(new TextBlock
            {
                Text = "🎉",
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White
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
            => Navegar(() => new MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalOrdenes());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

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

        /// <summary>Devuelve un pincel cacheado y congelado para el color hex indicado.</summary>
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