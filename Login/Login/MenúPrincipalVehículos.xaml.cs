#nullable enable
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
using System.Windows.Media.Animation;

namespace Vehículos
{
    /// <summary>
    /// Ventana principal del módulo de Vehículos. Permite ver, buscar, filtrar
    /// y administrar los vehículos registrados, además de mostrar notificaciones
    /// relacionadas (stock bajo, órdenes finalizadas, etc.).
    /// </summary>
    public partial class MenúPrincipalVehículos : Window
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

        private RepositorioSql? _db;
        private readonly ObservableCollection<Vehiculo> _listaVehiculos = new();
        private readonly CancellationTokenSource _cts = new();

        private ICollectionView? _vistaVehiculos;
        private string _filtroPlaca = string.Empty;
        private string _filtroModelo = string.Empty;

        private bool _cargandoDatos;
        private bool _navegando;

        /// <summary>
        /// Indica si la ventana ya fue cerrada. Se usa para evitar tocar controles
        /// de UI desde continuaciones async que terminan después del cierre.
        /// </summary>
        private volatile bool _ventanaCerrada;

        #endregion

        public MenúPrincipalVehículos()
        {
            InitializeComponent();

            AplicarPermisos();
            CargarInfoUsuario();

            Loaded += MenúPrincipalVehículos_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida

        private async void MenúPrincipalVehículos_Loaded(object sender, RoutedEventArgs e)
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

            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        /// <summary>Libera los recursos propios (token de cancelación, repositorio) al cerrar la ventana.</summary>
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
                btnReportes.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Info del usuario logueado

        /// <summary>
        /// Carga en el sidebar el nombre completo (nombre + apellido) y el rol
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

                string rol;
                if (!string.IsNullOrWhiteSpace(SesionActual.Rol))
                    rol = SesionActual.Rol.Trim();
                else
                    rol = SesionActual.EsAdministrador ? "Administrador" : "Empleado";

                txtNombreUsuario.Text = nombreCompleto;
                txtRolUsuario.Text = rol;
            }
            catch (Exception ex)
            {
                txtNombreUsuario.Text = "Usuario";
                txtRolUsuario.Text = "—";
                System.Diagnostics.Debug.WriteLine("Error al cargar info del usuario: " + ex.Message);
            }
        }

        #endregion

        #region Navegación del sidebar

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
            {
                SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        #endregion

        #region Carga de datos de vehículos

        /// <summary>
        /// Carga los vehículos desde la base de datos en un hilo secundario y
        /// actualiza la grilla sin bloquear la interfaz. Protegida contra
        /// ejecuciones simultáneas con <see cref="_cargandoDatos"/>.
        /// </summary>
        private async Task CargarDatosAsync()
        {
            if (_db is null) return;
            if (_cargandoDatos) return;
            _cargandoDatos = true;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                List<Vehiculo> vehiculos = await Task.Run(
                    () => _db.ObtenerVehiculos().ToList(), _cts.Token);

                if (_cts.IsCancellationRequested || _ventanaCerrada) return;

                _listaVehiculos.Clear();
                foreach (var v in vehiculos)
                    _listaVehiculos.Add(v);

                if (_vistaVehiculos == null)
                {
                    _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                    _vistaVehiculos.Filter = AplicarFiltros;
                    dgVehiculos.ItemsSource = _vistaVehiculos;
                }
                else
                {
                    _vistaVehiculos.Refresh();
                }

                ActualizarContador();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    MessageBox.Show("Error al cargar vehículos:\n" + ex.Message,
                        TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                _cargandoDatos = false;
            }
        }

        /// <summary>Predicado usado por la vista de colección para aplicar búsqueda y filtros combinados.</summary>
        private bool AplicarFiltros(object item)
        {
            if (item is not Vehiculo v) return false;

            string texto = txtBuscar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (v.Vehiculo_Placa ?? "").ToLowerInvariant().Contains(texto) ||
                    (v.Vehiculo_Marca ?? "").ToLowerInvariant().Contains(texto) ||
                    (v.Vehiculo_Modelo ?? "").ToLowerInvariant().Contains(texto) ||
                    (v.Vehiculo_Tipo ?? "").ToLowerInvariant().Contains(texto) ||
                    (v.Cliente_NombreCompleto ?? "").ToLowerInvariant().Contains(texto);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroPlaca) &&
                !(v.Vehiculo_Placa ?? "").ToLowerInvariant().Contains(_filtroPlaca)) return false;

            if (!string.IsNullOrEmpty(_filtroModelo) &&
                !(v.Vehiculo_Modelo ?? "").ToLowerInvariant().Contains(_filtroModelo)) return false;

            return true;
        }

        private void ActualizarContador()
        {
            int total = _vistaVehiculos?.Cast<object>().Count() ?? 0;
            tbTotalVehiculos.Text = $"{total} vehículo{(total != 1 ? "s" : "")}";
        }

        #endregion

        #region Búsqueda y filtros

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroPlaca = txtFiltroPlaca.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            _filtroModelo = txtFiltroModelo.Text?.Trim().ToLowerInvariant() ?? string.Empty;
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

        #endregion

        #region DataGrid: agregar / editar / reportes

        private async void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVehiculos.SelectedItem is not Vehiculo seleccionado) return;

            var ventana = new VehiWindow();
            ventana.CargarVehiculoParaEditar(seleccionado);
            ventana.ShowDialog();

            dgVehiculos.SelectedItem = null;
            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        private async void BtnNuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            new VehiWindow().ShowDialog();
            await CargarDatosAsync();
            await CargarNotificacionesAsync();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Vehiculos").ShowDialog();

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
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message,
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
            vacio.Children.Add(new Label
            {
                Content = "🎉",
                FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
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
        }

        /// <summary>Construye la tarjeta visual de una notificación individual dentro del popup.</summary>
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
                    MessageBox.Show("Error al marcar como leída: " + ex.Message);
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
                MessageBox.Show("Error al marcar notificaciones: " + ex.Message);
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