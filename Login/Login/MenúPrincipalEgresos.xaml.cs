using Dasboard_Prueba;
using Login;
using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Vehículos;

namespace Contabilidad
{
    public partial class MenúPrincipalEgresos : Window
    {
        private readonly clsConsultasBD _db = new clsConsultasBD();
        private DataTable _gastosCache;
        private readonly DispatcherTimer _debounceBusqueda;

        public MenúPrincipalEgresos()
        {
            InitializeComponent();

            _debounceBusqueda = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            _debounceBusqueda.Tick += (s, e) =>
            {
                _debounceBusqueda.Stop();
                AplicarFiltro(txtBuscar.Text.Trim());
            };

            Loaded += async (s, e) =>
            {
                await CargarEgresoAsync();
                CargarNotificaciones();
            };
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        // ════════════════════════════════════════════════════════════
        // GASTOS
        // ════════════════════════════════════════════════════════════

        private async Task CargarEgresoAsync()
        {
            try
            {
                _gastosCache = await Task.Run(() => _db.ObtenerGastos(null));
                dgGastos.ItemsSource = _gastosCache.DefaultView;
                ActualizarContador(_gastosCache.Rows.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gastos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void CargarEgreso(string busqueda = null)
        {
            await CargarEgresoAsync();
            if (!string.IsNullOrWhiteSpace(busqueda))
                AplicarFiltro(busqueda);
        }

        private void AplicarFiltro(string texto)
        {
            if (_gastosCache == null) return;
            try
            {
                _gastosCache.DefaultView.RowFilter = string.IsNullOrWhiteSpace(texto)
                    ? string.Empty
                    : $"Nombre_Gasto LIKE '%{texto.Replace("'", "''")}%' OR Tipo_Gasto LIKE '%{texto.Replace("'", "''")}%'";

                ActualizarContador(_gastosCache.DefaultView.Count);
            }
            catch
            {
                _gastosCache.DefaultView.RowFilter = string.Empty;
            }
        }

        private void ActualizarContador(int cantidad)
        {
            tbTotalGastos.Text = cantidad == 1 ? "1 registro" : $"{cantidad} registros";
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceBusqueda.Stop();
            _debounceBusqueda.Start();
        }

        // ── Botón Agregar ────────────────────────────────────────────
        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            new GestiónEgresos(this).ShowDialog();
        }

        // ── Doble clic DataGrid ──────────────────────────────────────
        private void dgGastos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgGastos.SelectedItem is not DataRowView fila) return;

            new GestiónEgresos(
                this,
                gastoId: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString(),
                nombre: fila["Nombre_Gasto"].ToString(),
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto")
                               && fila["Observaciones_Gasto"] != DBNull.Value
                               ? fila["Observaciones_Gasto"].ToString() : ""
            ).ShowDialog();

            dgGastos.SelectedItem = null;
            CargarEgreso();
        }

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
                tipo: fila["Tipo_Gasto"].ToString(),
                nombre: fila["Nombre_Gasto"].ToString(),
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto")
                               && fila["Observaciones_Gasto"] != DBNull.Value
                               ? fila["Observaciones_Gasto"].ToString() : ""
            )
            { Owner = this }.ShowDialog();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            new ReportesWindow("Egresos").ShowDialog();
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
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
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
                        Foreground = new SolidColorBrush(Colors.White),
                        Padding = new Thickness(0)
                    });
                    vacio.Children.Add(new TextBlock
                    {
                        Text = "Sin notificaciones pendientes",
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 8, 0, 0)
                    });
                    panelNotificaciones.Children.Add(vacio);
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
                        row["Tipo_Notificacion"].ToString(),
                        row["Mensaje"].ToString()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string colorIcono = esStock ? "#F0A500" : "#3D7EFF";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            var card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorFondo)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorBorde)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contenido = new StackPanel();
            var badgeTipo = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorBorde + "33")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 5)
            };
            badgeTipo.Child = new TextBlock
            {
                Text = labelTipo,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorIcono)),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            contenido.Children.Add(badgeTipo);
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
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = Cursors.Hand,
                ToolTip = "Marcar como leída",
                Tag = id
            };
            btnLeida.Click += (s, e) =>
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
            => Navegar(() => new Contabilidad.MenúPrincipalIngresos());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Navegar(() => new Login.MainWindow());
        }
    }
}