using Dasboard_Prueba;
using InterfazClientes;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace InterfazInventario
{
    public class Repuesto : INotifyPropertyChanged
    {
        public int Producto_ID { get; set; }
        public string? Producto_Nombre { get; set; }
        public string? Producto_Categoria { get; set; }
        public string? Producto_Marca { get; set; }
        public string? Producto_Modelo { get; set; }
        public int Producto_Cantidad_Actual { get; set; }
        public int Producto_Cantidad_Minima { get; set; }
        public decimal Producto_Precio { get; set; }

        public bool StockBajo => Producto_Cantidad_Actual < Producto_Cantidad_Minima;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MenúPrincipalInventario : Window
    {
        private clsConexion _conexion = new clsConexion();
        private ObservableCollection<Repuesto> _listaRepuestos = new ObservableCollection<Repuesto>();
        private ICollectionView? _vistaRepuestos;

        private string? _filtroCategoria = null;
        private decimal _filtroPrecioMin = 0;
        private decimal _filtroPrecioMax = decimal.MaxValue;
        private bool _filtroStockBajo = false;

        public MenúPrincipalInventario()
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

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Vehículos.MenúPrincipalVehículos();
            ventana.Show();
            this.Close();
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenúPrincipalClientes();
            ventana.Show();
            this.Close();
        }

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Órdenes_de_Trabajo.MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();

        }

        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            //var ventana = new MenúPrincipalEgresos();
            // ventana.Show();
            // this.Close();
            MessageBox.Show("Módulo de Egresos próximamente.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            // var ventana = new MenúPrincipalIngresos();
            // ventana.Show();
            // this.Close();
            MessageBox.Show("Módulo de Ingresos próximamente.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
            _listaRepuestos.Clear();
            try
            {
                _conexion.Abrir();
                string query = @"
                    SELECT Producto_ID,
                           Producto_Nombre,
                           Producto_Categoria,
                           ISNULL(Producto_Marca,  '—') AS Producto_Marca,
                           ISNULL(Producto_Modelo, '—') AS Producto_Modelo,
                           Producto_Cantidad_Actual,
                           Producto_Stock_Minimo,
                           Producto_Precio
                    FROM   Producto
                    ORDER  BY Producto_Nombre";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _listaRepuestos.Add(new Repuesto
                        {
                            Producto_ID = reader.GetInt32(reader.GetOrdinal("Producto_ID")),
                            Producto_Nombre = reader["Producto_Nombre"].ToString(),
                            Producto_Categoria = reader["Producto_Categoria"].ToString(),
                            Producto_Marca = reader["Producto_Marca"].ToString(),
                            Producto_Modelo = reader["Producto_Modelo"].ToString(),
                            Producto_Cantidad_Actual = reader.GetInt32(reader.GetOrdinal("Producto_Cantidad_Actual")),
                            Producto_Cantidad_Minima = reader.GetInt32(reader.GetOrdinal("Producto_Stock_Minimo")),
                            Producto_Precio = reader.GetDecimal(reader.GetOrdinal("Producto_Precio"))
                        });
                    }
                }

                var categorias = _listaRepuestos
                    .Select(r => r.Producto_Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                categorias.Insert(0, "Todas");
                cmbCategoria.ItemsSource = categorias;
                cmbCategoria.SelectedIndex = 0;

                _vistaRepuestos = CollectionViewSource.GetDefaultView(_listaRepuestos);
                _vistaRepuestos.Filter = AplicarFiltros;
                dgInventario.ItemsSource = _vistaRepuestos;

                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not Repuesto r) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (r.Producto_Nombre ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Categoria ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Marca ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Modelo ?? "").ToLower().Contains(texto);
                if (!coincide) return false;
            }

            if (_filtroCategoria != null && _filtroCategoria != "Todas")
                if (r.Producto_Categoria != _filtroCategoria) return false;

            if (r.Producto_Precio < _filtroPrecioMin) return false;
            if (r.Producto_Precio > _filtroPrecioMax) return false;
            if (_filtroStockBajo && !r.StockBajo) return false;

            return true;
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            popupFiltros.IsOpen = !popupFiltros.IsOpen;
        }

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroCategoria = cmbCategoria.SelectedItem?.ToString();
            _filtroPrecioMin = decimal.TryParse(txtPrecioMin.Text, out decimal pMin) ? pMin : 0;
            _filtroPrecioMax = decimal.TryParse(txtPrecioMax.Text, out decimal pMax) ? pMax : decimal.MaxValue;
            _filtroStockBajo = chkStockBajo.IsChecked == true;

            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbCategoria.SelectedIndex = 0;
            txtPrecioMin.Clear();
            txtPrecioMax.Clear();
            chkStockBajo.IsChecked = false;

            _filtroCategoria = null;
            _filtroPrecioMin = 0;
            _filtroPrecioMax = decimal.MaxValue;
            _filtroStockBajo = false;

            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }

        private void dgInventario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInventario.SelectedItem is Repuesto seleccionado)
            {
                var ventana = new InventarioWindow();
                ventana.CargarProductoParaEditar(seleccionado);
                ventana.ShowDialog();

                dgInventario.SelectedItem = null;
                CargarDatosDesdeDB();
                CargarNotificaciones();
            }
        }

        private void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InventarioWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaRepuestos != null)
                foreach (var _ in _vistaRepuestos) total++;
            tbTotalItems.Text = $"{total} item{(total != 1 ? "s" : "")}";
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
    }
}