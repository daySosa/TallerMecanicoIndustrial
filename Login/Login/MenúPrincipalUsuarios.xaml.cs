#nullable enable
using Dasboard_Prueba;
using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana principal del módulo de Usuarios. Permite ver, buscar, filtrar
    /// y administrar los usuarios del sistema, además de gestionar el registro
    /// biométrico y las notificaciones pendientes (stock bajo, órdenes finalizadas, etc.).
    /// </summary>
    public partial class MenúPrincipalUsuarios : Window
    {
        #region Constantes y caché estática

        /// <summary>Duración de las transiciones de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        /// <summary>Caché de pinceles ya congelados, para no crear un SolidColorBrush nuevo en cada render.</summary>
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        /// <summary>Título estándar para los cuadros de diálogo de error.</summary>
        private const string TituloError = "Error";

        #endregion

        #region Estado interno

        // La conexión se abre de forma asíncrona en el Loaded para no bloquear el hilo
        // de UI justo cuando Navegar<T> está creando esta ventana (antes de Show() y
        // del fade-in). Así la navegación se siente instantánea.
        private RepositorioSql? _db;
        private DataTable? _usuariosCache;
        private readonly DispatcherTimer _debounceBusqueda;
        private readonly CancellationTokenSource _cts = new();

        private bool _navegando;

        /// <summary>
        /// Indica si la ventana ya fue cerrada. Se usa para evitar tocar controles
        /// de UI desde continuaciones async que terminan después del cierre.
        /// </summary>
        private volatile bool _ventanaCerrada;

        #endregion

        public MenúPrincipalUsuarios()
        {
            InitializeComponent();

            AplicarPermisos();
            CargarInfoUsuario();
            _debounceBusqueda = ConfigurarDebounce();

            Loaded += MenúPrincipalUsuarios_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida

        private async void MenúPrincipalUsuarios_Loaded(object sender, RoutedEventArgs e)
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

            await CargarUsuariosAsync();
            await CargarNotificacionesAsync();
        }

        /// <summary>Libera los recursos propios (token de cancelación, repositorio, timer) al cerrar la ventana.</summary>
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
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                AplicarFiltroBusqueda();
            };
            return timer;
        }

        #endregion

        #region Transición de entrada/salida

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
        /// Navega a otra ventana con un crossfade real: la ventana nueva se crea y
        /// se muestra de inmediato (con su propio fade-in), mientras esta ventana
        /// hace fade-out en paralelo y recién se cierra al terminar su animación.
        /// Evita navegaciones duplicadas si el usuario hace doble clic en el sidebar.
        /// </summary>
        /// <typeparam name="T">Tipo de la ventana destino.</typeparam>
        /// <param name="crear">Función que construye la ventana destino.</param>
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

        /// <summary>Oculta las opciones exclusivas de administrador si el usuario no lo es.</summary>
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

        /// <summary>
        /// Carga en el sidebar el nombre completo, el rol y las iniciales
        /// del usuario con la sesión activa, tomados de <see cref="SesionActual"/>.
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

        #region Carga de usuarios

        private async Task CargarUsuariosAsync()
        {
            if (_db is null) return;
            try
            {
                DataTable datos = await Task.Run(() => _db.ObtenerUsuarios(), _cts.Token);
                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                _usuariosCache = datos;
                dgUsuarios.ItemsSource = _usuariosCache.DefaultView;
                ActualizarContador(_usuariosCache.Rows.Count);
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al cargar usuarios: " + ex.Message,
                        TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Punto de entrada público y seguro para recargar la grilla desde eventos síncronos.</summary>
        public void CargarUsuarios() => _ = RecargarUsuariosSeguroAsync();

        private async Task RecargarUsuariosSeguroAsync()
        {
            try
            {
                await CargarUsuariosAsync();
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al recargar usuarios: " + ex.Message,
                        TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActualizarContador(int cantidad)
            => tbTotalUsuarios.Text = cantidad == 1 ? "1 usuario" : $"{cantidad} usuarios";

        #endregion

        #region Búsqueda y filtros

        // ── Búsqueda con debounce para no filtrar en cada tecla ──────
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_usuariosCache == null) return;
            _debounceBusqueda.Stop();
            _debounceBusqueda.Start();
        }

        private void AplicarFiltroBusqueda()
        {
            if (_usuariosCache == null) return;

            string texto = EscaparParaFiltro(txtBuscar.Text.Trim());
            _usuariosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : $"Usuario_Nombre LIKE '%{texto}%' OR Usuario_Apellido LIKE '%{texto}%' " +
                  $"OR Usuario_Email LIKE '%{texto}%'";
            ActualizarContador(_usuariosCache.DefaultView.Count);
        }

        /// <summary>Escapa comillas y caracteres especiales de LIKE (% * [ ]) usados por DataView.RowFilter.</summary>
        private static string EscaparParaFiltro(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            return texto
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (_usuariosCache == null) return;

            var filtros = new List<string>();

            string nombre = EscaparParaFiltro(txtFiltroNombre.Text.Trim());
            if (!string.IsNullOrWhiteSpace(nombre))
                filtros.Add($"(Usuario_Nombre LIKE '%{nombre}%' OR Usuario_Apellido LIKE '%{nombre}%')");

            string? rol = (cmbFiltroRol.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
                filtros.Add($"Usuario_Rol = '{EscaparParaFiltro(rol)}'");

            string? estado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                filtros.Add($"Usuario_Activo = {(estado == "Activo" ? "True" : "False")}");

            _usuariosCache.DefaultView.RowFilter = filtros.Count > 0
                ? string.Join(" AND ", filtros)
                : string.Empty;

            ActualizarContador(_usuariosCache.DefaultView.Count);
            popupFiltros.IsOpen = false;
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroNombre.Clear();
            cmbFiltroRol.SelectedIndex = 0;
            cmbFiltroEstado.SelectedIndex = 0;

            if (_usuariosCache != null)
            {
                _usuariosCache.DefaultView.RowFilter = string.Empty;
                ActualizarContador(_usuariosCache.Rows.Count);
            }

            popupFiltros.IsOpen = false;
        }

        #endregion

        #region DataGrid: agregar / editar / biometría

        private void dgUsuarios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgUsuarios.SelectedItem is not DataRowView fila) return;

            object valor = fila["Usuario_Email"];
            if (valor == null || valor == DBNull.Value) return;

            string email = valor.ToString()!;
            new VentanaUsuario(email).ShowDialog();
            CargarUsuarios();
        }

        private void btnAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            new VentanaUsuario().ShowDialog();
            CargarUsuarios();
        }

        private void btnBiometria_Click(object sender, RoutedEventArgs e)
        {
            new VentanaBiometria().ShowDialog();
            CargarUsuarios();
        }

        #endregion

        #region Notificaciones

        /// <summary>Actualiza el badge de notificaciones pendientes en el header (contador rápido).</summary>
        private async Task CargarNotificacionesAsync()
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

        /// <summary>Carga el detalle completo de notificaciones pendientes dentro del popup.</summary>
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
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString() ?? string.Empty;
                    string msg = row["Mensaje"].ToString() ?? string.Empty;
                    panelNotificaciones.Children.Add(CrearTarjetaNotificacion(id, tipo, msg));
                }
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

        /// <summary>Construye la tarjeta visual de una notificación individual dentro del popup.</summary>
        private Border CrearTarjetaNotificacion(int id, string tipo, string mensaje)
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
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalOrdenes());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalEgresos());

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalIngresos());

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