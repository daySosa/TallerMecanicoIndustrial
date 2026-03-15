using Dasboard_Prueba;
using Login;
using Login.Clases;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#pragma warning disable CS0618

namespace InterfazClientes
{
    public partial class MenúPrincipalClientes : Window
    {
        private List<Cliente> _listaClientes = new List<Cliente>();
        private List<Cliente> _listaFiltrada = new List<Cliente>();
        private clsConexion _db = new clsConexion();

        private string _filtroNombre = "";
        private string _filtroTelefono = "";
        private string _filtroEstado = "Todos";
        private bool _editando = false;

        public MenúPrincipalClientes()
        {
            InitializeComponent();
            CargarClientes();
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

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Órdenes_de_Trabajo.MenúPrincipalOrdenes();
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

        private string FormatearTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return telefono;
            string soloNumeros = System.Text.RegularExpressions.Regex.Replace(telefono, @"\D", "");
            if (soloNumeros.Length == 8)
                return soloNumeros.Substring(0, 4) + "-" + soloNumeros.Substring(4);
            return telefono;
        }

        public void CargarClientes()
        {
            _listaClientes.Clear();
            try
            {
                _db.Abrir();
                string sql = @"
                    SELECT Cliente_DNI,
                           Cliente_Nombres,
                           Cliente_Apellidos,
                           Cliente_TelefonoPrincipal,
                           Cliente_Email,
                           Cliente_Direccion,
                           Cliente_Activo
                    FROM   Cliente
                    ORDER  BY Cliente_Nombres";

                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    _listaClientes.Add(new Cliente
                    {
                        Cliente_DPI = rd["Cliente_DNI"].ToString(),
                        Cliente_Nombre = rd["Cliente_Nombres"].ToString(),
                        Cliente_Apellido = rd["Cliente_Apellidos"].ToString(),
                        Cliente_Telefono = FormatearTelefono(rd["Cliente_TelefonoPrincipal"].ToString()),
                        Cliente_Correo = rd["Cliente_Email"].ToString(),
                        Cliente_Direccion = rd["Cliente_Direccion"].ToString(),
                        Cliente_Activo = rd["Cliente_Activo"] != DBNull.Value
                                            && (bool)rd["Cliente_Activo"]
                    });
                }
                rd.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar: " + ex.Message);
            }
            finally { _db.Cerrar(); }

            AplicarFiltros();
        }

        private void AplicarFiltros()
        {
            string busqueda = txtBuscar.Text?.Trim().ToLower() ?? "";

            _listaFiltrada = _listaClientes.FindAll(c =>
            {
                if (!string.IsNullOrEmpty(busqueda))
                {
                    bool coincide =
                        (c.Cliente_Nombre ?? "").ToLower().Contains(busqueda) ||
                        (c.Cliente_Apellido ?? "").ToLower().Contains(busqueda) ||
                        (c.Cliente_DPI ?? "").ToLower().Contains(busqueda) ||
                        (c.Cliente_Telefono ?? "").ToLower().Contains(busqueda);
                    if (!coincide) return false;
                }

                if (!string.IsNullOrEmpty(_filtroNombre))
                    if (!(c.Cliente_Nombre ?? "").ToLower().Contains(_filtroNombre) &&
                        !(c.Cliente_Apellido ?? "").ToLower().Contains(_filtroNombre))
                        return false;

                if (!string.IsNullOrEmpty(_filtroTelefono))
                    if (!(c.Cliente_Telefono ?? "").ToLower().Contains(_filtroTelefono))
                        return false;

                if (_filtroEstado == "Activo" && !c.Cliente_Activo) return false;
                if (_filtroEstado == "Inactivo" && c.Cliente_Activo) return false;

                return true;
            });

            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = _listaFiltrada;
            tbTotalClientes.Text = $"{_listaFiltrada.Count} cliente{(_listaFiltrada.Count != 1 ? "s" : "")}";
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            popupFiltros.IsOpen = !popupFiltros.IsOpen;
        }

        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroNombre = txtFiltroNombre.Text?.Trim().ToLower() ?? "";
            _filtroTelefono = txtFiltroTelefono.Text?.Trim().ToLower() ?? "";
            _filtroEstado = (cmbFiltroEstado.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            popupFiltros.IsOpen = false;
            AplicarFiltros();
        }

        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroNombre.Clear();
            txtFiltroTelefono.Clear();
            cmbFiltroEstado.SelectedIndex = 0;
            _filtroNombre = "";
            _filtroTelefono = "";
            _filtroEstado = "Todos";

            popupFiltros.IsOpen = false;
            AplicarFiltros();
        }

        private void btnAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var formulario = new ClientesWindow();
            bool? resultado = formulario.ShowDialog();

            if (resultado == true && formulario.ClienteResultado != null)
            {
                CargarClientes();
            }
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_editando) return;
            if (dgClientes.SelectedItem is Cliente seleccionado)
            {
                _editando = true;
                var formulario = new ClientesWindow();
                formulario.CargarClienteParaEditar(seleccionado);
                formulario.ShowDialog();

                dgClientes.SelectedItem = null;
                _editando = false;
                CargarClientes();
            }
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
                _db.Abrir();
                string query = "SELECT COUNT(*) FROM Notificaciones WHERE Leida = 0";
                using (SqlCommand cmd = new SqlCommand(query, _db.SqlC))
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
            finally { _db.Cerrar(); }
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                _db.Abrir();
                string query = @"
                    SELECT Notificacion_ID, Tipo_Notificacion, Mensaje
                    FROM   Vista_Notificaciones_Pendientes
                    ORDER  BY Notificacion_ID DESC";

                DataTable dt = new DataTable();
                using (SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(query, _db.SqlC)))
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
            finally { _db.Cerrar(); }
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
            try
            {
                _db.Abrir();
                SqlCommand cmd = new SqlCommand("sp_MarcarNotificacionLeida", _db.SqlC);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotificacionID", id.HasValue ? (object)id.Value : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally { _db.Cerrar(); }
        }

        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Clientes");
            ventana.ShowDialog();
        }
    }
}