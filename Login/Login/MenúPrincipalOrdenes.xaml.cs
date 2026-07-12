#nullable enable
using Dasboard_Prueba;
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

namespace Órdenes_de_Trabajo
{
    /// <summary>
    /// Representa una fila de la grilla de órdenes de trabajo, combinando datos
    /// de la orden, el cliente, el vehículo y el producto/servicio asociado.
    /// </summary>
    public class OrdenTrabajo
    {
        public int Orden_ID { get; set; }
        public string Cliente_DNI { get; set; } = string.Empty;
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

    /// <summary>
    /// Ventana principal del módulo de Órdenes de Trabajo. Permite ver, buscar,
    /// filtrar y administrar las órdenes registradas, además de mostrar
    /// notificaciones relacionadas (stock bajo, órdenes finalizadas, etc.).
    /// </summary>
    public partial class MenúPrincipalOrdenes : Window
    {
        #region Constantes y caché estática

        /// <summary>Duración de las transiciones de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        /// <summary>Caché de pinceles ya congelados, para no crear un SolidColorBrush nuevo en cada render.</summary>
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        /// <summary>Título estándar para los cuadros de diálogo de error.</summary>
        private const string TituloError = "Error";

        /// <summary>Valor que representa "sin filtro de estado" en el combo de filtros.</summary>
        private const string FiltroEstadoTodos = "Todos";

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private readonly ObservableCollection<OrdenTrabajo> _listaOrdenes = new();
        private ICollectionView? _vistaOrdenes;

        private string _filtroCliente = string.Empty;
        private string _filtroEstado = FiltroEstadoTodos;

        /// <summary>Evita navegaciones duplicadas si el usuario hace doble clic en el sidebar.</summary>
        private bool _navegando;

        #endregion

        public MenúPrincipalOrdenes()
        {
            InitializeComponent();
            AplicarPermisos();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        #region Ciclo de vida y transición

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
                btnReportes.Visibility = Visibility.Collapsed;
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
            => Navegar(() => new InterfazClientes.MenúPrincipalClientes());

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
                SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        #endregion

        #region Carga de datos

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
                    TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not OrdenTrabajo o) return false;

            if (_filtroEstado != "Finalizado" && o.Estado == "Finalizado") return false;

            string busqueda = txtBuscar.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            if (!string.IsNullOrEmpty(busqueda))
            {
                bool coincide =
                    (o.Cliente_NombreCompleto ?? "").ToLowerInvariant().Contains(busqueda) ||
                    (o.Vehiculo_Placa ?? "").ToLowerInvariant().Contains(busqueda) ||
                    (o.Producto_Nombre ?? "").ToLowerInvariant().Contains(busqueda) ||
                    (o.Estado ?? "").ToLowerInvariant().Contains(busqueda) ||
                    o.Orden_ID.ToString().Contains(busqueda);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroCliente) &&
                !(o.Cliente_NombreCompleto ?? "").ToLowerInvariant().Contains(_filtroCliente))
                return false;

            if (_filtroEstado != FiltroEstadoTodos && o.Estado != _filtroEstado)
                return false;

            return true;
        }

        private void ActualizarContador()
        {
            int total = _vistaOrdenes?.Cast<object>().Count() ?? 0;
            tbTotalOrdenes.Text = $"{total} orden{(total != 1 ? "es" : "")}";
        }

        #endregion

        #region Búsqueda y filtros

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroCliente = txtFiltroCliente.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            _filtroEstado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? FiltroEstadoTodos;
            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroCliente.Clear();
            cmbFiltroEstado.SelectedIndex = 0;
            _filtroCliente = string.Empty;
            _filtroEstado = FiltroEstadoTodos;
            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        #endregion

        #region DataGrid

        private async void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is not OrdenTrabajo seleccionada) return;
            dgOrdenes.SelectedItem = null;

            var ventana = new OrdenWindow();
            ventana.Closed += (_, _) =>
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

        #endregion

        #region Notificaciones

        /// <summary>Actualiza el badge de notificaciones pendientes en el header (contador rápido).</summary>
        public void CargarNotificaciones()
        {
            try
            {
                int cantidad = _db.ContarNotificacionesPendientes();
                badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad > 99 ? "99+" : cantidad.ToString();
            }
            catch (Exception ex)
            {
                // Se ignora intencionalmente: un fallo al refrescar el contador de
                // notificaciones no debe interrumpir el uso normal de la ventana.
                System.Diagnostics.Debug.WriteLine("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        /// <summary>Carga el detalle completo de notificaciones pendientes dentro del popup.</summary>
        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                DataTable dt = _db.ObtenerNotificacionesPendientes();

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
            btnLeida.Click += (s, _) =>
            {
                try
                {
                    int notifId = (int)((Button)s).Tag;
                    _db.MarcarNotificacionLeida(notifId);
                    CargarNotificacionesEnPopup();
                    CargarNotificaciones();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al marcar como leída: " + ex.Message,
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

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _db.MarcarNotificacionLeida(null);
                CargarNotificacionesEnPopup();
                CargarNotificaciones();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al marcar notificaciones: " + ex.Message,
                    TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
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