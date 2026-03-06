using Contabilidad;
using InterfazClientes;
using InterfazInventario;
using LiveCharts;
using Login;
using Login.Clases;
using Órdenes_de_Trabajo;
using System.Data.SqlClient;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vehículos;

namespace Dasboard_Prueba
{

    public class OrdenReciente
    {
        public int Orden_ID { get; set; }
        public string Cliente_NombreCompleto { get; set; }
        public string Vehiculo_Placa { get; set; }
        public DateTime Fecha { get; set; }
        public string Estado { get; set; }
        public decimal OrdenPrecio_Total { get; set; }
    }


    public class NotificacionItem
    {
        public int Notificacion_ID { get; set; }
        public string Tipo_Notificacion { get; set; }
        public string Mensaje { get; set; }
        public bool Leida { get; set; }
    }

    public partial class MenuPrincipal : Window
    {
        private clsConexion _conexion = new clsConexion();
        private DateTime _mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        private List<NotificacionItem> _notificaciones = new List<NotificacionItem>();

        public ChartValues<double> BalanceValues { get; set; }
        public ChartValues<double> OrderValues { get; set; }
        public ChartValues<double> GastosValues { get; set; }
        public ChartValues<double> IngresosSemanalValues { get; set; }
        public string[] IngresosSemanalLabels { get; set; }


        public MenuPrincipal()
        {
            BalanceValues = new ChartValues<double> { 30, 45, 28, 60, 40, 55, 50 };
            OrderValues = new ChartValues<double> { 5, 8, 3, 10, 7, 9, 6 };
            GastosValues = new ChartValues<double> { 20, 35, 15, 45, 30, 40, 25 };
            IngresosSemanalValues = new ChartValues<double> { 45, 60, 35, 50, 40, 55 };
            IngresosSemanalLabels = new[] { "Ene", "Feb", "Mar", "Abr", "May", "Jun" };

            DataContext = this;
            InitializeComponent();
            CargarDatos();
            CargarNotificaciones();
            GenerarCalendario();
        }

        private void CargarDatos()
        {
            try
            {
                _conexion.Abrir();

                string sqlOrdenes = @"
                    SELECT TOP 10
                        o.Orden_ID,
                        c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS Cliente_NombreCompleto,
                        o.Vehiculo_Placa,
                        o.Fecha,
                        o.Estado,
                        o.OrdenPrecio_Total
                    FROM Orden_Trabajo o
                    INNER JOIN Cliente c ON o.Cliente_DNI = c.Cliente_DNI
                    ORDER BY o.Fecha DESC";

                var ordenes = new List<OrdenReciente>();
                using (SqlCommand cmd = new SqlCommand(sqlOrdenes, _conexion.SqlC))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        ordenes.Add(new OrdenReciente
                        {
                            Orden_ID = Convert.ToInt32(rd["Orden_ID"]),
                            Cliente_NombreCompleto = rd["Cliente_NombreCompleto"].ToString(),
                            Vehiculo_Placa = rd["Vehiculo_Placa"].ToString(),
                            Fecha = Convert.ToDateTime(rd["Fecha"]),
                            Estado = rd["Estado"].ToString(),
                            OrdenPrecio_Total = Convert.ToDecimal(rd["OrdenPrecio_Total"])
                        });
                    }
                }

                dgOrdenes.ItemsSource = ordenes;
                tbTotalOrdenes.Text = $"{ordenes.Count} órdenes";
                txtTotalOrdenes.Text = ordenes.Count.ToString();


                string sqlBalance = "SELECT ISNULL(SUM(OrdenPrecio_Total), 0) FROM Orden_Trabajo";
                using (SqlCommand cmd = new SqlCommand(sqlBalance, _conexion.SqlC))
                {
                    decimal balance = Convert.ToDecimal(cmd.ExecuteScalar());
                    txtBalanceTotal.Text = $"Q {balance:N2}";
                }

                string sqlGastos = "SELECT ISNULL(SUM(Precio_Gasto), 0) FROM Contabilidad_Gastos";
                using (SqlCommand cmd = new SqlCommand(sqlGastos, _conexion.SqlC))
                {
                    decimal gastos = Convert.ToDecimal(cmd.ExecuteScalar());
                    txtGastosTotales.Text = $"Q {gastos:N2}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }


        private void CargarNotificaciones()
        {
            _notificaciones.Clear();
            try
            {
                _conexion.Abrir();
                string sql = @"
                    SELECT Notificacion_ID, Tipo_Notificacion, Mensaje, Leida
                    FROM   Notificaciones
                    ORDER  BY Notificacion_ID DESC";

                using (SqlCommand cmd = new SqlCommand(sql, _conexion.SqlC))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        _notificaciones.Add(new NotificacionItem
                        {
                            Notificacion_ID = Convert.ToInt32(rd["Notificacion_ID"]),
                            Tipo_Notificacion = rd["Tipo_Notificacion"].ToString(),
                            Mensaje = rd["Mensaje"].ToString(),
                            Leida = Convert.ToBoolean(rd["Leida"])
                        });
                    }
                }
            }
            catch { }
            finally { _conexion.Cerrar(); }

            ActualizarPanelNotificaciones();
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
                    Text = "No hay notificaciones",
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
                return;
            }

