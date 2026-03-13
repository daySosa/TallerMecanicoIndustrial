using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Vehículos
{
    public partial class MenúPrincipalVehículos : Window
    {
        private clsConexion _conexion = new clsConexion();
        private ObservableCollection<Vehiculo> _listaVehiculos = new ObservableCollection<Vehiculo>();
        private ICollectionView _vistaVehiculos;

        private string _filtroMarca = "";
        private string _filtroAnio = "";
        private string _filtroTipo = "Todos";
        private string _filtroEstado = "Todos";

        public MenúPrincipalVehículos()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
        }

        private void CargarDatosDesdeDB()
        {
            _listaVehiculos.Clear();

            try
            {
                _conexion.Abrir();

                string query = @"
                    SELECT
                        v.Vehiculo_Placa,
                        v.Vehiculo_Marca,
                        v.Vehiculo_Modelo,
                        v.Vehiculo_Año,
                        v.Vehiculo_Tipo,
                        ISNULL(v.Vehiculo_Observaciones, '') AS Vehiculo_Observaciones,
                        v.Vehiculo_Activo,
                        c.Cliente_DNI,
                        c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS Cliente_NombreCompleto
                    FROM Vehiculo v
                    INNER JOIN Cliente c ON v.Cliente_DNI = c.Cliente_DNI
                    ORDER BY v.Vehiculo_Placa";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _listaVehiculos.Add(new Vehiculo
                        {
                            Vehiculo_Placa = reader["Vehiculo_Placa"].ToString(),
                            Vehiculo_Marca = reader["Vehiculo_Marca"].ToString(),
                            Vehiculo_Modelo = reader["Vehiculo_Modelo"].ToString(),
                            Vehiculo_Año = Convert.ToInt32(reader["Vehiculo_Año"]),
                            Vehiculo_Tipo = reader["Vehiculo_Tipo"].ToString(),
                            Vehiculo_Observaciones = reader["Vehiculo_Observaciones"].ToString(),
                            Cliente_DNI = Convert.ToInt32(reader["Cliente_DNI"]),
                            Cliente_NombreCompleto = reader["Cliente_NombreCompleto"].ToString(),
                            EstaActivo = reader["Vehiculo_Activo"] != DBNull.Value
                                         && Convert.ToBoolean(reader["Vehiculo_Activo"])
                        });
                    }
                }

                _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                _vistaVehiculos.Filter = AplicarFiltros;
                dgVehiculos.ItemsSource = _vistaVehiculos;

                badgeNotif.Badge = _listaVehiculos.Count(v => !v.EstaActivo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar vehículos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        private bool AplicarFiltros(object item)
        {
            if (item is not Vehiculo v) return false;

            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";

            bool pasaBusqueda = string.IsNullOrEmpty(texto) ||
                                (v.Vehiculo_Placa ?? "").ToLower().Contains(texto) ||
                                (v.Vehiculo_Marca ?? "").ToLower().Contains(texto) ||
                                (v.Vehiculo_Modelo ?? "").ToLower().Contains(texto) ||
                                (v.Vehiculo_Tipo ?? "").ToLower().Contains(texto) ||
                                (v.Cliente_NombreCompleto ?? "").ToLower().Contains(texto);

            bool pasaMarca = string.IsNullOrEmpty(_filtroMarca) ||
                             (v.Vehiculo_Marca ?? "").ToLower().Contains(_filtroMarca.ToLower());

            bool pasaAnio = string.IsNullOrEmpty(_filtroAnio) ||
                            v.Vehiculo_Año.ToString() == _filtroAnio;

            bool pasaTipo = _filtroTipo == "Todos" || v.Vehiculo_Tipo == _filtroTipo;

            bool pasaEstado = _filtroEstado == "Todos" ||
                              (_filtroEstado == "Activo" && v.EstaActivo) ||
                              (_filtroEstado == "Inactivo" && !v.EstaActivo);

            return pasaBusqueda && pasaMarca && pasaAnio && pasaTipo && pasaEstado;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
        }

        private void BtnNotificaciones_Click(object sender, RoutedEventArgs e)
        {
            if (!popupNotificaciones.IsOpen)
                CargarNotificacionesEnPopup();
            popupNotificaciones.IsOpen = !popupNotificaciones.IsOpen;
        }

        private void CargarNotificacionesEnPopup()
        {
            panelNotificaciones.Children.Clear();

            var inactivos = _listaVehiculos.Where(v => !v.EstaActivo).ToList();

            if (inactivos.Count == 0)
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

            txtContadorPopup.Text = inactivos.Count.ToString();
            badgeContadorPopup.Visibility = Visibility.Visible;

            foreach (var v in inactivos)
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
                    Kind = MaterialDesignThemes.Wpf.PackIconKind.CarOff,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#f44336")),
                    Width = 20,
                    Height = 20,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                var info = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                info.Children.Add(new TextBlock
                {
                    Text = $"Vehículo inactivo: {v.Vehiculo_Placa}",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Medium
                });
                info.Children.Add(new TextBlock
                {
                    Text = $"{v.Vehiculo_Marca} {v.Vehiculo_Modelo} — {v.Cliente_NombreCompleto}",
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

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            // pendiente
        }

        private void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVehiculos.SelectedItem is Vehiculo seleccionado)
            {
                var ventana = new MainWindow();
                ventana.CargarVehiculoParaEditar(seleccionado);
                ventana.ShowDialog();

                dgVehiculos.SelectedItem = null;
                CargarDatosDesdeDB();
            }
        }

        private void BtnNuevoVehiculo_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MainWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();
        }
    }
}