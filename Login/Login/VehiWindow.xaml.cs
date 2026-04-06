using Login.Clases;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Vehículos
{
    /// <summary>
    /// Representa un vehículo registrado en el sistema.
    /// Implementa <see cref="INotifyPropertyChanged"/> para soportar
    /// enlace de datos reactivo en la interfaz.
    /// </summary>
    public class Vehiculo : INotifyPropertyChanged
    {
        /// <summary>Obtiene o establece la placa del vehículo.</summary>
        public string? Vehiculo_Placa { get; set; }

        /// <summary>Obtiene o establece la marca del vehículo.</summary>
        public string? Vehiculo_Marca { get; set; }

        /// <summary>Obtiene o establece el modelo del vehículo.</summary>
        public string? Vehiculo_Modelo { get; set; }

        /// <summary>Obtiene o establece el año de fabricación del vehículo.</summary>
        public int Vehiculo_Año { get; set; }

        /// <summary>Obtiene o establece el tipo de vehículo (Auto, Moto, Camión, etc.).</summary>
        public string? Vehiculo_Tipo { get; set; }

        /// <summary>Obtiene o establece las observaciones adicionales del vehículo.</summary>
        public string? Vehiculo_Observaciones { get; set; }

        /// <summary>Obtiene o establece el DNI del cliente propietario del vehículo.</summary>
        public string Cliente_DNI { get; set; }

        /// <summary>Obtiene o establece el nombre completo del cliente propietario.</summary>
        public string? Cliente_NombreCompleto { get; set; }

        /// <summary>
        /// Obtiene o establece si el vehículo está activo en el sistema.
        /// El valor predeterminado es <c>true</c>.
        /// </summary>
        public bool EstaActivo { get; set; } = true;

        /// <summary>
        /// Evento que se dispara cuando una propiedad del vehículo cambia de valor.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Notifica a los suscriptores que una propiedad ha cambiado.
        /// </summary>
        /// <param name="name">Nombre de la propiedad que cambió.</param>
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Ventana encargada del registro y actualización de vehículos en el sistema.
    /// Permite agregar nuevos vehículos, editar los existentes y verificar
    /// el cliente propietario mediante su DNI.
    /// </summary>
    public partial class VehiWindow : Window
    {
        /// <summary>
        /// Instancia para consultas a la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// Placa del vehículo que se encuentra en edición actualmente.
        /// Permanece vacía cuando se está registrando un nuevo vehículo.
        /// </summary>
        private string _placaSeleccionada = string.Empty;

        /// <summary>
        /// DNI del cliente verificado y asociado al vehículo en el formulario.
        /// Permanece vacío si el cliente no ha sido verificado correctamente.
        /// </summary>
        private string _clienteDNI = string.Empty;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="VehiWindow"/>.
        /// Deshabilita el botón de actualizar al inicio, ya que se requiere
        /// cargar un vehículo existente para habilitarlo.
        /// </summary>
        public VehiWindow()
        {
            InitializeComponent();
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
        }

        /// <summary>
        /// Restringe el campo de año para aceptar únicamente caracteres numéricos.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtAnio_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>
        /// Restringe el campo de DNI del cliente para aceptar únicamente caracteres numéricos.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtClienteDNI_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
        }

        /// <summary>
        /// Restringe el campo de placa para aceptar únicamente caracteres alfanuméricos.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void txtPlaca_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");
        }

        /// <summary>
        /// Maneja el evento de cambio de texto en el campo de DNI del cliente.
        /// Si el campo tiene el foco y su contenido difiere del DNI verificado,
        /// oculta el panel de información del cliente y limpia <see cref="_clienteDNI"/>
        /// para requerir una nueva verificación.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de cambio de texto.</param>
        private void txtClienteDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (borderClienteInfo != null && txtClienteDNI.IsFocused && txtClienteDNI.Text != _clienteDNI)
            {
                borderClienteInfo.Visibility = Visibility.Collapsed;
                _clienteDNI = string.Empty;
            }
        }

        /// <summary>
        /// Establece el DNI del cliente en el campo de texto y lanza automáticamente
        /// la verificación en la base de datos. Usado para precargar un cliente
        /// desde otra ventana.
        /// </summary>
        /// <param name="clienteDNI">DNI del cliente a establecer y verificar.</param>
        public void EstablecerCliente(int clienteDNI)
        {
            txtClienteDNI.Text = clienteDNI.ToString();
            VerificarClienteEnBD(clienteDNI.ToString());
        }

        /// <summary>
        /// Maneja el evento Click del botón Verificar Cliente.
        /// Valida el formato del DNI ingresado y consulta su existencia
        /// en la base de datos mediante <see cref="VerificarClienteEnBD"/>.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnVerificarCliente_Click(object sender, RoutedEventArgs e)
        {
            string dni = txtClienteDNI.Text.Trim();

            if (!clsValidacionesVehiculo.ValidarFormatoDNICliente(dni))
            {
                txtClienteDNI.Focus();
                return;
            }

            VerificarClienteEnBD(dni);
        }

        /// <summary>
        /// Consulta la base de datos para verificar si existe un cliente con el DNI indicado.
        /// Si se encuentra, guarda el DNI en <see cref="_clienteDNI"/> y muestra el resultado
        /// con <see cref="MostrarClienteOk"/>; de lo contrario, limpia el DNI
        /// y muestra el error con <see cref="MostrarClienteError"/>.
        /// </summary>
        /// <param name="dni">DNI del cliente a verificar.</param>
        private void VerificarClienteEnBD(string dni)
        {
            try
            {
                var res = _db.VerificarClienteDNI(dni);
                if (res.existe)
                {
                    _clienteDNI = dni;
                    MostrarClienteOk(res.nombre);
                }
                else
                {
                    _clienteDNI = string.Empty;
                    MostrarClienteError($"No existe ningún cliente con DNI {dni}.");
                }
            }
            catch (Exception ex)
            {
                _clienteDNI = string.Empty;
                MostrarClienteError(ex.Message);
            }
        }

        /// <summary>
        /// Muestra el panel de información del cliente con ícono y texto en verde,
        /// indicando que el cliente fue verificado correctamente.
        /// </summary>
        /// <param name="nombreCompleto">Nombre completo del cliente verificado.</param>
        private void MostrarClienteOk(string nombreCompleto)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountCheck;
            iconClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            txtClienteNombre.Text = nombreCompleto;
            txtClienteEstado.Text = "✔ Cliente verificado correctamente";
            txtClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        /// <summary>
        /// Muestra el panel de información del cliente con ícono y texto en rojo,
        /// indicando que el cliente no fue encontrado o se produjo un error.
        /// </summary>
        /// <param name="mensaje">Mensaje de error a mostrar en el panel.</param>
        private void MostrarClienteError(string mensaje)
        {
            borderClienteInfo.Visibility = Visibility.Visible;
            iconClienteEstado.Kind = MaterialDesignThemes.Wpf.PackIconKind.AccountAlert;
            iconClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            txtClienteNombre.Text = mensaje;
            txtClienteEstado.Text = "✘ Cliente no encontrado";
            txtClienteEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
        }

        /// <summary>
        /// Ejecuta todas las validaciones compartidas entre guardar y actualizar:
        /// campos vacíos, formato y longitud de placa, formato según tipo de vehículo,
        /// placa reservada, marca, modelo, año, tipo, observaciones y DNI del cliente.
        /// Enfoca el campo con error y retorna <c>false</c> ante la primera falla.
        /// </summary>
        /// <param name="año">Año del vehículo parseado si la validación es exitosa.</param>
        /// <returns><c>true</c> si todos los campos son válidos; de lo contrario, <c>false</c>.</returns>
        private bool ValidarCamposComunes(out int año)
        {
            año = 0;

            // ── CAMPOS VACIOS ────────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarFormularioVacio(
                txtPlaca.Text, txtMarca.Text, txtModelo.Text,
                txtAnio.Text, txtClienteDNI.Text)) return false;

            // ── PLACA ────────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarPlacaNoNula(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarPlacaSoloAlfanumerico(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarLongitudPlaca(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            string tipoStr = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

            if (!clsValidacionesVehiculo.ValidarFormatoPlacaSegunTipo(txtPlaca.Text, tipoStr))
            { txtPlaca.Focus(); return false; }

            if (!clsValidacionesVehiculo.ValidarPlacaNoReservada(txtPlaca.Text))
            { txtPlaca.Focus(); return false; }

            // ── MARCA ────────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarMarca(txtMarca.Text))
            { txtMarca.Focus(); return false; }

            // ── MODELO ───────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarModelo(txtModelo.Text))
            { txtModelo.Focus(); return false; }

            // ── AÑO ──────────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarAnioVehiculo(txtAnio.Text, out año))
            { txtAnio.Focus(); return false; }

            // ── TIPO ─────────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarTipoVehiculo(cmbTipo.SelectedItem))
            { cmbTipo.Focus(); return false; }

            // ── OBSERVACIONES ────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarObservaciones(txtObservaciones.Text))
            { txtObservaciones.Focus(); return false; }

            // ── CLIENTE ──────────────────────────────────────────────
            if (!clsValidacionesVehiculo.ValidarClienteDNI(txtClienteDNI.Text, _clienteDNI))
            { txtClienteDNI.Focus(); return false; }

            return true;
        }

        /// <summary>
        /// Ejecuta las validaciones necesarias para registrar un nuevo vehículo.
        /// Invoca <see cref="ValidarCamposComunes"/> y además verifica que
        /// la placa no esté duplicada en la base de datos.
        /// </summary>
        /// <param name="año">Año del vehículo parseado si la validación es exitosa.</param>
        /// <returns><c>true</c> si todos los campos son válidos; de lo contrario, <c>false</c>.</returns>
        private bool ValidarCamposGuardar(out int año)
        {
            año = 0;
            if (!ValidarCamposComunes(out año)) return false;

            if (!clsValidacionesVehiculo.ValidarPlacaNoDuplicada(txtPlaca.Text, p => _db.ExistePlaca(p)))
            { txtPlaca.Focus(); return false; }

            return true;
        }

        /// <summary>
        /// Maneja el evento Click del botón Guardar.
        /// Valida el formulario mediante <see cref="ValidarCamposGuardar"/>,
        /// construye el objeto de datos del vehículo y lo persiste como nuevo
        /// registro en la base de datos. Cierra la ventana si la operación es exitosa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidarCamposGuardar(out int año)) return;

            try
            {
                var datos = new
                {
                    Placa = txtPlaca.Text.Trim().ToUpper(),
                    DNI = _clienteDNI,
                    Marca = txtMarca.Text.Trim(),
                    Modelo = txtModelo.Text.Trim(),
                    Anio = año,
                    Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Obs = string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                ? (object)DBNull.Value
                                : txtObservaciones.Text.Trim(),
                    Activo = true
                };

                _db.GuardarOActualizarVehiculo(true, datos);
                MessageBox.Show("✅ Vehículo registrado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón Actualizar.
        /// Verifica que haya un vehículo cargado, valida el formulario
        /// mediante <see cref="ValidarCamposComunes"/>, construye el objeto de datos
        /// con el estado del toggle y actualiza el registro en la base de datos.
        /// Cierra la ventana si la operación es exitosa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_placaSeleccionada))
            {
                MessageBox.Show("No hay ningún vehículo cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidarCamposComunes(out int año)) return;

            try
            {
                var datos = new
                {
                    Placa = txtPlaca.Text.Trim().ToUpper(),
                    DNI = _clienteDNI,
                    Marca = txtMarca.Text.Trim(),
                    Modelo = txtModelo.Text.Trim(),
                    Anio = año,
                    Tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    Obs = string.IsNullOrWhiteSpace(txtObservaciones.Text)
                                ? (object)DBNull.Value
                                : txtObservaciones.Text.Trim(),
                    Activo = toggleActivo.IsChecked == true
                };

                _db.GuardarOActualizarVehiculo(false, datos, _placaSeleccionada);
                MessageBox.Show("✅ Vehículo actualizado correctamente.", "Éxito",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        /// <summary>
        /// Maneja el evento Click del botón Cancelar.
        /// Cierra la ventana sin guardar ningún cambio.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        /// <summary>
        /// Maneja el evento de activación del toggle de estado del vehículo.
        /// Actualiza el texto e ícono del indicador visual al estado activo (verde).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está activo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
            if (iconEstado != null) iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
        }

        /// <summary>
        /// Maneja el evento de desactivación del toggle de estado del vehículo.
        /// Actualiza el texto e ícono del indicador visual al estado inactivo (rojo).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (txtEstadoLabel == null) return;
            txtEstadoLabel.Text = "El vehículo está inactivo";
            txtEstadoLabel.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
            if (iconEstado != null) iconEstado.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f44336"));
        }

        /// <summary>
        /// Carga los datos de un vehículo existente en el formulario para su edición.
        /// Rellena todos los campos visuales, verifica el cliente asociado,
        /// selecciona el tipo correspondiente en el ComboBox, y deshabilita el botón
        /// de guardar mientras habilita el de actualizar.
        /// </summary>
        /// <param name="vehiculo">Instancia de <see cref="Vehiculo"/> con los datos a cargar.</param>
        public void CargarVehiculoParaEditar(Vehiculo vehiculo)
        {
            _placaSeleccionada = vehiculo.Vehiculo_Placa;
            txtPlaca.Text = vehiculo.Vehiculo_Placa;
            txtMarca.Text = vehiculo.Vehiculo_Marca;
            txtModelo.Text = vehiculo.Vehiculo_Modelo;
            txtAnio.Text = vehiculo.Vehiculo_Año.ToString();
            txtObservaciones.Text = vehiculo.Vehiculo_Observaciones;
            txtClienteDNI.Text = vehiculo.Cliente_DNI;
            _clienteDNI = vehiculo.Cliente_DNI;
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
            btnGuardar.IsEnabled = false;
            btnGuardar.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
        }

        /// <summary>
        /// Maneja el evento de cambio de texto en el campo de placa.
        /// Convierte automáticamente el texto a mayúsculas preservando
        /// la posición del cursor, siempre que el campo no sea de solo lectura.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de cambio de texto.</param>
        private void txtPlaca_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPlaca.IsReadOnly) return;
            int caret = txtPlaca.CaretIndex;
            txtPlaca.Text = txtPlaca.Text.ToUpper();
            txtPlaca.CaretIndex = caret;
        }

        /// <summary>
        /// Restablece todos los campos del formulario a su estado inicial.
        /// Limpia placa, marca, modelo, año, observaciones, tipo, estado del toggle,
        /// DNI del cliente, panel de información del cliente y las variables internas
        /// <see cref="_placaSeleccionada"/> y <see cref="_clienteDNI"/>.
        /// </summary>
        private void LimpiarFormulario()
        {
            txtPlaca.Clear();
            txtMarca.Clear();
            txtModelo.Clear();
            txtAnio.Clear();
            txtObservaciones.Clear();
            cmbTipo.SelectedIndex = -1;
            toggleActivo.IsChecked = true;
            txtClienteDNI.Clear();
            borderClienteInfo.Visibility = Visibility.Collapsed;
            _placaSeleccionada = string.Empty;
            _clienteDNI = string.Empty;
        }
    }
}