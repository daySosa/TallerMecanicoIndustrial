using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vehículos;

namespace Vehículos
{
    public class Vehiculo : INotifyPropertyChanged
    {
        public string Vehiculo_Placa { get; set; }
        public string Vehiculo_Marca { get; set; }
        public string Vehiculo_Modelo { get; set; }
        public int Vehiculo_Año { get; set; }
        public string Vehiculo_Tipo { get; set; }
        public string Vehiculo_Observaciones { get; set; }
        public int Cliente_DNI { get; set; }
        public string Cliente_NombreCompleto { get; set; }
        public bool EstaActivo { get; set; } = true;

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class MainWindow : Window
    {
        private clsConexion _conexion = new clsConexion();
        private string _placaSeleccionada = string.Empty;
        private int _clienteDNI = -1;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void EstablecerCliente(int clienteDNI)
        {
            _clienteDNI = clienteDNI;
            txtClienteDNI.Text = clienteDNI.ToString();
            VerificarClienteEnBD(clienteDNI);
        }

        private void BtnVerificarCliente_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtClienteDNI.Text.Trim(), out int dni) || dni <= 0)
            {
                MostrarClienteError("DNI inválido. Ingresa un número entero positivo.");
                return;
            }
            VerificarClienteEnBD(dni);
        }

        private void VerificarClienteEnBD(int dni)
        {
            try
            {
                _conexion.Abrir();

                string query = @"
                    SELECT Cliente_DNI,
                           Cliente_Nombres + ' ' + Cliente_Apellidos AS NombreCompleto
                    FROM   Cliente
                    WHERE  Cliente_DNI = @DNI";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@DNI", dni);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            _clienteDNI = dni;
                            MostrarClienteOk(reader["NombreCompleto"].ToString());
                        }
                        else
                        {
                            _clienteDNI = -1;
                            MostrarClienteError($"No existe ningún cliente con DNI {dni}.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _clienteDNI = -1;
                MostrarClienteError("Error al consultar: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        private void MostrarClienteOk(string nombreCompleto)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountCheck;
            iconClienteEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
            txtClienteNombre.Text = nombreCompleto;
            txtClienteEstado.Text = "✔ Cliente verificado correctamente";
            txtClienteEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void MostrarClienteError(string mensaje)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountAlert;
            iconClienteEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
            txtClienteNombre.Text = mensaje;
            txtClienteEstado.Text = "✘ Cliente no encontrado";
            txtClienteEstado.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
        }

        private void txtClienteDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (borderClienteInfo != null)
                borderClienteInfo.Visibility = Visibility.Collapsed;
            _clienteDNI = -1;
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
                string.IsNullOrWhiteSpace(txtMarca.Text) ||
                string.IsNullOrWhiteSpace(txtModelo.Text) ||
                !int.TryParse(txtAnio.Text, out int año) ||
                cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("Complete todos los campos obligatorios.",
                    "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (año < 1900 || año > DateTime.Now.Year + 1)
            {
                MessageBox.Show($"El año debe estar entre 1900 y {DateTime.Now.Year + 1}.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_clienteDNI == -1)
            {
                MessageBox.Show("Debes ingresar y verificar el DNI del cliente antes de guardar.",
                    "Cliente requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _conexion.Abrir();

                using (SqlCommand cmd = new SqlCommand("sp_RegistrarVehiculo", _conexion.SqlC))
                {
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Placa", txtPlaca.Text.Trim().ToUpper());
                    cmd.Parameters.AddWithValue("@ClienteDNI", _clienteDNI);
                    cmd.Parameters.AddWithValue("@Marca", txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo", txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Año", año);
                    cmd.Parameters.AddWithValue("@Tipo", (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text)
                        ? (object)DBNull.Value : txtObservaciones.Text.Trim());

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("✅ Vehículo registrado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                this.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show("Error al registrar vehículo:\n" + ex.Message,
                    "Error de base de datos", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error inesperado:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
                string.IsNullOrWhiteSpace(txtMarca.Text) ||
                string.IsNullOrWhiteSpace(txtModelo.Text) ||
                !int.TryParse(txtAnio.Text, out int anio) ||
                cmbTipo.SelectedItem == null)
            {
                MessageBox.Show("Complete todos los campos obligatorios.",
                    "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (anio < 1900 || anio > DateTime.Now.Year + 1)
            {
                MessageBox.Show($"El año debe estar entre 1900 y {DateTime.Now.Year + 1}.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_placaSeleccionada))
            {
                MessageBox.Show("No hay ningún vehículo cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _conexion.Abrir();

                string query = @"
                    UPDATE Vehiculo SET
                        Vehiculo_Marca         = @Marca,
                        Vehiculo_Modelo        = @Modelo,
                        Vehiculo_Año           = @Año,
                        Vehiculo_Tipo          = @Tipo,
                        Vehiculo_Observaciones = @Observaciones
                    WHERE Vehiculo_Placa = @Placa";

                using (SqlCommand cmd = new SqlCommand(query, _conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Marca", txtMarca.Text.Trim());
                    cmd.Parameters.AddWithValue("@Modelo", txtModelo.Text.Trim());
                    cmd.Parameters.AddWithValue("@Año", anio);
                    cmd.Parameters.AddWithValue("@Tipo", (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString());
                    cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(txtObservaciones.Text)
                        ? (object)DBNull.Value : txtObservaciones.Text.Trim());
                    cmd.Parameters.AddWithValue("@Placa", _placaSeleccionada);

                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("✅ Vehículo actualizado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar vehículo:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { _conexion.Cerrar(); }
        }


        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#f44336"));
        }

        public void CargarVehiculoParaEditar(Vehiculo vehiculo)
        {
            _placaSeleccionada = vehiculo.Vehiculo_Placa;
            _clienteDNI = vehiculo.Cliente_DNI;

            txtPlaca.Text = vehiculo.Vehiculo_Placa;
            txtPlaca.IsReadOnly = true;
            txtMarca.Text = vehiculo.Vehiculo_Marca;
            txtModelo.Text = vehiculo.Vehiculo_Modelo;
            txtAnio.Text = vehiculo.Vehiculo_Año.ToString();
            txtObservaciones.Text = vehiculo.Vehiculo_Observaciones;
            txtClienteDNI.Text = vehiculo.Cliente_DNI.ToString();
            txtClienteDNI.IsReadOnly = true;

            MostrarClienteOk(vehiculo.Cliente_NombreCompleto);

            foreach (ComboBoxItem item in cmbTipo.Items)
            {
                if (item.Content.ToString() == vehiculo.Vehiculo_Tipo)
                {
                    cmbTipo.SelectedItem = item;
                    break;
                }
            }

            toggleActivo.IsChecked = vehiculo.EstaActivo;
        }

        private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPlaca.IsReadOnly) return;
            int caret = txtPlaca.CaretIndex;
            txtPlaca.Text = txtPlaca.Text.ToUpper();
            txtPlaca.CaretIndex = caret;
        }

        private void LimpiarFormulario()
        {
            txtPlaca.Clear();
            txtPlaca.IsReadOnly = false;
            txtMarca.Clear();
            txtModelo.Clear();
            txtAnio.Clear();
            txtObservaciones.Clear();
            cmbTipo.SelectedIndex = -1;
            toggleActivo.IsChecked = true;
            txtClienteDNI.Clear();
            txtClienteDNI.IsReadOnly = false;
            borderClienteInfo.Visibility = Visibility.Collapsed;
            _placaSeleccionada = string.Empty;
            _clienteDNI = -1;
        }
    }
}