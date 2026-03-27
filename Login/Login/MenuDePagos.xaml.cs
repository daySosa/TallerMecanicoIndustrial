using Dasboard_Prueba;
using Login;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vehículos;
using InterfazInventario;
using InterfazClientes;
using Login.Clases;

namespace Contabilidad
{
    /// <summary>
    /// Ventana principal del módulo de pagos.
    /// Permite gestionar ingresos, visualizar comprobantes,
    /// editar registros y administrar notificaciones del sistema.
    /// </summary>
    public partial class MenuDePagos : Window
    {
        /// <summary>
        /// Instancia utilizada para realizar consultas y operaciones en la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Inicializa la ventana y carga los pagos junto con las notificaciones pendientes.
        /// </summary>
        public MenuDePagos()
        {
            InitializeComponent();
            CargarPago();
            CargarNotificaciones();
        }

        /// <summary>
        /// Carga la lista de pagos en el DataGrid, aplicando un filtro opcional de búsqueda.
        /// </summary>
        /// <param name="busqueda">Texto utilizado para filtrar los pagos.</param>
        public void CargarPago(string busqueda = null)
        {
            try
            {
                dgPagos.ItemsSource = _db.ObtenerPagos(busqueda).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar pagos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filtra los pagos en tiempo real según el texto ingresado.
        /// </summary>
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarPago(string.IsNullOrEmpty(texto) ? null : texto);
        }

        /// <summary>
        /// Abre la ventana para registrar un nuevo pago.
        /// </summary>
        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            AgregarPago ventana = new AgregarPago(this);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        /// <summary>
        /// Recarga la lista de pagos.
        /// </summary>
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            CargarPago();
        }

        /// <summary>
        /// Permite editar un pago al hacer doble clic sobre un registro.
        /// </summary>
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

        /// <summary>
        /// Muestra el comprobante del pago seleccionado.
        /// </summary>
        private void btnMostrarComprobantes_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarComboSeleccionado(dgPagos.SelectedItem, "pago para ver el comprobante"))
                return;

            DataRowView fila = (DataRowView)dgPagos.SelectedItem;
            int pagoId = Convert.ToInt32(fila["Pago_ID"]);
            ComprobanteDePago ventana = new ComprobanteDePago(pagoId);
            ventana.Owner = this;
            ventana.ShowDialog();
        }

        /// <summary>
        /// Muestra u oculta el panel de notificaciones.
        /// </summary>
        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        /// <summary>
        /// Carga el número de notificaciones pendientes y actualiza el indicador visual.
        /// </summary>
        public void CargarNotificaciones()
        {
            try
            {
                int cantidad = _db.ContarNotificacionesPendientes();
                badgeNotificaciones.Visibility = cantidad > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                txtContadorNotificaciones.Text = cantidad.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // <summary>
        /// Carga las notificaciones pendientes dentro del panel emergente.
        /// </summary>
        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();

            try
            {
                DataTable dt = _db.ObtenerNotificacionesPendientes();

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
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Crea una tarjeta visual para representar una notificación en la interfaz.
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
        /// Abre la ventana de reportes del módulo de pagos.
        /// </summary>
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Ingresos");
            ventana.ShowDialog();
        }

        /// <summary>
        /// Navega al menú principal del sistema.
        /// </summary>
        private void btnPantallaPrincipal_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenuPrincipal();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de inventario.
        /// </summary>
        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenúPrincipalInventario();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de vehículos.
        /// </summary>
        private void btnVehículos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenúPrincipalVehículos();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de gestión de clientes.
        /// </summary>
        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MenúPrincipalClientes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de egresos (gastos).
        /// </summary>
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ContaWindow();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de órdenes de trabajo.
        /// </summary>
        private void btnÓrdenes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Órdenes_de_Trabajo.MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Cierra la sesión del usuario actual.
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
    }
}