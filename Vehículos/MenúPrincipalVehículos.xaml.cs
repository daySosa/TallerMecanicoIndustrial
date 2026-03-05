using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
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
                            Vehiculo_Año = reader.GetInt32(reader.GetOrdinal("Vehiculo_Año")),
                            Vehiculo_Tipo = reader["Vehiculo_Tipo"].ToString(),
                            Vehiculo_Observaciones = reader["Vehiculo_Observaciones"].ToString(),
                            Cliente_DNI = reader.GetInt32(reader.GetOrdinal("Cliente_DNI")),
                            Cliente_NombreCompleto = reader["Cliente_NombreCompleto"].ToString(),

                            EstaActivo = reader["Vehiculo_Activo"] != DBNull.Value
                                         && (bool)reader["Vehiculo_Activo"]
                        });
                    }
                }

                _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                _vistaVehiculos.Filter = AplicarFiltros;
                dgVehiculos.ItemsSource = _vistaVehiculos;
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
            if (string.IsNullOrEmpty(texto)) return true;

            return (v.Vehiculo_Placa ?? "").ToLower().Contains(texto) ||
                   (v.Vehiculo_Marca ?? "").ToLower().Contains(texto) ||
                   (v.Vehiculo_Modelo ?? "").ToLower().Contains(texto) ||
                   (v.Vehiculo_Tipo ?? "").ToLower().Contains(texto) ||
                   (v.Cliente_NombreCompleto ?? "").ToLower().Contains(texto);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
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