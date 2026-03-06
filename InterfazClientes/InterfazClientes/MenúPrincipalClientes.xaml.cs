using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace InterfazClientes
{
    public partial class MenúPrincipalClientes : Window
    {
        private List<Cliente> _listaClientes = new List<Cliente>();
        private clsConexiónClie _db = new clsConexiónClie();

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

                string sql = @"
                    SELECT Cliente_DNI,
                           Cliente_Nombres,
                           Cliente_Apellidos,
                           Cliente_TelefonoPrincipal,
                           Cliente_Email,
                           Cliente_Direccion
                    FROM   Cliente
                    ORDER  BY Cliente_Nombres";

                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                SqlDataReader rd = cmd.ExecuteReader();

                while (rd.Read())
                {
                    _listaClientes.Add(new Cliente
                    {
                        Cliente_DPI = rd["Cliente_DNI"].ToString(),
                        Cliente_Nombre = rd["Cliente_Nombres"].ToString(),
                        Cliente_Apellido = rd["Cliente_Apellidos"].ToString(),
                        Cliente_Telefono = rd["Cliente_TelefonoPrincipal"].ToString(),
                        Cliente_Correo = rd["Cliente_Email"].ToString(),
                        Cliente_Direccion = rd["Cliente_Direccion"].ToString(),
                        Cliente_Activo = true
                    });
                }
                rd.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar: " + ex.Message);
            }
            finally { _db.Cerrar(); }

            RefrescarDataGrid();
        }


        private void btnAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var formulario = new MainWindow();


            bool? resultado = formulario.ShowDialog();

            if (resultado == true && formulario.ClienteResultado != null)
            {
                var c = formulario.ClienteResultado;
                GuardarEnDB(c);
                CargarClientes(); 
            }
        }


        private void GuardarEnDB(Cliente c)
        {
            try
            {
                _db.Abrir();

                string sql = @"
            INSERT INTO Cliente
                (Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                 Cliente_TelefonoPrincipal, Cliente_Email,
                 Cliente_Direccion, Cliente_Activo)
            VALUES
                (@DNI, @Nombres, @Apellidos,
                 @Telefono, @Email,
                 @Direccion, @Activo)";

                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                cmd.Parameters.AddWithValue("@DNI", c.Cliente_DPI);  // ✔ string directo
                cmd.Parameters.AddWithValue("@Nombres", c.Cliente_Nombre);
                cmd.Parameters.AddWithValue("@Apellidos", c.Cliente_Apellido);
                cmd.Parameters.AddWithValue("@Telefono", c.Cliente_Telefono);
                cmd.Parameters.AddWithValue("@Email", c.Cliente_Correo);
                cmd.Parameters.AddWithValue("@Direccion", c.Cliente_Direccion);
                cmd.Parameters.AddWithValue("@Activo", c.Cliente_Activo ? 1 : 0);

                cmd.ExecuteNonQuery();

                MessageBox.Show($"✅ Cliente {c.Cliente_Nombre} {c.Cliente_Apellido} guardado.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
            finally { _db.Cerrar(); }
        }


        private void dgClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgClientes.SelectedItem is Cliente seleccionado)
            {
                var formulario = new MainWindow();
                formulario.CargarClienteParaEditar(seleccionado);
                formulario.ShowDialog();

                dgClientes.SelectedItem = null;
                CargarClientes();
            }
        }


        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = txtBuscar.Text.Trim().ToLower();

            var filtrados = _listaClientes.FindAll(c =>
                (c.Cliente_Nombre ?? "").ToLower().Contains(filtro) ||
                (c.Cliente_Apellido ?? "").ToLower().Contains(filtro) ||
                (c.Cliente_DPI ?? "").ToLower().Contains(filtro) ||
                (c.Cliente_Telefono ?? "").ToLower().Contains(filtro));

            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = filtrados;
        }


        private void RefrescarDataGrid()
        {
            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = _listaClientes;
            tbTotalClientes.Text = $"{_listaClientes.Count} clientes";
        }
    }
}