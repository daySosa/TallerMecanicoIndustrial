using Dasboard_Prueba;
using Login;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vehículos;
using Login.Clases;

namespace Contabilidad
{
    /// <summary>
    /// Ventana principal del módulo de contabilidad.
    /// Permite gestionar egresos, visualizar comprobantes, acceder a otros módulos
    /// y administrar notificaciones del sistema.
    /// </summary>
    public partial class ContaWindow : Window
    {
        /// <summary>
        /// Instancia utilizada para realizar consultas y operaciones en la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="ContaWindow"/>
        /// y carga los datos iniciales del módulo.
        /// </summary>
        public ContaWindow()
        {
            InitializeComponent();
            CargarEgreso();
            CargarNotificaciones();
        }

        /// <summary>
        /// Carga la lista de gastos en el DataGrid, aplicando un filtro opcional de búsqueda.
        /// </summary>
        /// <param name="busqueda">Texto de búsqueda para filtrar los gastos.</param>
        public void CargarEgreso(string busqueda = null)
        {
            try
            {
                dgGastos.ItemsSource = _db.ObtenerGastos(busqueda).DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gastos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filtra los gastos en tiempo real según el texto ingresado en el campo de búsqueda.
        /// </summary>
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarEgreso(string.IsNullOrEmpty(texto) ? null : texto);
        }

        /// <summary>
        /// Abre la ventana para registrar un nuevo gasto.
        /// </summary>
        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarGasto();
            ventana.Owner = this;
            if (ventana.ShowDialog() == true)
                CargarEgreso();
        }

        //// <summary>
        /// Evento reservado para futuras funcionalidades de actualización manual.
        /// </summary>
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Abre la ventana de edición de un gasto al hacer doble clic sobre un registro.
        /// </summary>
        private void dgGastos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgGastos.SelectedItem is DataRowView fila)
            {
                var ventana = new ActualizarGasto(
                    gastoId: Convert.ToInt32(fila["Gasto_ID"]),
                    tipo: fila["Tipo_Gasto"].ToString(),
                    nombre: fila["Nombre_Gasto"].ToString(),
                    precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                    fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                    observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto") && fila["Observaciones_Gasto"] != DBNull.Value
                                   ? fila["Observaciones_Gasto"].ToString()
                                   : ""
                );
                ventana.Owner = this;
                ventana.ShowDialog();
                dgGastos.SelectedItem = null;
                CargarEgreso();
            }
        }

        /// <summary>
        /// Muestra el comprobante del gasto seleccionado.
        /// </summary>
        private void btnMostrarComprobante_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un gasto para ver el comprobante.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fila = (DataRowView)dgGastos.SelectedItem;

            var ventana = new MostrarComprobante(
                id: Convert.ToInt32(fila["Gasto_ID"]),
                tipo: fila["Tipo_Gasto"].ToString(),
                nombre: fila["Nombre_Gasto"].ToString(),
                precio: Convert.ToDecimal(fila["Precio_Gasto"]),
                fecha: Convert.ToDateTime(fila["Fecha_Gasto"]),
                observaciones: fila.Row.Table.Columns.Contains("Observaciones_Gasto") && fila["Observaciones_Gasto"] != DBNull.Value
                               ? fila["Observaciones_Gasto"].ToString()
                               : ""
            );

            ventana.Owner = this;
            ventana.ShowDialog();
        }

        /// <summary>
        /// Recarga la lista de egresos.
        /// </summary>
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            CargarEgreso();
        }

        /// <summary>
        /// Navega al módulo de ingresos (pagos).
        /// </summary>
        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            MenuDePagos ventana = new MenuDePagos();
            ventana.Show();
            this.Close();
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
            var ventana = new InterfazInventario.MenúPrincipalInventario();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de vehículos.
        /// </summary>
        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            MenúPrincipalVehículos ventana = new MenúPrincipalVehículos();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de clientes.
        /// </summary>
        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new InterfazClientes.MenúPrincipalClientes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Navega al módulo de órdenes de trabajo.
        /// </summary>
        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new Órdenes_de_Trabajo.MenúPrincipalOrdenes();
            ventana.Show();
            this.Close();
        }

        /// <summary>
        /// Cierra la sesión del usuario actual y regresa a la pantalla de inicio de sesión.
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
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        /// <summary>
        /// Carga las notificaciones pendientes dentro del panel emergente (popup).
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
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Crea una tarjeta visual para mostrar una notificación en el panel.
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
        /// Abre la ventana de reportes del módulo de egresos.
        /// </summary>
        private void btnReportes_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new ReportesWindow("Egresos");
            ventana.ShowDialog();
        }
    }
}