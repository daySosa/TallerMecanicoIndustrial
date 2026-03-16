using Login;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Contabilidad
{
    public partial class MenuDePagos : Window
    {
        private string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        public MenuDePagos()
        {
            InitializeComponent();
            CargarPago();
            CargarNotificaciones();
        }

        public void CargarPago(string busqueda = null)
        {
            using (SqlConnection conn = new SqlConnection(conexion))
            {
                string query = @"
                    SELECT 
                        Pago_ID,
                        Cliente_DNI,
                        Cliente_Nombres,
                        Orden_ID,
                        Precio_Pago,
                        Fecha_Pago
                    FROM Vista_Pagos_Completos
                    WHERE (@Busqueda IS NULL
                           OR CAST(Pago_ID AS VARCHAR) LIKE '%' + @Busqueda + '%'
                           OR Cliente_Nombres        LIKE '%' + @Busqueda + '%'
                           OR Cliente_Apellidos      LIKE '%' + @Busqueda + '%')
                    ORDER BY Fecha_Pago DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Busqueda", (object)busqueda ?? DBNull.Value);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                conn.Open();
                da.Fill(dt);

                dgPagos.ItemsSource = dt.DefaultView;
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarPago(string.IsNullOrEmpty(texto) ? null : texto);
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            AgregarPago ventana = new AgregarPago(this);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
        }

        private void dgPagos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgPagos.SelectedItem is DataRowView fila)
            {
                int pagoId = Convert.ToInt32(fila["Pago_ID"]);
                string dniStr = fila["Cliente_DNI"].ToString();
                int ordenId = Convert.ToInt32(fila["Orden_ID"]);
                decimal monto = Convert.ToDecimal(fila["Precio_Pago"]);
                DateTime fecha = Convert.ToDateTime(fila["Fecha_Pago"]);

                ActualizarPago ventana = new ActualizarPago(this, pagoId, dniStr, ordenId, monto, fecha);
                ventana.Owner = this;
                ventana.ShowDialog();
                CargarPago();
            }
        }

        private void btnMostrarComprobantes_Click(object sender, RoutedEventArgs e)
        {
            if (dgPagos.SelectedItem == null)
            {
                MessageBox.Show("⚠ Selecciona un pago para ver el comprobante.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataRowView fila = (DataRowView)dgPagos.SelectedItem;
            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            ComprobanteDePago ventana = new ComprobanteDePago(pagoId);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        public void CargarNotificaciones()
        {
            using (SqlConnection conn = new SqlConnection(conexion))
            {
                string query = "SELECT COUNT(*) FROM Notificaciones WHERE Leida = 0";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                int cantidad = (int)cmd.ExecuteScalar();

                badgeNotificaciones.Visibility = cantidad > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad.ToString();
            }
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();

            using (SqlConnection conn = new SqlConnection(conexion))
            {
                string query = @"
                    SELECT Notificacion_ID, Tipo_Notificacion, Mensaje
                    FROM Vista_Notificaciones_Pendientes
                    ORDER BY Notificacion_ID DESC";

                SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(query, conn));
                DataTable dt = new DataTable();
                conn.Open();
                da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    StackPanel vacio = new StackPanel
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

                txtContadorPopup.Text = dt.Rows.Count.ToString();
                badgeContadorPopup.Visibility = Visibility.Visible;
                btnMarcarTodas.Visibility = Visibility.Visible;

                foreach (DataRow row in dt.Rows)
                {
                    int id = Convert.ToInt32(row["Notificacion_ID"]);
                    string tipo = row["Tipo_Notificacion"].ToString();
                    string msg = row["Mensaje"].ToString();
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
        }

        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string colorIcono = esStock ? "#F0A500" : "#3D7EFF";
            string labelTipo = esStock ? "Stock Bajo" : "Orden Finalizada";

            Border card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorFondo)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorBorde)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(12, 10, 12, 10)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel contenido = new StackPanel();

            Border badgeTipo = new Border
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

            Button btnLeida = new Button
            {
                Content = "✓",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 15,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Marcar como leída",
                Tag = id
            };
            btnLeida.Click += (s, e) =>
            {
                MarcarLeida((int)((Button)s).Tag);
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
            MarcarLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }

        private void MarcarLeida(int? id)
        {
            using (SqlConnection conn = new SqlConnection(conexion))
            {
                SqlCommand cmd = new SqlCommand("sp_MarcarNotificacionLeida", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotificacionID",
                    id.HasValue ? (object)id.Value : DBNull.Value);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Ingresos");
            ventana.ShowDialog();
        }
    }
}