using Login.Clases;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Órdenes_de_Trabajo
{
    /// <summary>
    /// Ventana principal para la gestión de órdenes de trabajo.
    /// Permite crear, editar y consultar órdenes, así como asociar clientes,
    /// vehículos y repuestos.
    /// </summary>
    public partial class OrdenWindow : Window
    {
        /// <summary>
        /// Instancia para consultas a la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// DNI del cliente asociado a la orden actual.
        /// </summary>
        private string _clienteDNI = string.Empty;

        /// <summary>
        /// Placa del vehículo asociado a la orden actual.
        /// </summary>
        private string _vehiculoPlaca = string.Empty;

        /// <summary>
        /// Indica si la búsqueda se realiza por DNI (<c>true</c>) o por placa (<c>false</c>).
        /// </summary>
        private bool _buscarPorDNI = true;

        /// <summary>
        /// Identificador de la orden que se está editando.
        /// Permanece en 0 cuando se crea una nueva orden.
        /// </summary>
        private int _ordenIDEditar = 0;

        /// <summary>
        /// Ruta local de la fotografía adjunta al vehículo de la orden.
        /// </summary>
        private string _rutaFoto = string.Empty;

        /// <summary>
        /// Colección observable de repuestos asociados a la orden actual.
        /// </summary>
        private ObservableCollection<RepuestoOrden> _repuestos
            = new ObservableCollection<RepuestoOrden>();

       
        // CONSTRUCTOR
        

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="OrdenWindow"/>.
        /// Configura el idioma de los selectores de fecha, enlaza la colección
        /// de repuestos al grid, registra los manejadores de validación de entrada
        /// y establece el estado inicial de los botones.
        /// </summary>
        public OrdenWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;

            dpFecha.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");
            dpEntrega.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");

            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;

            txtBuscar.PreviewTextInput += TxtBuscar_PreviewTextInput;
            DataObject.AddPastingHandler(txtBuscar, TxtBuscar_Pasting);

            txtBuscar.TextChanged += (s, e) =>
            {
                int limite = _buscarPorDNI ? 13 : 7;
                if (txtBuscar.Text.Length > limite)
                    txtBuscar.Text = txtBuscar.Text.Substring(0, limite);
                txtContador.Text = $"{txtBuscar.Text.Length} / {limite}";
            };

            txtPrecioServicio.TextChanged += (s, e) => RecalcularPrecios();

            txtPrecioServicio.LostFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                if (decimal.TryParse(texto,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal valor))
                    txtPrecioServicio.Text = $"L {valor:N2}";
                else
                    txtPrecioServicio.Text = "L 0.00";
            };

            txtPrecioServicio.GotFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                txtPrecioServicio.Text = texto == "0.00" ? "0" : texto;
                txtPrecioServicio.SelectAll();
            };
        }

       
        /// <summary>
        /// Restringe la entrada del campo de búsqueda según el modo activo:
        /// solo dígitos para DNI o solo caracteres alfanuméricos para placa.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de entrada de texto.</param>
        private void TxtBuscar_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (_buscarPorDNI)
                e.Handled = !clsValidacionesOrden.EsCaracterValidoDNI(e.Text);
            else
                e.Handled = !clsValidacionesOrden.EsCaracterValidoPlaca(e.Text);
        }

        /// <summary>
        /// Cancela el pegado de texto en el campo de búsqueda si el contenido
        /// no cumple con el formato válido según el modo de búsqueda activo.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de pegado.</param>
        private void TxtBuscar_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
            {
                e.CancelCommand();
                return;
            }

            string texto = (string)e.DataObject.GetData(typeof(string));

            if (_buscarPorDNI && !clsValidacionesOrden.EsCaracterValidoDNI(texto))
                e.CancelCommand();
            else if (!_buscarPorDNI && !clsValidacionesOrden.EsCaracterValidoPlaca(texto))
                e.CancelCommand();
        }

        /// <summary>
        /// Carga los datos de una orden existente en el formulario para su edición.
        /// Valida que la orden pueda editarse según el mes de registro, rellena todos
        /// los campos visuales, carga los repuestos asociados y habilita el botón de actualizar.
        /// Cierra la ventana si la orden no es editable.
        /// </summary>
        /// <param name="ordenID">Identificador de la orden a cargar.</param>
        public async Task CargarOrdenParaEditar(int ordenID)
        {
            _ordenIDEditar = ordenID;
            try
            {
                var orden = _db.ObtenerOrdenParaEditar(ordenID);
                if (orden == default) return;

                if (!clsValidacionesOrden.ValidarMesActualizacion(orden.fecha))
                {
                    this.Close();
                    return;
                }

                _clienteDNI = orden.clienteDNI;
                _vehiculoPlaca = orden.vehiculoPlaca;

                txtClienteNombre.Text = orden.nombreCompleto;
                txtClienteTelefono.Text = orden.telefono;
                txtClienteEmail.Text = orden.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                txtVehiculoNombre.Text = orden.vehiculoNombre;
                txtVehiculoTipo.Text = orden.vehiculoTipo;
                txtVehiculoPropietario.Text = orden.nombreCompleto;
                borderVehiculoInfo.Visibility = Visibility.Visible;

                dpFecha.SelectedDate = orden.fecha;
                dpEntrega.SelectedDate = orden.fechaEntrega;
                txtObservaciones.Text = orden.observaciones;
                txtPrecioServicio.Text = $"L {orden.servicioPrecio:N2}";

                if (!string.IsNullOrEmpty(orden.foto) && System.IO.File.Exists(orden.foto))
                {
                    _rutaFoto = orden.foto;
                    imgFoto.Source = new BitmapImage(new Uri(orden.foto));
                    imgFoto.Visibility = Visibility.Visible;
                    txtFotoPlaceholder.Visibility = Visibility.Collapsed;
                }

                foreach (ComboBoxItem item in cmbEstado.Items)
                {
                    if (item.Content.ToString() == orden.estado)
                    {
                        cmbEstado.SelectedItem = item;
                        break;
                    }
                }

                var repuestos = _db.ObtenerRepuestosOrden(ordenID);
                foreach (var rep in repuestos)
                {
                    rep.PropertyChanged += (s, e) => RecalcularPrecios();
                    _repuestos.Add(rep);
                }

                foreach (ComboBoxItem item in cmbPrioridad.Items)
                {
                    if (item.Content.ToString() == "Normal")
                    {
                        cmbPrioridad.SelectedItem = item;
                        break;
                    }
                }

                btnAñadir.IsEnabled = false;
                btnAñadir.Opacity = 0.4;
                btnActualizar.IsEnabled = true;
                btnActualizar.Opacity = 1;

                this.UpdateLayout();
                RecalcularPrecios();
            }
            catch (Exception ex)
            {
                MostrarError("Error al cargar la orden: " + ex.Message);
            }
        }


        /// <summary>
        /// Activa el modo de búsqueda por DNI.
        /// Actualiza el estilo visual de las pestañas, ajusta el límite de caracteres
        /// del campo de búsqueda y limpia el panel de errores.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de clic.</param>
        private void TabDNI_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = true;
            txtBuscar.MaxLength = 13;
            txtContador.Text = "0 / 13";
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            if (tabPlaca.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6c7293"));
            lblBuscar.Text = "DNI del Cliente";
            txtBuscar.Text = string.Empty;
            borderError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Activa el modo de búsqueda por placa de vehículo.
        /// Actualiza el estilo visual de las pestañas, ajusta el límite de caracteres
        /// del campo de búsqueda y limpia el panel de errores.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de clic.</param>
        private void TabPlaca_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = false;
            txtBuscar.MaxLength = 7;
            txtContador.Text = "0 / 7";
            tabPlaca.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4f6ef7"));
            tabDNI.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1e2130"));
            if (tabPlaca.Child is TextBlock tb)
                tb.Foreground = new SolidColorBrush(Colors.White);
            lblBuscar.Text = "Placa del Vehículo";
            txtBuscar.Text = string.Empty;
            borderError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Maneja el evento Click del botón Buscar.
        /// Valida el campo de búsqueda y delega la consulta al método correspondiente
        /// según el modo activo (DNI o placa).
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string valor = txtBuscar.Text.Trim();

            if (!clsValidacionesOrden.ValidarCampoBusqueda(valor, _buscarPorDNI))
            {
                txtBuscar.Focus();
                return;
            }

            if (_buscarPorDNI)
            {
                if (!clsValidacionesOrden.ValidarFormatoDNIBusqueda(valor))
                {
                    txtBuscar.Focus();
                    return;
                }
            }
            else
            {
                if (!clsValidacionesOrden.ValidarFormatoPlacaBusqueda(valor))
                {
                    txtBuscar.Focus();
                    return;
                }
            }

            LimpiarResultados();

            if (_buscarPorDNI) BuscarPorDNI(valor);
            else BuscarPorPlaca(valor.ToUpper());
        }

        /// <summary>
        /// Busca un cliente en la base de datos por su DNI y muestra su información
        /// junto con la del vehículo asociado si existe.
        /// Muestra un mensaje de error si no se encuentra ningún resultado.
        /// </summary>
        /// <param name="dni">DNI del cliente a buscar.</param>
        private void BuscarPorDNI(string dni)
        {
            try
            {
                var resultado = _db.BuscarClientePorDNI(dni);
                if (resultado == default)
                {
                    MostrarError($"No existe cliente con DNI '{dni}'.");
                    return;
                }

                _clienteDNI = dni;
                txtClienteNombre.Text = resultado.nombreCompleto;
                txtClienteTelefono.Text = resultado.telefono;
                txtClienteEmail.Text = resultado.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(resultado.vehiculoPlaca))
                {
                    _vehiculoPlaca = resultado.vehiculoPlaca;
                    txtVehiculoNombre.Text = resultado.vehiculoNombre;
                    txtVehiculoTipo.Text = resultado.vehiculoTipo;
                    txtVehiculoPropietario.Text = resultado.nombreCompleto;
                    borderVehiculoInfo.Visibility = Visibility.Visible;
                }

                this.UpdateLayout();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        /// <summary>
        /// Busca un vehículo en la base de datos por su placa y muestra su información
        /// junto con los datos del cliente propietario.
        /// Muestra un mensaje de error si no se encuentra ningún resultado.
        /// </summary>
        /// <param name="placa">Placa del vehículo a buscar (en mayúsculas).</param>
        private void BuscarPorPlaca(string placa)
        {
            try
            {
                var resultado = _db.BuscarVehiculoPorPlaca(placa);
                if (resultado == default)
                {
                    MostrarError($"No existe vehículo con placa '{placa}'.");
                    return;
                }

                _vehiculoPlaca = placa;
                _clienteDNI = resultado.clienteDNI;
                txtVehiculoNombre.Text = resultado.vehiculoNombre;
                txtVehiculoTipo.Text = resultado.vehiculoTipo;
                txtVehiculoPropietario.Text = resultado.nombreCompleto;
                txtClienteNombre.Text = resultado.nombreCompleto;
                txtClienteTelefono.Text = resultado.telefono;
                txtClienteEmail.Text = resultado.email;
                borderVehiculoInfo.Visibility = Visibility.Visible;
                borderClienteInfo.Visibility = Visibility.Visible;

                this.UpdateLayout();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }


        /// <summary>
        /// Maneja el evento Click del botón Añadir.
        /// Valida el formulario completo, calcula el total de repuestos y servicio,
        /// registra la nueva orden en la base de datos, limpia el formulario y cierra la ventana.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidacionesOrden.ValidarFormularioVacio(
                _clienteDNI, _vehiculoPlaca, txtPrecioServicio.Text)) return;

            if (!clsValidacionesOrden.ValidarClienteAsignado(_clienteDNI)) return;

            if (!clsValidacionesOrden.ValidarFormularioAñadir(
                    _clienteDNI,
                    _vehiculoPlaca,
                    cmbEstado.SelectedItem,
                    cmbPrioridad.SelectedItem,
                    dpFecha.SelectedDate,
                    dpEntrega.SelectedDate,
                    txtPrecioServicio.Text,
                    txtObservaciones.Text,
                    _rutaFoto,
                    _repuestos.Count,
                    out decimal precioServicio))
                return;

            try
            {
                decimal totalRepuestos = 0;
                foreach (var r in _repuestos)
                    if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

                decimal total = totalRepuestos + precioServicio;
                string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";

                _db.AgregarOrden(
                    _clienteDNI, _vehiculoPlaca, null, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, _rutaFoto,
                    _repuestos.ToList()
                );

                MessageBox.Show("✅ Orden guardada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar la orden:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón Actualizar.
        /// Valida el formulario completo, calcula el total actualizado de repuestos y servicio,
        /// y persiste los cambios de la orden existente en la base de datos.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidacionesOrden.ValidarFormularioVacio(
                _clienteDNI, _vehiculoPlaca, txtPrecioServicio.Text)) return;

            if (!clsValidacionesOrden.ValidarFormularioActualizar(
                    _clienteDNI,
                    _vehiculoPlaca,
                    cmbEstado.SelectedItem,
                    cmbPrioridad.SelectedItem,
                    dpFecha.SelectedDate,
                    dpEntrega.SelectedDate,
                    txtPrecioServicio.Text,
                    txtObservaciones.Text,
                    _rutaFoto,
                    _repuestos.Count,
                    out decimal precioServicio))
                return;

            try
            {
                decimal totalRepuestos = 0;
                foreach (var r in _repuestos)
                    if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

                decimal total = totalRepuestos + precioServicio;
                string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";

                _db.ActualizarOrden(
                    _ordenIDEditar, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, _rutaFoto,
                    _repuestos.ToList()
                );

                MessageBox.Show("✅ Orden actualizada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la orden:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        /// <summary>
        /// Maneja el evento de clic sobre el área de foto del vehículo.
        /// Abre un diálogo de selección de archivo filtrado por imágenes,
        /// valida el archivo seleccionado y lo muestra en el control de imagen.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento de clic.</param>
        private void AdjuntarFoto_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Seleccionar foto del vehículo"
            };

            if (dialog.ShowDialog() == true)
            {
                if (!clsValidacionesOrden.ValidarFoto(dialog.FileName))
                    return;

                _rutaFoto = dialog.FileName;
                imgFoto.Source = new BitmapImage(new Uri(_rutaFoto));
                imgFoto.Visibility = Visibility.Visible;
                txtFotoPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Maneja el evento Click del botón de agregar repuesto.
        /// Abre la ventana <see cref="AgregarRepuesto"/> como diálogo modal,
        /// y si el usuario confirma, añade el repuesto resultante a la colección
        /// y recalcula los precios totales.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarRepuesto();
            ventana.Owner = this;
            ventana.ShowDialog();

            if (ventana.RepuestoResultado != null)
            {
                ventana.RepuestoResultado.Numero = _repuestos.Count + 1;
                ventana.RepuestoResultado.PropertyChanged += (s, ev) => RecalcularPrecios();
                _repuestos.Add(ventana.RepuestoResultado);
                RecalcularPrecios();
            }
        }


        /// <summary>
        /// Maneja el evento Click del botón Cancelar.
        /// Cierra la ventana sin guardar ningún cambio.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Datos del evento.</param>
        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

      
        /// <summary>
        /// Recalcula y actualiza los campos de precio en pantalla.
        /// Suma el total de los repuestos incluidos y lo combina con el precio
        /// del servicio para obtener el costo total de la orden.
        /// </summary>
        private void RecalcularPrecios()
        {
            decimal totalRepuestos = 0;
            foreach (var r in _repuestos)
                if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

            txtPrecioRepuesto.Text = $"L {totalRepuestos:N2}";

            string precioTexto = txtPrecioServicio.Text
                .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            decimal.TryParse(precioTexto,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal servicio);

            txtCostoTotal.Text = $"L {(totalRepuestos + servicio):N2}";
        }


        /// <summary>
        /// Muestra el panel de error con el mensaje indicado.
        /// </summary>
        /// <param name="mensaje">Texto del error a mostrar al usuario.</param>
        private void MostrarError(string mensaje)
        {
            borderError.Visibility = Visibility.Visible;
            txtError.Text = mensaje;
        }

        /// <summary>
        /// Oculta los paneles de información de cliente y vehículo,
        /// limpia el panel de errores y restablece los campos de DNI y placa.
        /// </summary>
        private void LimpiarResultados()
        {
            borderClienteInfo.Visibility = Visibility.Collapsed;
            borderVehiculoInfo.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
            _clienteDNI = string.Empty;
            _vehiculoPlaca = string.Empty;
        }

        /// <summary>
        /// Restablece todos los campos del formulario a su estado inicial.
        /// Invoca <see cref="LimpiarResultados"/> y limpia adicionalmente los campos
        /// de repuestos, precios, fechas, observaciones, foto, estado y prioridad.
        /// </summary>
        private void LimpiarFormulario()
        {
            LimpiarResultados();
            txtBuscar.Clear();
            _repuestos.Clear();
            _ordenIDEditar = 0;
            _rutaFoto = string.Empty;
            txtPrecioRepuesto.Text = "L 0.00";
            txtPrecioServicio.Text = "L 0.00";
            txtCostoTotal.Text = "L 0.00";
            dpFecha.SelectedDate = null;
            dpEntrega.SelectedDate = null;
            txtObservaciones?.Clear();
            cmbEstado.SelectedIndex = -1;
            cmbPrioridad.SelectedIndex = -1;
            imgFoto.Source = null;
            imgFoto.Visibility = Visibility.Collapsed;
            txtFotoPlaceholder.Visibility = Visibility.Visible;
            btnAñadir.IsEnabled = true;
            btnAñadir.Opacity = 1;
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
            txtContador.Text = "0 / 13";
        }
    }
}