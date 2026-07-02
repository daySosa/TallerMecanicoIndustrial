using Dasboard_Prueba;
using InterfazClientes;
using InterfazInventario;
using Login;
using Login.Clases;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vehículos;

namespace Contabilidad
{
    public partial class MenúPrincipalIngresos : Window
    {
        private readonly clsConsultasBD _db = new clsConsultasBD();

        public MenúPrincipalIngresos()
        {
            InitializeComponent();
            AplicarPermisos();
            CargarPago();
            CargarNotificaciones();
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
        // PAGOS
        // ════════════════════════════════════════════════════════════

        public void CargarPago(string busqueda = null)
        {
            try
            {
                var dt = _db.ObtenerPagos(busqueda);
                dgPagos.ItemsSource = dt.DefaultView;
                tbTotalPagos.Text = $"{dt.Rows.Count} registros";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar pagos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarPago(string.IsNullOrEmpty(texto) ? null : texto);
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new GestiónIngresos(this);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void dgPagos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgPagos.SelectedItem is not DataRowView fila) return;

            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            string dniStr = fila["Cliente_DNI"].ToString();
            int ordenId = Convert.ToInt32(fila["Orden_ID"]);
            decimal monto = Convert.ToDecimal(fila["Precio_Pago"]);
            DateTime fecha = Convert.ToDateTime(fila["Fecha_Pago"]);

            var ventana = new GestiónIngresos(this, pagoId, dniStr, ordenId, monto, fecha);
            ventana.Owner = this;
            ventana.ShowDialog();
            CargarPago();
        }

        private void btnMostrarComprobantes_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidacionesGenerales.ValidarComboSeleccionado(
                    dgPagos.SelectedItem, "pago para ver el comprobante")) return;

            var fila = (DataRowView)dgPagos.SelectedItem;
            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            new ComprobantePago(pagoId) { Owner = this }.ShowDialog();
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
            => new ReportesWindow("Ingresos").ShowDialog();

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
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                    return;
                }

                txtContadorPopup.Text = dt.Rows.Count > 99 ? "99+" : dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString() ?? "";
                    string msg = row["Mensaje"].ToString() ?? "";
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

        // ════════════════════════════════════════════════════════════
        // NAVEGACIÓN — nombres exactos según el XAML
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
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalOrdenes());

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Contabilidad.MenúPrincipalEgresos());

        private void btnUsuarios_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new InterfazClientes.MenúPrincipalUsuarios());

        private void btnBitacora_Click(object sender, RoutedEventArgs e)
            => Navegar(() => new Órdenes_de_Trabajo.MenúPrincipalBitacora());

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Login.Clases.SesionActual.CerrarSesion();
                Navegar(() => new Login.MainWindow());
            }
        }

        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}