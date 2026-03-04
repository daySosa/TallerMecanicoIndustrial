using System.ComponentModel;
using System.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Vehículos;

namespace Vehículos
{
    // ═══════════════════════════════════════════════════════════════
    // MODELO — clase Vehiculo definida aquí para que tanto
    // MainWindow como MenuPrincipalVehiculos la compartan
    // dentro del mismo namespace.
    // ═══════════════════════════════════════════════════════════════
    public class Vehiculo : INotifyPropertyChanged
{
    public string Vehiculo_Placa { get; set; }
    public string Vehiculo_Marca { get; set; }
    public string Vehiculo_Modelo { get; set; }
    public int Vehiculo_Año { get; set; }
    public string Vehiculo_Tipo { get; set; }
    public string Vehiculo_Observaciones { get; set; }

    // Datos del cliente vinculado (vienen de Vista_Vehiculo_Con_Cliente)
    public int Cliente_DNI { get; set; }
    public string Cliente_NombreCompleto { get; set; }

    // Estado visual del vehículo
    public bool EstaActivo { get; set; } = true;

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// ═══════════════════════════════════════════════════════════════
// CODE-BEHIND — MainWindow (Formulario de Vehículo)
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private clsConexion _conexion = new clsConexion();

    // Placa del vehículo cargado para editar. Vacío = modo "Nuevo Vehículo".
    private string _placaSeleccionada = string.Empty;

    // Cliente_DNI inyectado desde el menú principal (modo nuevo)
    // o recuperado del objeto Vehiculo (modo edición).
    private int _clienteDNI = -1;

    public MainWindow()
    {
        InitializeComponent();
    }

    // ═══════════════════════════════════════════
    // MODO NUEVO: el menú principal llama este
    // método ANTES de ShowDialog() para pasar
    // el cliente al que pertenecerá el vehículo.
    //
    // Ejemplo desde MenuPrincipalVehiculos:
    //   var ventana = new MainWindow();
    //   ventana.EstablecerCliente(clienteSeleccionado.Cliente_DNI);
    //   ventana.ShowDialog();
    // ═══════════════════════════════════════════
    public void EstablecerCliente(int clienteDNI)
    {
        _clienteDNI = clienteDNI;
    }

