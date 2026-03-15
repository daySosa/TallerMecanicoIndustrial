using Dasboard_Prueba;
using Login;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public class OrdenTrabajo
    {
        public int Orden_ID { get; set; }
        public string Cliente_DNI { get; set; }
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

    public partial class MenúPrincipalOrdenes : Window
    {
        private clsConexion _conexion = new clsConexion();
        private ObservableCollection<OrdenTrabajo> _listaOrdenes = new ObservableCollection<OrdenTrabajo>();
        private ICollectionView? _vistaOrdenes;

        private string _filtroCliente = "";
        private string _filtroEstado = "Todos";

        public MenúPrincipalOrdenes()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }


        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenuPrincipal();
            ventana.Show();
            this.Close();
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InterfazInventario.MenúPrincipalInventario();
            ventana.Show();
            this.Close();
        }

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Vehículos.MenúPrincipalVehículos();
            ventana.Show();
            this.Close();
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InterfazClientes.MenúPrincipalClientes();
            ventana.Show();
            this.Close();
        }

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Contabilidad.ContaWindow();
            ventana.Show();
            this.Close();

        }


        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Contabilidad.MenuDePagos();
            ventana.Show();
            this.Close();
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado == MessageBoxResult.Yes)
            {
                var login = new Login.MainWindow();
                login.Show();
                this.Close();
            }
        }

        private void CargarDatosDesdeDB()
        {
            _listaOrdenes.Clear();
            try
            {
                _conexion.Abrir();
                string query = @"
                    SELECT
                        o.Orden_ID,
                        o.Cliente_DNI,
                        c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS Cliente_NombreCompleto,
                        o.Vehiculo_Placa,
                        o.Producto_ID,
                        ISNULL(p.Producto_Nombre,   '—') AS Producto_Nombre,
                        ISNULL(p.Producto_Categoria,'—') AS Producto_Categoria,
                        o.Estado,
                        o.Fecha,
                        o.Fecha_Entrega,
                        ISNULL(o.Observaciones, '') AS Observaciones,
                        o.Servicio_Precio,
                        o.OrdenPrecio_Total
                    FROM  Orden_Trabajo o
                    INNER JOIN Cliente c      ON o.Cliente_DNI   = c.Cliente_DNI
                    LEFT  JOIN Producto p     ON o.Producto_ID   = p.Producto_ID
                    ORDER BY o.Orden_ID DESC";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        _listaOrdenes.Add(new OrdenTrabajo
                        {
                            Orden_ID = rd.GetInt32(rd.GetOrdinal("Orden_ID")),
                            Cliente_DNI = rd["Cliente_DNI"].ToString(),
                            Cliente_NombreCompleto = rd["Cliente_NombreCompleto"].ToString(),
                            Vehiculo_Placa = rd["Vehiculo_Placa"].ToString(),
                            Producto_Nombre = rd["Producto_Nombre"].ToString(),
                            Producto_Categoria = rd["Producto_Categoria"].ToString(),
                            Estado = rd["Estado"].ToString(),
                            Fecha = rd.GetDateTime(rd.GetOrdinal("Fecha")),
                            Fecha_Entrega = rd["Fecha_Entrega"] != DBNull.Value
                                                     ? rd.GetDateTime(rd.GetOrdinal("Fecha_Entrega"))
                                                     : null,
                            Observaciones = rd["Observaciones"].ToString(),
                            Servicio_Precio = rd.GetDecimal(rd.GetOrdinal("Servicio_Precio")),
                            OrdenPrecio_Total = rd.GetDecimal(rd.GetOrdinal("OrdenPrecio_Total"))
                        });
                    }
                }

                _vistaOrdenes = CollectionViewSource.GetDefaultView(_listaOrdenes);
                _vistaOrdenes.Filter = AplicarFiltros;
                dgOrdenes.ItemsSource = _vistaOrdenes;
                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar órdenes:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not OrdenTrabajo o) return false;

            if (_filtroEstado != "Finalizado" && o.Estado == "Finalizado") return false;

            string busqueda = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(busqueda))
            {
                bool coincide =
                    (o.Cliente_NombreCompleto ?? "").ToLower().Contains(busqueda) ||
                    (o.Vehiculo_Placa ?? "").ToLower().Contains(busqueda) ||
                    (o.Producto_Nombre ?? "").ToLower().Contains(busqueda) ||
                    (o.Estado ?? "").ToLower().Contains(busqueda) ||
                    o.Orden_ID.ToString().Contains(busqueda);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroCliente))
                if (!(o.Cliente_NombreCompleto ?? "").ToLower().Contains(_filtroCliente))
                    return false;

            if (_filtroEstado != "Todos")
                if (o.Estado != _filtroEstado) return false;

            return true;
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            popupFiltros.IsOpen = !popupFiltros.IsOpen;
        }

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroCliente = txtFiltroCliente.Text?.Trim().ToLower() ?? "";
            _filtroEstado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroCliente.Clear();
            cmbFiltroEstado.SelectedIndex = 0;
            _filtroCliente = "";
            _filtroEstado = "Todos";

            popupFiltros.IsOpen = false;
            _vistaOrdenes?.Refresh();
            ActualizarContador();
        }

        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaOrdenes != null)
                foreach (var _ in _vistaOrdenes) total++;
            tbTotalOrdenes.Text = $"{total} orden{(total != 1 ? "es" : "")}";
        }

        private async void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is not OrdenTrabajo seleccionada) return;

            dgOrdenes.SelectedItem = null;

            var ventana = new OrdenWindow();
            ventana.Closed += (s, args) =>
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
                _conexion.Abrir();
                string query = "SELECT COUNT(*) FROM Notificaciones WHERE Leida = 0";
                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    int cantidad = (int)cmd.ExecuteScalar();
                    badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                    txtContadorNotificaciones.Text = cantidad.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                _conexion.Abrir();
                string query = @"
                    SELECT Notificacion_ID, Tipo_Notificacion, Mensaje
                    FROM   Vista_Notificaciones_Pendientes
                    ORDER  BY Notificacion_ID DESC";

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(query, _conexion.SqlC)))
                    da.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    var vacio = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 20, 0, 20) };
                    vacio.Children.Add(new Label { Content = "🎉", FontSize = 32, HorizontalAlignment = HorizontalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Colors.White), Padding = new Thickness(0) });
                    vacio.Children.Add(new TextBlock { Text = "Sin notificaciones pendientes", Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")), FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) });
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
                    string tipo = row["Tipo_Notificacion"].ToString() ?? "";
                    string msg = row["Mensaje"].ToString() ?? "";
                    panelNotificaciones.Children.Add(CrearTarjeta(id, tipo, msg));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
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
            badgeTipo.Child = new TextBlock { Text = labelTipo, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorIcono)), FontSize = 10, FontWeight = FontWeights.SemiBold };
            contenido.Children.Add(badgeTipo);
            contenido.Children.Add(new TextBlock { Text = mensaje, Foreground = new SolidColorBrush(Colors.White), FontSize = 11, TextWrapping = TextWrapping.Wrap, LineHeight = 17 });

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
            try
            {
                _conexion.Abrir();
                SqlCommand cmd = new SqlCommand("sp_MarcarNotificacionLeida", _conexion.SqlC);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotificacionID", id.HasValue ? (object)id.Value : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Ordenes");
            ventana.ShowDialog();
        }
    }
}