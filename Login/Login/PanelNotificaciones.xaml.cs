using Login.Clases;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Contabilidad
{
    /// <summary>
    /// Ventana que muestra un panel de notificaciones del sistema.
    /// Permite visualizar notificaciones pendientes, marcarlas como leídas
    /// y actualizar el estado de las mismas en la base de datos.
    /// </summary>
    public partial class PanelNotificaciones : Window
    {

        /// <summary>
        /// Instancia de acceso a la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Acción que se ejecuta al cerrar la ventana.
        /// </summary>
        private Action _onCerrar;

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="PanelNotificaciones"/>.
        /// </summary>
        /// <param name="onCerrar">Acción opcional que se ejecuta al cerrar la ventana.</param>
        public PanelNotificaciones(Action onCerrar = null)
        {
            InitializeComponent();
            _onCerrar = onCerrar;
            CargarNotificaciones();
        }


        /// <summary>
        /// Carga las notificaciones pendientes desde la base de datos
        /// y las muestra en el panel de la interfaz.
        /// </summary>
        private void CargarNotificaciones()
        {
            panelNotificaciones.Children.Clear();

            try
            {

                DataTable dt = _db.ObtenerNotificacionesPendientes();

                if (dt == null || dt.Rows.Count == 0)
                {
                    MostrarPantallaVacia();
                    badgeContador.Visibility = Visibility.Collapsed;
                    btnMarcarTodas.Visibility = Visibility.Collapsed;
                    return;
                }


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
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar notificaciones: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Muestra una vista cuando no existen notificaciones pendientes.
        /// </summary>
        private void MostrarPantallaVacia()
        {
            StackPanel vacio = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 40, 0, 0) };
            vacio.Children.Add(new Label { Content = "🎉", FontSize = 36, HorizontalAlignment = HorizontalAlignment.Center });
            vacio.Children.Add(new TextBlock
            {
                Text = "Sin notificaciones pendientes",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });
            panelNotificaciones.Children.Add(vacio);
        }


        /// <summary>
        /// Crea una tarjeta visual que representa una notificación.
        /// </summary>
        /// <param name="id">Identificador de la notificación.</param>
        /// <param name="tipo">Tipo de notificación.</param>
        /// <param name="mensaje">Mensaje de la notificación.</param>
        /// <returns>Un control <see cref="Border"/> que contiene la tarjeta.</returns>
        private Border CrearTarjeta(int id, string tipo, string mensaje)
        {
            bool esStock = tipo == "STOCK_BAJO";
            string colorBorde = esStock ? "#F0A500" : "#3D7EFF";
            string colorFondo = esStock ? "#1A1500" : "#0D1A2E";
            string colorIcono = esStock ? "#F0A500" : "#3D7EFF";

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

            StackPanel contenido = new StackPanel();

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
                Text = esStock ? "⚠️ Stock Bajo" : "💰 Orden Finalizada",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorIcono)),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold
            };

            contenido.Children.Add(badgeTipo);
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

        /// <summary>
        /// Maneja el evento Click del botón para marcar una notificación como leída.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento <see cref="RoutedEventArgs"/>.</param>
        private void BtnLeida_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                MarcarLeida(id);
                CargarNotificaciones();
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón para marcar todas las notificaciones como leídas.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento <see cref="RoutedEventArgs"/>.</param>
        private void btnMarcarTodas_Click(object sender, RoutedEventArgs e)
        {
            MarcarLeida(null);
            CargarNotificaciones();
        }

        /// <summary>
        /// Marca una notificación como leída en la base de datos.
        /// </summary>
        /// <param name="id">Identificador de la notificación. Si es null, marca todas como leídas.</param>
        private void MarcarLeida(int? id)
        {
            try
            {

                _db.MarcarNotificacionLeida(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Evento que se ejecuta cuando la ventana se cierra.
        /// Permite ejecutar una acción adicional opcional definida externamente.
        /// </summary>
        /// <param name="e">Datos del evento <see cref="EventArgs"/>.</param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onCerrar?.Invoke();
        }
    }
}

