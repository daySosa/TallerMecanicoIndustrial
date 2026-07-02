using Contabilidad;
using Dasboard_Prueba;
using InterfazInventario;
using Login.Clases;
using Órdenes_de_Trabajo;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vehículos;

namespace InterfazClientes
{
    public partial class MenúPrincipalUsuarios : Window
    {
        private readonly RepositorioSql _db = new RepositorioSql();
        private DataTable _usuariosCache;

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ════════════════════════════════════════════════════════════

        public MenúPrincipalUsuarios()
        {
            InitializeComponent();
            AplicarPermisos();

            Loaded += async (s, e) =>
            {
                await CargarUsuariosAsync();
                CargarNotificaciones();
            };

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
            if (!Login.Clases.SesionActual.EsAdministrador)
            {
                btnUsuarios.Visibility = Visibility.Collapsed;
                btnBitacora.Visibility = Visibility.Collapsed;
                expanderContabilidad.Visibility = Visibility.Collapsed;
            }
        }

        // ════════════════════════════════════════════════════════════
        // USUARIOS
        // ════════════════════════════════════════════════════════════

        private async Task CargarUsuariosAsync()
        {
            try
            {
                _usuariosCache = await Task.Run(() => _db.ObtenerUsuarios());
                dgUsuarios.ItemsSource = _usuariosCache.DefaultView;
                ActualizarContador(_usuariosCache.Rows.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void CargarUsuarios()
            => await CargarUsuariosAsync();

        private void ActualizarContador(int cantidad)
            => tbTotalUsuarios.Text = cantidad == 1 ? "1 usuario" : $"{cantidad} usuarios";

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_usuariosCache == null) return;
            string texto = txtBuscar.Text.Trim().Replace("'", "''");
            _usuariosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                ? string.Empty
                : $"Usuario_Nombre LIKE '%{texto}%' OR Usuario_Apellido LIKE '%{texto}%' " +
                  $"OR Usuario_Email LIKE '%{texto}%'";
            ActualizarContador(_usuariosCache.DefaultView.Count);
        }

        // ── Doble clic en una fila = editar ese usuario ──────────────
        private void dgUsuarios_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgUsuarios.SelectedItem is DataRowView fila)
            {
                string email = fila["Usuario_Email"].ToString();
                new VentanaUsuario(email).ShowDialog();
                CargarUsuarios();
            }
        }

        private void dgUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ════════════════════════════════════════════════════════════
        // BOTONES PRINCIPALES
        // ════════════════════════════════════════════════════════════

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

        // ════════════════════════════════════════════════════════════
        // FILTROS
        // ════════════════════════════════════════════════════════════

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
            => popupFiltros.IsOpen = !popupFiltros.IsOpen;

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            if (_usuariosCache == null) return;

            var filtros = new List<string>();

            string nombre = txtFiltroNombre.Text.Trim().Replace("'", "''");
            if (!string.IsNullOrWhiteSpace(nombre))
                filtros.Add($"(Usuario_Nombre LIKE '%{nombre}%' OR Usuario_Apellido LIKE '%{nombre}%')");

            string rol = (cmbFiltroRol.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(rol) && rol != "Todos")
                filtros.Add($"Usuario_Rol = '{rol}'");

            string estado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString();
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

        // ════════════════════════════════════════════════════════════
        // NOTIFICACIONES
        // ════════════════════════════════════════════════════════════

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

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
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
                    panelNotificaciones.Children.Add(CrearTarjetaNotificacion(
                        Convert.ToInt32(row["Notificacion_ID"]),
                        row["Tipo_Notificacion"].ToString(),
                        row["Mensaje"].ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error notificaciones: " + ex.Message);
            }
        }

        private Border CrearTarjetaNotificacion(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            var card = new Border
            {
                Background = Pincel(colorFondo),
                BorderBrush = Pincel(colorBorde),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
            Grid.SetColumn(contenido, 0);
            grid.Children.Add(contenido);

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
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(btnLeida);

            card.Child = grid;
            return card;
        }

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            _db.MarcarNotificacionLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }

        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN
        // ════════════════════════════════════════════════════════════

        private void Navegar<T>(Func<T> crear) where T : Window
        {
            crear().Show();
            this.Close();
        }

        private void btnHome_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenuPrincipal());

        private void btnInventario_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalInventario());

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalVehículos());

        private void btnClientes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalClientes());

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new MenúPrincipalOrdenes());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

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

        // ════════════════════════════════════════════════════════════
        // HELPER
        // ════════════════════════════════════════════════════════════

        private static SolidColorBrush Pincel(string hex)
            => new((Color)ColorConverter.ConvertFromString(hex));
    }
}