            foreach (var notif in _notificaciones)
            {

                string colorHex = notif.Tipo_Notificacion == "STOCK_BAJO" ? "#F0A500"
                                : notif.Tipo_Notificacion == "ORDEN_FINALIZADA" ? "#4CAF50"
                                : "#4A9EFF";

                string iconoKind = notif.Tipo_Notificacion == "STOCK_BAJO" ? "AlertCircle"
                                 : notif.Tipo_Notificacion == "ORDEN_FINALIZADA" ? "CheckCircle"
                                 : "Bell";

                Color iconColor = (Color)ColorConverter.ConvertFromString(colorHex);

                Border card = new Border
                {
                    Background = new SolidColorBrush(notif.Leida
                        ? (Color)ColorConverter.ConvertFromString("#1E2130")
                        : (Color)ColorConverter.ConvertFromString("#1A2A3D")),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 8),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                Grid grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Border iconBorder = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Background = new SolidColorBrush(Color.FromArgb(40, iconColor.R, iconColor.G, iconColor.B)),
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 2, 8, 0)
                };
                iconBorder.Child = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = ParseIconKind(iconoKind),
                    Foreground = new SolidColorBrush(iconColor),
                    Width = 16,
                    Height = 16,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(iconBorder, 0);


                StackPanel texto = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                texto.Children.Add(new TextBlock
                {
                    Text = notif.Tipo_Notificacion.Replace("_", " "),
                    Foreground = new SolidColorBrush(iconColor),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Margin = new Thickness(0, 0, 0, 2)
                });
                texto.Children.Add(new TextBlock
                {
                    Text = notif.Mensaje,
                    Foreground = new SolidColorBrush(notif.Leida
                        ? (Color)ColorConverter.ConvertFromString("#8B9BB4")
                        : Colors.White),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                });
                Grid.SetColumn(texto, 1);


                Border punto = new Border
                {
                    Width = 8,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D7EFF")),
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

        private void MarcarComoLeida(int id)
        {
            try
            {
                _conexion.Abrir();
                using (SqlCommand cmd = new SqlCommand(
                    "EXEC sp_MarcarNotificacionLeida @NotificacionID", _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@NotificacionID", id);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
            finally { _conexion.Cerrar(); }

            var n = _notificaciones.FirstOrDefault(x => x.Notificacion_ID == id);
            if (n != null) n.Leida = true;
            ActualizarPanelNotificaciones();
        }

        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _conexion.Abrir();
                using (SqlCommand cmd = new SqlCommand(
                    "EXEC sp_MarcarNotificacionLeida", _conexion.SqlC))
                    cmd.ExecuteNonQuery();
            }
            catch { }
            finally { _conexion.Cerrar(); }

            foreach (var n in _notificaciones) n.Leida = true;
            ActualizarPanelNotificaciones();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            CargarNotificaciones();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }


        private void btnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(-1);
            GenerarCalendario();
        }

        private void btnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(1);
            GenerarCalendario();
        }

        private void GenerarCalendario()
        {
            var cultura = new CultureInfo("es-HN");
            string titulo = _mesActual.ToString("MMMM, yyyy", cultura);
            txtMesAnio.Text = char.ToUpper(titulo[0]) + titulo.Substring(1);
            gridDias.Children.Clear();

            foreach (string dia in new[] { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" })
                gridDias.Children.Add(new TextBlock
                {
                    Text = dia,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B9BB4")),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                });

            int primerDia = (int)_mesActual.DayOfWeek;
            primerDia = primerDia == 0 ? 6 : primerDia - 1;
            int diasEnMes = DateTime.DaysInMonth(_mesActual.Year, _mesActual.Month);
            int diasMesAnt = DateTime.DaysInMonth(
                _mesActual.AddMonths(-1).Year, _mesActual.AddMonths(-1).Month);

            for (int i = 0; i < 42; i++)
            {
                int dia; bool esDelMes = true;
                if (i < primerDia) { dia = diasMesAnt - primerDia + 1 + i; esDelMes = false; }
                else if (i >= primerDia + diasEnMes) { dia = i - primerDia - diasEnMes + 1; esDelMes = false; }
                else { dia = i - primerDia + 1; }

                bool esHoy = esDelMes && dia == DateTime.Today.Day
                             && _mesActual.Month == DateTime.Today.Month
                             && _mesActual.Year == DateTime.Today.Year;

                Border celda = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = esHoy
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4757"))
                        : Brushes.Transparent,
                    Margin = new Thickness(2)
                };
                celda.Child = new TextBlock
                {
                    Text = dia.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = esHoy ? Brushes.White
                                        : esDelMes ? Brushes.White
                                        : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A5568"))
                };
                gridDias.Children.Add(celda);
            }
        }

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            new MenúPrincipalOrdenes().Show();
            this.Close();
        }

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            new MenúPrincipalVehículos().Show();
            this.Close();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            new MenúPrincipalInventario().Show();
            this.Close();
        }
        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            new MenúPrincipalClientes().Show();
            this.Close();
        }
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            new ContaWindow().Show();
            this.Close();
        }
        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            new MenuDePagos().Show();
            this.Close();
        }
        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();
            this.Close();
        }

        private MaterialDesignThemes.Wpf.PackIconKind ParseIconKind(string nombre)
        {
            if (Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(nombre, out var kind))
                return kind;
            return MaterialDesignThemes.Wpf.PackIconKind.Bell;
        }
    }
}