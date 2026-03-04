using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Vehículos
{
    /// <summary>
    /// Lógica de interacción para MenúPrincipalVehículos.xaml
    /// </summary>
    public partial class MenúPrincipalVehículos : Window
    {
        private clsConexion _conexion = new clsConexion();

        // Colección observable que alimenta el DataGrid
        private ObservableCollection<Vehiculo> _listaVehiculos = new ObservableCollection<Vehiculo>();
        private ICollectionView _vistaVehiculos;

        public MenúPrincipalVehículos()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
        }

        // ═══════════════════════════════════════════
        // 1. CARGAR DATOS DESDE AZURE
        //    Usa Vista_Vehiculo_Con_Cliente para traer
        //    vehículo + cliente en un solo query.
        // ═══════════════════════════════════════════
        private void CargarDatosDesdeDB()
        {
            _listaVehiculos.Clear();

            try
            {
                _conexion.Abrir();

                string query = @"
                    SELECT
                        Vehiculo_Placa,
                        Vehiculo_Marca,
                        Vehiculo_Modelo,
                        Vehiculo_Año,
                        Vehiculo_Tipo,
                        ISNULL(Vehiculo_Observaciones, '') AS Vehiculo_Observaciones,
                        Cliente_DNI,
                        Cliente_Nombres + ' ' + Cliente_Apellidos AS Cliente_NombreCompleto
                    FROM Vista_Vehiculo_Con_Cliente
                    ORDER BY Vehiculo_Placa";

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
                            EstaActivo = true
                        });
                    }
                }

                // Vincula la colección al DataGrid con soporte de filtros
                _vistaVehiculos = CollectionViewSource.GetDefaultView(_listaVehiculos);
                _vistaVehiculos.Filter = AplicarFiltros;
                dgVehiculos.ItemsSource = _vistaVehiculos;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar vehículos:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _conexion.Cerrar();
            }
        }

        // ═══════════════════════════════════════════
        // 2. FILTRO POR BUSCADOR
        //    Busca en tiempo real por placa, marca,
        //    modelo, tipo y nombre del cliente.
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // 3. BUSCADOR EN TIEMPO REAL
        //    Evento TextChanged del txtBuscar en el XAML
        // ═══════════════════════════════════════════
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaVehiculos?.Refresh();
        }

        // ═══════════════════════════════════════════
        // 4. SELECCIÓN EN DATAGRID → abrir edición
        //    Al hacer clic en una fila se carga el
        //    vehículo en el formulario MainWindow.
        // ═══════════════════════════════════════════
        private void dgVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgVehiculos.SelectedItem is Vehiculo seleccionado)
            {
                var ventana = new MainWindow();
                ventana.CargarVehiculoParaEditar(seleccionado);
                ventana.ShowDialog();

                dgVehiculos.SelectedItem = null;  // limpia la selección al volver
                CargarDatosDesdeDB();              // refresca el grid con los cambios
            }
        }

        // ═══════════════════════════════════════════
        // 5. BOTÓN NUEVO VEHÍCULO
        //    Abre el formulario MainWindow vacío.
        // ═══════════════════════════════════════════
        private void BtnNuevaOrden_Click(object sender, RoutedEventArgs e)
        {
            MainWindow ventana = new MainWindow();
            ventana.Show();
            this.Close();
        }
    }
}