using Login.Clases; 
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Contabilidad
{
    public partial class PanelNotificaciones : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        private Action _onCerrar;

        public PanelNotificaciones(Action onCerrar = null)
        {
            InitializeComponent();
            _onCerrar = onCerrar;
            CargarNotificaciones();
        }

        // ─── CARGAR NOTIFICACIONES ────────────────────────────────────────────
        private void CargarNotificaciones()
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

                    SqlCommand cmd = new SqlCommand(query, conn);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    conn.Open();
                    da.Fill(dt);

                    if (dt.Rows.Count == 0)
                    {
                        // Sin notificaciones pendientes
                        StackPanel vacio = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
                        vacio.Children.Add(new System.Windows.Controls.Label
                        {
                            Content = "🎉",
                            FontSize = 36,
                            HorizontalAlignment = HorizontalAlignment.Center
                        });
                        vacio.Children.Add(new TextBlock
                        {
                            Text = "Sin notificaciones pendientes",
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                            FontSize = 13,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 8, 0, 0)
                        });
                        panelNotificaciones.Children.Add(vacio);

                        badgeContador.Visibility = Visibility.Collapsed;
                        btnMarcarTodas.Visibility = Visibility.Collapsed;
                        return;
                    }

                    // Mostrar contador
                    txtContador.Text = dt.Rows.Count.ToString();
                    badgeContador.Visibility = Visibility.Visible;
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
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── CREAR TARJETA DE NOTIFICACIÓN ───────────────────────────────────
        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            // Colores según tipo
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string colorIcono = esStock ? "#F0A500" : "#3D7EFF";
            string icono = esStock ? "⚠️" : "💰";

            Border card = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorFondo)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorBorde)),
                BorderThickness = new Thickness(0, 0, 0, 3),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(14, 12, 14, 12)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Contenido izquierdo
            StackPanel contenido = new StackPanel();

            // Badge tipo
            Border badgeTipo = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorBorde + "33")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(6, 2, 6, 2),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 6)
            };
            badgeTipo.Child = new TextBlock
            {
                Text = esStock ? "Stock Bajo" : "Orden Finalizada",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorIcono)),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };
            contenido.Children.Add(badgeTipo);

            // Mensaje
            contenido.Children.Add(new TextBlock
            {
                Text = mensaje,
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 18
            });

            Grid.SetColumn(contenido, 0);
            grid.Children.Add(contenido);

            // Botón marcar leída (X)
            Button btnLeida = new Button
            {
                Content = "✓",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Marcar como leída",
                Tag = id
            };
            btnLeida.Click += BtnLeida_Click;
            Grid.SetColumn(btnLeida, 1);
            grid.Children.Add(btnLeida);

            card.Child = grid;
            return card;
        }

        // ─── MARCAR UNA COMO LEÍDA ────────────────────────────────────────────
        private void BtnLeida_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            MarcarLeida(id);
            CargarNotificaciones();
        }

        // ─── MARCAR TODAS COMO LEÍDAS ─────────────────────────────────────────
        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            MarcarLeida(null);
            CargarNotificaciones();
        }

        private void MarcarLeida(int? id)
        {
            try
            {

                _db.MarcarNotificacionLeida(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ─── AL CERRAR: REFRESCAR BADGE DEL MENÚ PADRE ───────────────────────
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onCerrar?.Invoke();
        }
    }
}
