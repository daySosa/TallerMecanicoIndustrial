using Contabilidad;
using Dasboard_Prueba;
using InterfazClientes;
using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public class BitacoraItem
    {
        public DateTime Bitacora_Fecha { get; set; }
        public string Bitacora_Usuario { get; set; } = string.Empty;
        public string Bitacora_Rol { get; set; } = string.Empty;
        public string Bitacora_Modulo { get; set; } = string.Empty;
        public string Bitacora_Accion { get; set; } = string.Empty;
        public string Bitacora_Descripcion { get; set; } = string.Empty;
    }

    public partial class MenúPrincipalBitacora : Window
    {
        private readonly RepositorioSql _db = new();
        private List<BitacoraItem> _listaCompleta = new();

        // ── CONSTRUCTOR ──────────────────────────────────────────────

        public MenúPrincipalBitacora()
        {
            InitializeComponent();
            AplicarPermisos();
            CargarBitacora();
            CargarNotificaciones();
        }

        // ── MOVER VENTANA ────────────────────────────────────────────

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
            if (!Login.Clases.SesionActual.EsAdministrador)
            {
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnBitacora.Visibility = Visibility.Collapsed;
                expanderContabilidad.Visibility = Visibility.Collapsed;
            }
        }

        // ── DATOS ────────────────────────────────────────────────────

        private void CargarBitacora()
        {
            try
            {
                _listaCompleta = _db.ObtenerBitacora();
                AplicarFiltros();
                ActualizarEstadisticas(_listaCompleta);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar bitácora:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltros()
        {
            string usuario = txtFiltroUsuario.Text.Trim().ToLower();
            string modulo = (cmbFiltroModulo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            DateTime? desde = dtpDesde.SelectedDate;
            DateTime? hasta = dtpHasta.SelectedDate?.AddDays(1);
            string buscar = txtBuscar.Text.Trim().ToLower();

            var filtrada = _listaCompleta.Where(b =>
            {
                if (!string.IsNullOrEmpty(usuario) &&
                    !b.Bitacora_Usuario.ToLower().Contains(usuario)) return false;

                if (modulo != "Todos" &&
                    !b.Bitacora_Modulo.Equals(modulo, StringComparison.OrdinalIgnoreCase)) return false;

                if (desde.HasValue && b.Bitacora_Fecha < desde.Value) return false;
                if (hasta.HasValue && b.Bitacora_Fecha > hasta.Value) return false;

                if (!string.IsNullOrEmpty(buscar) &&
                    !b.Bitacora_Usuario.ToLower().Contains(buscar) &&
                    !b.Bitacora_Modulo.ToLower().Contains(buscar) &&
                    !b.Bitacora_Accion.ToLower().Contains(buscar) &&
                    !b.Bitacora_Descripcion.ToLower().Contains(buscar)) return false;

                return true;
            }).ToList();

            dgBitacora.ItemsSource = filtrada;
            tbTotalEventos.Text = $"{filtrada.Count} evento{(filtrada.Count != 1 ? "s" : "")}";
        }

        private void ActualizarEstadisticas(List<BitacoraItem> lista)
        {
            tbTotalRegistros.Text = lista.Count.ToString();

            tbRegistrosHoy.Text = lista
                .Count(b => b.Bitacora_Fecha.Date == DateTime.Today).ToString();

            tbUsuariosActivos.Text = lista
                .Select(b => b.Bitacora_Usuario)
                .Distinct().Count().ToString();

            string moduloActivo = lista
                .GroupBy(b => b.Bitacora_Modulo)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? "—";
            tbModuloActivo.Text = moduloActivo;
        }

        // ── BÚSQUEDA Y FILTROS ───────────────────────────────────────

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
            => AplicarFiltros();

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
            => AplicarFiltros();

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroUsuario.Clear();
            cmbFiltroModulo.SelectedIndex = 0;
            dtpDesde.SelectedDate = null;
            dtpHasta.SelectedDate = null;
            txtBuscar.Clear();
            AplicarFiltros();
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
                badgeNotificaciones.Visibility = cantidad > 0
                    ? Visibility.Visible : Visibility.Collapsed;
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
            catch (Exception ex)
            {
                MessageBox.Show("Error notificaciones: " + ex.Message);
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
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
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
                Login.Clases.SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        // ── HELPER ───────────────────────────────────────────────────

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}