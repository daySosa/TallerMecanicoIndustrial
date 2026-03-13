using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Órdenes_de_Trabajo
{
    public partial class MenúPrincipalOrdenes : Window
    {
        public class OrdenItem
        {
            public int NumeroOrden { get; set; }
            public string NombreCliente { get; set; } = string.Empty;
            public string Placa { get; set; } = string.Empty;
            public DateTime FechaOrden { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string Prioridad { get; set; } = string.Empty;
            public decimal Precio { get; set; }
        }

        private clsConexion _conexion = new clsConexion();
        private ObservableCollection<OrdenItem> _listaOrdenes = new ObservableCollection<OrdenItem>();
        private ICollectionView _vistaOrdenes;

        private string _filtroEstado = "Todos";
        private string _filtroPrioridad = "Todos";

        public MenúPrincipalOrdenes()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
        }

        private void CargarDatosDesdeDB()
        {
            {
                _listaOrdenes.Clear();

                try
                {
                    _conexion.Abrir();

                    string query = @"
                    SELECT
                        o.Orden_ID AS NumeroOrden,
                        c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCliente,
                        v.Vehiculo_Placa AS Placa,
                        o.Fecha AS FechaOrden,
                        o.Estado,
                        'Normal' AS Prioridad,
                        ISNULL(o.OrdenPrecio_Total, 0) AS Precio
                    FROM Orden_Trabajo o
                    INNER JOIN Cliente c ON o.Cliente_DNI = c.Cliente_DNI
                    INNER JOIN Vehiculo v ON o.Vehiculo_Placa = v.Vehiculo_Placa
                    ORDER BY o.Orden_ID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            _listaOrdenes.Add(new OrdenItem
                            {
                                NumeroOrden = Convert.ToInt32(reader["NumeroOrden"]),
                                NombreCliente = reader["NombreCliente"].ToString(),
                                Placa = reader["Placa"].ToString(),
                                FechaOrden = Convert.ToDateTime(reader["FechaOrden"]),
                                Estado = reader["Estado"].ToString(),
                                Prioridad = reader["Prioridad"].ToString(),
                                Precio = Convert.ToDecimal(reader["Precio"])
                            });
                        }
                    }

                    _vistaOrdenes = CollectionViewSource.GetDefaultView(_listaOrdenes);
                    _vistaOrdenes.Filter = AplicarFiltros;
                    dgOrdenes.ItemsSource = _vistaOrdenes;

                    int pendientes = _listaOrdenes.Count(o => o.Estado == "En Espera");
                    badgeNotif.Badge = pendientes;

                    if (txtContador != null)
                        txtContador.Text = $"{_listaOrdenes.Count} orden(es)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar órdenes:\n" + ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally { _conexion.Cerrar(); }
            }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not OrdenItem o) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";

            bool pasaBusqueda = string.IsNullOrEmpty(texto) ||
                                o.NombreCliente.ToLower().Contains(texto) ||
                                o.Placa.ToLower().Contains(texto) ||
                                o.NumeroOrden.ToString().Contains(texto);

            bool pasaEstado = _filtroEstado == "Todos" || o.Estado == _filtroEstado;
            bool pasaPrioridad = _filtroPrioridad == "Todos" || o.Prioridad == _filtroPrioridad;

            return pasaBusqueda && pasaEstado && pasaPrioridad;
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaOrdenes?.Refresh();
        }

        private void btnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();

            var pendientes = _listaOrdenes
                .Where(o => o.Estado == "En Espera" || o.Prioridad == "Urgente")
                .ToList();

            if (pendientes.Count == 0)
            {
                var vacio = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                };

                vacio.Children.Add(new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = MaterialDesignThemes.Wpf.PackIconKind.PartyPopper,
                    Width = 48,
                    Height = 48,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280"))
                });

                vacio.Children.Add(new TextBlock
                {
                    Text = "Sin notificaciones pendientes",
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 8, 0, 0)
                });

                panelNotificaciones.Children.Add(vacio);
                badgeContadorPopup.Visibility = Visibility.Collapsed;
                return;
            }

            txtContadorPopup.Text = pendientes.Count.ToString();
            badgeContadorPopup.Visibility = Visibility.Visible;

            foreach (var o in pendientes)
            {
                var tarjeta = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#162030")),
                    CornerRadius = new CornerRadius(8),
                    Margin = new Thickness(10, 4, 10, 4),
                    Padding = new Thickness(12, 10, 12, 10)
                };

                var stack = new StackPanel { Orientation = Orientation.Horizontal };

                var icono = new MaterialDesignThemes.Wpf.PackIcon
                {
                    Kind = o.Prioridad == "Urgente"
                        ? MaterialDesignThemes.Wpf.PackIconKind.AlertCircle
                        : MaterialDesignThemes.Wpf.PackIconKind.ClipboardAlert,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                            o.Prioridad == "Urgente" ? "#f44336" : "#F0A500")),
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock
                {
                    Text = o.Prioridad == "Urgente"
                        ? $"Orden #{o.NumeroOrden} — Urgente"
                        : $"Orden #{o.NumeroOrden} en espera",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium
                });
                info.Children.Add(new TextBlock
                {
                    Text = $"{o.NombreCliente} — {o.Placa}",
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")),
                    FontSize = 11
                });

                stack.Children.Add(icono);
                stack.Children.Add(info);
                tarjeta.Child = stack;
                panelNotificaciones.Children.Add(tarjeta);
            }
        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            // pendiente
        }

        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MainWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();
        }
    }
}