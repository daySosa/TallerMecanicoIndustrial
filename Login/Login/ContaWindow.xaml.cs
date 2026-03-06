using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Contabilidad
{


    public partial class ContaWindow : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        public ContaWindow()
        {
            InitializeComponent();
            CargarEgreso();
            CargarNotificaciones();
        }




        public void CargarEgreso(string busqueda = null)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
                        SELECT Gasto_ID, Tipo_Gasto, Nombre_Gasto, Precio_Gasto, Fecha_Gasto
                        FROM Contabilidad_Gastos
                        WHERE (@Busqueda IS NULL
                               OR Nombre_Gasto LIKE '%' + @Busqueda + '%'
                               OR Tipo_Gasto   LIKE '%' + @Busqueda + '%')
                        ORDER BY Fecha_Gasto DESC";

                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Busqueda", (object)busqueda ?? DBNull.Value);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);

                    dgGastos.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gastos: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string texto = txtBuscar.Text.Trim();
            CargarEgreso(string.IsNullOrEmpty(texto) ? null : texto);
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarGasto();
            ventana.Owner = this;
            if (ventana.ShowDialog() == true)
                CargarEgreso();
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (dgGastos.SelectedItem == null)
            {
                MessageBox.Show("Selecciona un gasto para actualizar.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fila = (DataRowView)dgGastos.SelectedItem;

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
            if (ventana.ShowDialog() == true)
                CargarEgreso();
        }

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
        private void btnEgresos_Click(object sender, RoutedEventArgs e)
        {
            CargarEgreso();
        }

        private void btnIngresos_Click(object sender, RoutedEventArgs e)
        {
            MenuDePagos ventana = new MenuDePagos();
            ventana.Show();
            this.Close();
        }

        private void btnPantallaPrincipal_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a la pantalla principal
        }

        private void btnInventario_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas al inventario
        }

        private void btnVehiculos_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a vehículos
        }

        private void btnClientes_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a clientes
        }

        private void btnOrdenes_Click(object sender, RoutedEventArgs e)
        {
            // Aquí navegas a órdenes
        }

        private void btnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("¿Estás seguro que deseas cerrar sesión?", "Cerrar Sesión",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                this.Close();
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
                using (SqlConnection conn = new SqlConnection(connectionString))
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
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message);
            }
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
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

                        // ── Label en vez de TextBlock para el emoji ──
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
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand("sp_MarcarNotificacionLeida", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@NotificacionID",
                        id.HasValue ? (object)id.Value : DBNull.Value);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}


