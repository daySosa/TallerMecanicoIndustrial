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
        private clsConexión _db = new clsConexión();

        public MenúPrincipalClientes()
        {
            InitializeComponent();
            CargarClientes();
        }

        // ═══════════════════════════════════════════
        // 1. CARGAR CLIENTES DESDE BD
        //    Tabla correcta: Cliente (sin 's')
        // ═══════════════════════════════════════════
        public void CargarClientes()
        {
            _listaClientes.Clear();

            try
            {
                _db.Abrir();

                // ✔ Nombre correcto de la tabla y sus columnas según tu BD
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

        // ═══════════════════════════════════════════
        // 2. BOTÓN AGREGAR CLIENTE
        //    ✔ Una sola llamada a ShowDialog()
        //    ✔ Guarda en BD si el usuario confirmó
        // ═══════════════════════════════════════════
        private void btnAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            var formulario = new MainWindow();

            // Una sola llamada — bloquea hasta que el formulario cierre
            bool? resultado = formulario.ShowDialog();

            if (resultado == true && formulario.ClienteResultado != null)
            {
                var c = formulario.ClienteResultado;
                GuardarEnDB(c);
                CargarClientes(); // recarga desde BD para reflejar el nuevo registro
            }
        }

        // ═══════════════════════════════════════════
        // 3. GUARDAR EN BD
        //    Columnas correctas según tu esquema
        // ═══════════════════════════════════════════
        private void GuardarEnDB(Cliente c)
        {
            try
            {
                _db.Abrir();

                string sql = @"
                    INSERT INTO Cliente
                        (Cliente_Nombres, Cliente_Apellidos,
                         Cliente_TelefonoPrincipal, Cliente_Email, Cliente_Direccion)
                    VALUES
                        (@Nombres, @Apellidos, @Telefono, @Email, @Direccion)";

                SqlCommand cmd = new SqlCommand(sql, _db.SqlC);
                cmd.Parameters.AddWithValue("@Nombres", c.Cliente_Nombre);
                cmd.Parameters.AddWithValue("@Apellidos", c.Cliente_Apellido);
                cmd.Parameters.AddWithValue("@Telefono", c.Cliente_Telefono);
                cmd.Parameters.AddWithValue("@Email", c.Cliente_Correo);
                cmd.Parameters.AddWithValue("@Direccion", c.Cliente_Direccion);

                cmd.ExecuteNonQuery();

                MessageBox.Show($"✅ Cliente {c.Cliente_Nombre} {c.Cliente_Apellido} guardado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
            finally { _db.Cerrar(); }
        }

        // ═══════════════════════════════════════════
        // 4. SELECCIÓN EN DATAGRID → editar cliente
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // 5. BUSCADOR EN TIEMPO REAL
        // ═══════════════════════════════════════════
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

        // ═══════════════════════════════════════════
        // 6. REFRESCAR DATAGRID
        // ═══════════════════════════════════════════
        private void RefrescarDataGrid()
        {
            dgClientes.ItemsSource = null;
            dgClientes.ItemsSource = _listaClientes;
            tbTotalClientes.Text = $"{_listaClientes.Count} clientes";
        }
    }
}