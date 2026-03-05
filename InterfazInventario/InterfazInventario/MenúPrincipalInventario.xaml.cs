using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

// ✅ Cuando esté unido en main, agrega el using del namespace del Login
// donde está la clsConexion real del equipo
//using Vehículos; // ← cambia esto por el namespace real del proyecto Login

namespace InterfazInventario
{
   
    public class Repuesto : INotifyPropertyChanged
    {
        public int? Producto_ID { get; set; }
        public string? Producto_Nombre { get; set; }
        public string? Producto_Categoria { get; set; }
        public string? Producto_Marca { get; set; }   
        public string? Producto_Modelo { get; set; }
        public int Producto_Cantidad_Actual { get; set; }
        public int Producto_Cantidad_Minima { get; set; }
        public decimal Producto_Precio { get; set; }

        public bool StockBajo => Producto_Cantidad_Actual < Producto_Cantidad_Minima;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    public partial class MenúPrincipalInventario : Window
    {
        private clsConexion _conexion = new clsConexion();

        private ObservableCollection<Repuesto> _listaRepuestos = new ObservableCollection<Repuesto>();
        private ICollectionView _vistaRepuestos;

        private string _filtroCategoria = null;
        private decimal _filtroPrecioMin = 0;
        private decimal _filtroPrecioMax = decimal.MaxValue;
        private bool _filtroStockBajo = false;

        public MenúPrincipalInventario()
        {
            InitializeComponent();
            CargarDatosDesdeDB();
        }


        private void CargarDatosDesdeDB()
        {
            _listaRepuestos.Clear();

            try
            {
                _conexion.Abrir();

                string query = @"
                    SELECT 
                        Producto_ID,
                        Producto_Nombre,
                        Producto_Categoria,
                        ISNULL(Producto_Marca,  '—') AS Producto_Marca,
                        ISNULL(Producto_Modelo, '—') AS Producto_Modelo,
                        Producto_Cantidad_Actual,
                        Producto_Stock_Minimo,
                        Producto_Precio
                    FROM Producto
                    ORDER BY Producto_Nombre";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        _listaRepuestos.Add(new Repuesto
                        {
                            Producto_ID = reader.GetInt32(reader.GetOrdinal("Producto_ID")),
                            Producto_Nombre = reader["Producto_Nombre"].ToString(),
                            Producto_Categoria = reader["Producto_Categoria"].ToString(),
                            Producto_Marca = reader["Producto_Marca"].ToString(),  
                            Producto_Modelo = reader["Producto_Modelo"].ToString(),
                            Producto_Cantidad_Actual = reader.GetInt32(reader.GetOrdinal("Producto_Cantidad_Actual")),
                            Producto_Cantidad_Minima = reader.GetInt32(reader.GetOrdinal("Producto_Stock_Minimo")),
                            Producto_Precio = reader.GetDecimal(reader.GetOrdinal("Producto_Precio"))
                        });
                    }
                }


                var categorias = _listaRepuestos
                    .Select(r => r.Producto_Categoria)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

                categorias.Insert(0, "Todas");
                cmbCategoria.ItemsSource = categorias;
                cmbCategoria.SelectedIndex = 0;


                _vistaRepuestos = CollectionViewSource.GetDefaultView(_listaRepuestos);
                _vistaRepuestos.Filter = AplicarFiltros;
                dgInventario.ItemsSource = _vistaRepuestos;

                ActualizarContador();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar inventario:\n" + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _conexion.Cerrar();
            }
        }


        private bool AplicarFiltros(object item)
        {
            if (item is not Repuesto r) return false;


            string texto = txtBuscar.Text?.Trim().ToLower() ?? "";
            if (!string.IsNullOrEmpty(texto))
            {
                bool coincide =
                    (r.Producto_Nombre ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Categoria ?? "").ToLower().Contains(texto) ||
                    (r.Producto_Marca ?? "").ToLower().Contains(texto) ||  
                    (r.Producto_Modelo ?? "").ToLower().Contains(texto);

                if (!coincide) return false;
            }


            if (_filtroCategoria != null && _filtroCategoria != "Todas")
                if (r.Producto_Categoria != _filtroCategoria) return false;


            if (r.Producto_Precio < _filtroPrecioMin) return false;
            if (r.Producto_Precio > _filtroPrecioMax) return false;


            if (_filtroStockBajo && !r.StockBajo) return false;

            return true;
        }


        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }


        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            popupFiltros.IsOpen = !popupFiltros.IsOpen;
        }


        private void btnAplicarFiltros_Click(object sender, RoutedEventArgs e)
        {
            _filtroCategoria = cmbCategoria.SelectedItem?.ToString();
            _filtroPrecioMin = decimal.TryParse(txtPrecioMin.Text, out decimal pMin) ? pMin : 0;
            _filtroPrecioMax = decimal.TryParse(txtPrecioMax.Text, out decimal pMax) ? pMax : decimal.MaxValue;
            _filtroStockBajo = chkStockBajo.IsChecked == true;

            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }


        private void btnLimpiarFiltros_Click(object sender, RoutedEventArgs e)
        {
            cmbCategoria.SelectedIndex = 0;
            txtPrecioMin.Clear();
            txtPrecioMax.Clear();
            chkStockBajo.IsChecked = false;

            _filtroCategoria = null;
            _filtroPrecioMin = 0;
            _filtroPrecioMax = decimal.MaxValue;
            _filtroStockBajo = false;

            popupFiltros.IsOpen = false;
            _vistaRepuestos?.Refresh();
            ActualizarContador();
        }


        private void dgInventario_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgInventario.SelectedItem is Repuesto seleccionado)
            {
                var ventana = new MainWindow();
                ventana.CargarProductoParaEditar(seleccionado);
                ventana.ShowDialog();

                dgInventario.SelectedItem = null;  
                CargarDatosDesdeDB();               
            }
        }


        private void btnAgregarRepuesto_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new MainWindow();
            ventana.ShowDialog();
            CargarDatosDesdeDB();  
        }

        private void ActualizarContador()
        {
            int total = 0;
            if (_vistaRepuestos != null)
                foreach (var _ in _vistaRepuestos) total++;

            tbTotalItems.Text = $"{total} item{(total != 1 ? "s" : "")}";
        }
    }
}