    // ═══════════════════════════════════════════
    // 1. GUARDAR VEHÍCULO → INSERT vía stored procedure
    //    ✔ Usa sp_RegistrarVehiculo (procedimiento almacenado)
    //    ✔ El Cliente_DNI viene del menú principal, no del formulario
    //    ✔ Valida placa duplicada y cliente existente (lo hace el SP)
    //    ✔ Valida campos obligatorios antes de persistir
    // ═══════════════════════════════════════════
    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            !int.TryParse(txtAnio.Text, out int año) ||
            cmbTipo.SelectedItem == null)
        {
            MessageBox.Show("Complete todos los campos obligatorios.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validación de rango de año (igual al CHECK de la BD)
        if (año < 1900 || año > DateTime.Now.Year + 1)
        {
            MessageBox.Show(
                $"El año debe estar entre 1900 y {DateTime.Now.Year + 1}.",
                "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // El Cliente_DNI debe haber sido inyectado desde el menú principal
        if (_clienteDNI == -1)
        {
            MessageBox.Show(
                "No se ha asociado ningún cliente a este vehículo.\n" +
                "Selecciona un cliente desde el menú principal antes de continuar.",
                "Cliente requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _conexion.Abrir();

            using (SqlCommand cmd = new SqlCommand("sp_RegistrarVehiculo", _conexion.SqlC))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Placa",
                    txtPlaca.Text.Trim().ToUpper());
                cmd.Parameters.AddWithValue("@ClienteDNI", _clienteDNI);
                cmd.Parameters.AddWithValue("@Marca",
                    txtMarca.Text.Trim());
                cmd.Parameters.AddWithValue("@Modelo",
                    txtModelo.Text.Trim());
                cmd.Parameters.AddWithValue("@Año", año);
                cmd.Parameters.AddWithValue("@Tipo",
                    (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString());
                cmd.Parameters.AddWithValue("@Observaciones",
                    string.IsNullOrWhiteSpace(txtObservaciones.Text)
                        ? (object)DBNull.Value
                        : txtObservaciones.Text.Trim());

                cmd.ExecuteNonQuery();
            }

            MessageBox.Show("✅ Vehículo registrado correctamente.",
                "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            LimpiarFormulario();
            this.Close();
        }
        catch (SqlException ex)
        {
            // El SP lanza RAISERROR: placa duplicada o cliente inexistente
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

    // ═══════════════════════════════════════════
    // 2. ACTUALIZAR VEHÍCULO → UPDATE directo
    //    ✔ La placa (PK) y el Cliente_DNI no se modifican
    //    ✔ Solo se actualizan los campos editables del formulario
    // ═══════════════════════════════════════════
    private void BtnActualizar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPlaca.Text) ||
            string.IsNullOrWhiteSpace(txtMarca.Text) ||
            string.IsNullOrWhiteSpace(txtModelo.Text) ||
            !int.TryParse(txtAnio.Text, out int anio) ||
            cmbTipo.SelectedItem == null)
        {
            MessageBox.Show("Complete todos los campos obligatorios.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (anio < 1900 || anio > DateTime.Now.Year + 1)
        {
            MessageBox.Show(
                $"El año debe estar entre 1900 y {DateTime.Now.Year + 1}.",
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
                cmd.Parameters.AddWithValue("@Marca",
                    txtMarca.Text.Trim());
                cmd.Parameters.AddWithValue("@Modelo",
                    txtModelo.Text.Trim());
                cmd.Parameters.AddWithValue("@Año", anio);
                cmd.Parameters.AddWithValue("@Tipo",
                    (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString());
                cmd.Parameters.AddWithValue("@Observaciones",
                    string.IsNullOrWhiteSpace(txtObservaciones.Text)
                        ? (object)DBNull.Value
                        : txtObservaciones.Text.Trim());
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

    // ═══════════════════════════════════════════
    // 3. CANCELAR → regresa al menú principal
    // ═══════════════════════════════════════════
    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        MenúPrincipalVehículos ventana = new MenúPrincipalVehículos();
        ventana.Show();
        this.Close();
    }

    // ═══════════════════════════════════════════
    // 4. TOGGLE ESTADO DEL VEHÍCULO
    // ═══════════════════════════════════════════
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

    // ═══════════════════════════════════════════
    // 5. CARGAR DATOS PARA EDITAR
    //    Llamado desde MenuPrincipalVehiculos al
    //    seleccionar una fila del DataGrid.
    //    El Cliente_DNI viaja dentro del objeto Vehiculo.
    // ═══════════════════════════════════════════
    public void CargarVehiculoParaEditar(Vehiculo vehiculo)
    {
        _placaSeleccionada = vehiculo.Vehiculo_Placa;

        // Guardamos el DNI internamente para no perderlo en el UPDATE
        _clienteDNI = vehiculo.Cliente_DNI;

        // La placa es PK: se muestra pero no se puede editar
        txtPlaca.Text = vehiculo.Vehiculo_Placa;
        txtPlaca.IsReadOnly = true;

        txtMarca.Text = vehiculo.Vehiculo_Marca;
        txtModelo.Text = vehiculo.Vehiculo_Modelo;
        txtAnio.Text = vehiculo.Vehiculo_Año.ToString();
        txtObservaciones.Text = vehiculo.Vehiculo_Observaciones;

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

    // ═══════════════════════════════════════════
    // 6. txtPlaca_TextChanged
    //    Convierte a mayúsculas automáticamente
    // ═══════════════════════════════════════════
    private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (txtPlaca.IsReadOnly) return;

        int caret = txtPlaca.CaretIndex;
        txtPlaca.Text = txtPlaca.Text.ToUpper();
        txtPlaca.CaretIndex = caret;
    }

    // ═══════════════════════════════════════════
    // 7. LIMPIAR FORMULARIO
    // ═══════════════════════════════════════════
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

        _placaSeleccionada = string.Empty;
        _clienteDNI = -1;
    }
}
}
