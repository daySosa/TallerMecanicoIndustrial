using Dasboard_Prueba;
using InterfazClientes;
using Login;
using Login.Clases;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Vehículos
{
    /// <summary>
    /// Ventana principal del módulo de vehículos.
    /// Permite visualizar, filtrar, crear y gestionar vehículos,
    /// así como manejar notificaciones y navegación entre módulos.
    /// </summary>
    public partial class MenúPrincipalVehículos : Window
    {
        /// <summary>
        /// Instancia para realizar consultas a la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Colección observable que almacena la lista de vehículos.
        /// </summary>
        private ObservableCollection<Vehiculo> _listaVehiculos = new ObservableCollection<Vehiculo>();

        /// <summary>
        /// Vista de colección utilizada para aplicar filtros y ordenar los vehículos.
        /// </summary>
        private ICollectionView _vistaVehiculos;

        /// <summary>
        /// Filtro por placa del vehículo.
        /// </summary>
        private string _filtroPlaca = "";

        /// <summary>
        /// Filtro por modelo del vehículo.
        /// </summary>
        private string _filtroModelo = "";

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MenúPrincipalVehículos"/>.
        /// </summary>
        public MenúPrincipalVehículos()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
            CargarNotificaciones();
        }

        /// <summary>
        /// Evento click del botón Home.
        /// Navega hacia el menú principal.
        /// </summary>
        private void btnHome_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenuPrincipal();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón Inventario.
        /// Abre el módulo de inventario.
        /// </summary>
        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InterfazInventario.MenúPrincipalInventario();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón Clientes.
        /// Abre el módulo de clientes.
        /// </summary>
        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenúPrincipalClientes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón Órdenes.
        /// Abre el módulo de órdenes de trabajo.
        /// </summary>
        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Órdenes_de_Trabajo.MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón Egresos.
        /// Abre el módulo de contabilidad (egresos).
        /// </summary>
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Contabilidad.ContaWindow();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón Ingresos.
        /// Abre el módulo de pagos/ingresos.
        /// </summary>
        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Contabilidad.MenuDePagos();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Evento click del botón cerrar sesión.
        /// Solicita confirmación y redirige al login si se confirma.
        /// </summary>
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

        /// <summary>
        /// Carga los vehículos desde la base de datos y los muestra en la interfaz.
        /// </summary>
        private void CargarDatosDesdeDB()
        {
            _listaVehiculos.Clear();
            try
            {
                var vehiculos = _db.ObtenerVehiculos();
                foreach (var v in vehiculos)
                    _listaVehiculos.Add(v);

                _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                _vistaVehiculos.Filter = AplicarFiltros;
                dgVehiculos.ItemsSource = _vistaVehiculos;
                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar vehículos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Aplica los filtros de búsqueda, placa y modelo a la lista de vehículos.
        /// </summary>
        /// <param name="item">Elemento a evaluar.</param>
        /// <returns>True si el elemento cumple los filtros; de lo contrario, false.</returns>
        private bool AplicarFiltros(object item)
        {
            if (item is not Vehiculo v) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (v.Vehiculo_Placa ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Marca ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Modelo ?? "").ToLower().Contains(texto) ||
                    (v.Vehiculo_Tipo ?? "").ToLower().Contains(texto) ||
                    (v.Cliente_NombreCompleto ?? "").ToLower().Contains(texto);
                if (!coincide) return false;
            }

            if (!string.IsNullOrEmpty(_filtroPlaca))
                if (!(v.Vehiculo_Placa ?? "").ToLower().Contains(_filtroPlaca)) return false;

            if (!string.IsNullOrEmpty(_filtroModelo))
                if (!(v.Vehiculo_Modelo ?? "").ToLower().Contains(_filtroModelo)) return false;

            return true;
        }

        /// <summary>
        /// Evento de cambio de texto en el buscador.
        /// Actualiza la vista filtrada.
        /// </summary>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        /// <summary>
        /// Muestra u oculta el popup de filtros.
        /// </summary>
        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            popupFiltros.IsOpen = !popupFiltros.IsOpen;
        }

        /// <summary>
        /// Aplica los filtros seleccionados por el usuario.
        /// </summary>
        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroPlaca = txtFiltroPlaca.Text?.Trim().ToLower() ?? "";
            _filtroModelo = txtFiltroModelo.Text?.Trim().ToLower() ?? "";

            popupFiltros.IsOpen = false;
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }


        /// <summary>
        /// Limpia los filtros aplicados.
        /// </summary>
        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            txtFiltroPlaca.Clear();
            txtFiltroModelo.Clear();
            _filtroPlaca = "";
            _filtroModelo = "";

            popupFiltros.IsOpen = false;
            _vistaVehiculos?.Refresh();
            ActualizarContador();
        }

        /// <summary>
        /// Actualiza el contador de vehículos mostrados en la interfaz.
        /// </summary>
        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaVehiculos != null)
                foreach (var _ in _vistaVehiculos) total++;
            tbTotalVehiculos.Text = $"{total} vehículo{(total != 1 ? "s" : "")}";
        }

        /// <summary>
        /// Evento al seleccionar un vehículo en el DataGrid.
        /// Abre la ventana de edición con los datos seleccionados.
        /// </summary>
        private void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVehiculos.SelectedItem is Vehiculo seleccionado)
            {
                var ventana = new VehiWindow();
                ventana.CargarVehiculoParaEditar(seleccionado);
                ventana.ShowDialog();

                dgVehiculos.SelectedItem = null;
                CargarDatosDesdeDB();
            }
        }

        /// <summary>
        /// Evento click para crear un nuevo vehículo.
        /// Abre la ventana de registro.
        /// </summary>
        private void BtnNuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new VehiWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();
        }

        /// <summary>
        /// Evento click para mostrar u ocultar notificaciones.
        /// </summary>
        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        /// <summary>
        /// Carga la cantidad de notificaciones pendientes desde la base de datos.
        /// </summary>
        public void CargarNotificaciones()
        {
            try
            {
                int cantidad = _db.ContarNotificacionesPendientes();
                badgeNotificaciones.Visibility = cantidad > 0 ? Visibility.Visible : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        /// <summary>
        /// Carga las notificaciones pendientes en el popup visual.
        /// </summary>
        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();
            try
            {
                DataTable dt = _db.ObtenerNotificacionesPendientes();

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
        }

        /// <summary>
        /// Crea una tarjeta visual para representar una notificación.
        /// </summary>
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
                _db.MarcarNotificacionLeida((int)((Button)s).Tag);
                CargarNotificacionesEnPopup();
                CargarNotificaciones();
            };
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(btnLeida);
            card.Child = grid;
            return card;
        }


        /// <summary>
        /// Marca todas las notificaciones como leídas.
        /// </summary>
        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            _db.MarcarNotificacionLeida(null);
            CargarNotificacionesEnPopup();
            CargarNotificaciones();
        }


        /// <summary>
        /// Abre la ventana de reportes para vehículos.
        /// </summary>
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Vehiculos");
            ventana.ShowDialog();
        }
    }
}
