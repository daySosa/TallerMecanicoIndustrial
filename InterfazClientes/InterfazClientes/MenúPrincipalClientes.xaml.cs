using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static InterfazClientes.MainWindow;

namespace InterfazClientes
{
    /// <summary>
    /// Lógica de interacción para MenúPrincipalClientes.xaml
    /// </summary>
    public partial class MenúPrincipalClientes : Window
    {

        private List<Cliente> _listaClientes = new List<Cliente>();
        private clsConexión _db = new clsConexión();

        public MenúPrincipalClientes()
        {
            InitializeComponent();
            CargarClientes();
        }

        public void CargarClientes()
        {
            _listaClientes.Clear();

            try
            {
                _db.Abrir();
                string sql = "SELECT * FROM Clientes ORDER BY Cliente_Nombre";
                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    _listaClientes.Add(new Cliente
                    {
                        Cliente_DPI = rd["Cliente_DPI"].ToString(),
                        Cliente_Nombre = rd["Cliente_Nombre"].ToString(),
                        Cliente_Apellido = rd["Cliente_Apellido"].ToString(),
                        Cliente_Telefono = rd["Cliente_Telefono"].ToString(),
                        Cliente_Correo = rd["Cliente_Correo"].ToString(),
                        Cliente_Direccion = rd["Cliente_Direccion"].ToString(),
                        Cliente_Activo = (bool)rd["Cliente_Activo"]
                    });
                }
                rd.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar: " + ex.Message);
            }

            RefrescarDataGrid();
        }

        private void btnAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var formulario = new MainWindow();
            formulario.ShowDialog();
            CargarClientes();

            if (formulario.ShowDialog() == true)
            {
                var c = formulario.ClienteResultado;
                GuardarEnDB(c);
                _listaClientes.Add(c);
                RefrescarDataGrid();

                MessageBox.Show($"Cliente {c.Cliente_Nombre} {c.Cliente_Apellido} agregado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GuardarEnDB(Cliente c)
        {
            try
            {
                _db.Abrir();

                string sql = @"INSERT INTO Clientes
                               (Cliente_DPI, Cliente_Nombre, Cliente_Apellido,
                                Cliente_Telefono, Cliente_Correo, Cliente_Direccion, Cliente_Activo)
                               VALUES
                               (@DPI, @Nombre, @Apellido, @Telefono, @Correo, @Direccion, @Activo)";

                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                cmd.Parameters.AddWithValue("@DPI", c.Cliente_DPI);
                cmd.Parameters.AddWithValue("@Nombre", c.Cliente_Nombre);
                cmd.Parameters.AddWithValue("@Apellido", c.Cliente_Apellido);
                cmd.Parameters.AddWithValue("@Telefono", c.Cliente_Telefono);
                cmd.Parameters.AddWithValue("@Correo", c.Cliente_Correo);
                cmd.Parameters.AddWithValue("@Direccion", c.Cliente_Direccion);
                cmd.Parameters.AddWithValue("@Activo", c.Cliente_Activo);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }

        }

        private void RefrescarDataGrid()
        {
            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = _listaClientes;
            tbTotalClientes.Text = $"{_listaClientes.Count} clientes";
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // string filtro = txtBuscar.Text.ToLower();
            // dgClientes.ItemsSource = db.Clientes
            //     .Where(c => c.Cliente_Nombre.ToLower().Contains(filtro)
            //              || c.Cliente_Apellido.ToLower().Contains(filtro)).ToList();
        }

        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
