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
        /// Instancia de acceso a la base de datos.
        /// </summary>
        private clsConsultasBD _db = new clsConsultasBD();

        /// <summary>
        /// DNI del cliente seleccionado y verificado mediante búsqueda.
        /// </summary>
        private string _clienteDNI = string.Empty;

        /// <summary>
        /// Placa del vehículo seleccionado y verificado mediante búsqueda.
        /// </summary>
        private string _vehiculoPlaca = string.Empty;

        /// <summary>
        /// Indica si la búsqueda se realiza por DNI (true) o por placa (false).
        /// </summary>
        private bool _buscarPorDNI = true;

        /// <summary>
        /// Identificador de la orden que se está editando. 0 si es una orden nueva.
        /// </summary>
        private int _ordenIDEditar = 0;

        /// <summary>
        /// Ruta absoluta de la imagen adjunta a la orden.
        /// </summary>
        private string _rutaFoto = string.Empty;

        /// <summary>
        /// Colección observable de repuestos asociados a la orden actual.
        /// </summary>
        private ObservableCollection<RepuestoOrden> _repuestos
            = new ObservableCollection<RepuestoOrden>();

        // ─────────────────────────────────────────────────────────────
        // CONSTRUCTOR
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="OrdenWindow"/>.
        /// Configura el DataGrid, los DatePicker en español y los eventos
        /// de formato automático del campo de precio de servicio.
        /// </summary>
        public OrdenWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;

            // Idioma español Honduras para los DatePicker
            dpFecha.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");
            dpEntrega.Language = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");

            // El botón Actualizar se habilita solo al cargar una orden existente
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;

            // Límite de caracteres dinámico según modo de búsqueda
            txtBuscar.MaxLength = 13;

            txtBuscar.PreviewTextInput += (s, e) =>
            {
                int limite = _buscarPorDNI ? 13 : 7;
                if (txtBuscar.Text.Length >= limite) e.Handled = true;
            };

            txtBuscar.TextChanged += (s, e) =>
            {
                int limite = _buscarPorDNI ? 13 : 7;
                if (txtBuscar.Text.Length > limite)
                    txtBuscar.Text = txtBuscar.Text.Substring(0, limite);
                txtContador.Text = $"{txtBuscar.Text.Length} / {limite}";
            };

            // Recalcular totales al escribir en precio de servicio
            txtPrecioServicio.TextChanged += (s, e) => RecalcularPrecios();

            // Al perder foco: formatear como "L 0.00"
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

            // Al ganar foco: mostrar solo el número para edición
            txtPrecioServicio.GotFocus += (s, e) =>
            {
                string texto = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                txtPrecioServicio.Text = texto == "0.00" ? "0" : texto;
                txtPrecioServicio.SelectAll();
            };
        }

        // ─────────────────────────────────────────────────────────────
        // CARGA PARA EDICIÓN
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Carga los datos de una orden existente en el formulario para su edición.
        /// Deshabilita el botón Añadir y habilita el botón Actualizar.
        /// </summary>
        /// <param name="ordenID">Identificador único de la orden a editar.</param>
        public async Task CargarOrdenParaEditar(int ordenID)
        {
            _ordenIDEditar = ordenID;
            try
            {
                var orden = _db.ObtenerOrdenParaEditar(ordenID);
                if (orden == default) return;

                _clienteDNI = orden.clienteDNI;
                _vehiculoPlaca = orden.vehiculoPlaca;

                // Datos del cliente
                txtClienteNombre.Text = orden.nombreCompleto;
                txtClienteTelefono.Text = orden.telefono;
                txtClienteEmail.Text = orden.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                // Datos del vehículo
                txtVehiculoNombre.Text = orden.vehiculoNombre;
                txtVehiculoTipo.Text = orden.vehiculoTipo;
                txtVehiculoPropietario.Text = orden.nombreCompleto;
                borderVehiculoInfo.Visibility = Visibility.Visible;

                // Datos de la orden
                dpFecha.SelectedDate = orden.fecha;
                dpEntrega.SelectedDate = orden.fechaEntrega;
                txtObservaciones.Text = orden.observaciones;
                txtPrecioServicio.Text = $"L {orden.servicioPrecio:N2}";

                // Foto adjunta
                if (!string.IsNullOrEmpty(orden.foto) && System.IO.File.Exists(orden.foto))
                {
                    _rutaFoto = orden.foto;
                    imgFoto.Source = new BitmapImage(new Uri(orden.foto));
                    imgFoto.Visibility = Visibility.Visible;
                    txtFotoPlaceholder.Visibility = Visibility.Collapsed;
                }

                // Estado
                foreach (ComboBoxItem item in cmbEstado.Items)
                {
                    if (item.Content.ToString() == orden.estado)
                    {
                        cmbEstado.SelectedItem = item;
                        break;
                    }
                }

                // Repuestos
                var repuestos = _db.ObtenerRepuestosOrden(ordenID);
                foreach (var rep in repuestos)
                {
                    rep.PropertyChanged += (s, e) => RecalcularPrecios();
                    _repuestos.Add(rep);
                }

                // Prioridad por defecto
                foreach (ComboBoxItem item in cmbPrioridad.Items)
                {
                    if (item.Content.ToString() == "Normal")
                    {
                        cmbPrioridad.SelectedItem = item;
                        break;
                    }
                }

                // Modo edición: Añadir deshabilitado, Actualizar habilitado
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

        // ─────────────────────────────────────────────────────────────
        // BÚSQUEDA — TABS DNI / PLACA
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Cambia el modo de búsqueda a DNI del cliente.
        /// Limpia el campo de búsqueda y oculta mensajes de error.
        /// </summary>
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
        /// Cambia el modo de búsqueda a placa del vehículo.
        /// Limpia el campo de búsqueda y oculta mensajes de error.
        /// </summary>
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
        /// Ejecuta la búsqueda validando el formato del campo según el modo activo.
        /// Valida formato de DNI (13 dígitos) o formato de placa hondureña antes de consultar BD.
        /// </summary>
        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string valor = txtBuscar.Text.Trim();

            // — Validar que el campo no esté vacío —
            if (!clsValidacionesOrden.ValidarCampoBusqueda(valor, _buscarPorDNI))
            {
                txtBuscar.Focus();
                return;
            }

            // — Validar formato según el modo de búsqueda —
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
        /// Busca un cliente en la base de datos por su número de DNI.
        /// Si el cliente tiene vehículo registrado, lo muestra automáticamente.
        /// </summary>
        /// <param name="dni">DNI del cliente a buscar (13 dígitos).</param>
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
        /// Busca un vehículo en la base de datos por su placa.
        /// Carga también los datos del propietario (cliente) automáticamente.
        /// </summary>
        /// <param name="placa">Placa del vehículo a buscar.</param>
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

        // ─────────────────────────────────────────────────────────────
        // GUARDAR — NUEVA ORDEN
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida todos los campos del formulario y guarda una nueva orden de trabajo.
        /// Ejecuta validaciones en este orden: cliente/vehículo → estado → fechas →
        /// precio → observaciones → foto → repuestos (advertencia).
        /// </summary>
        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            // — Validación completa antes de tocar la BD —
            if (!clsValidacionesOrden.ValidarFormularioAñadir(
                    _clienteDNI,
                    _vehiculoPlaca,
                    cmbEstado.SelectedItem,
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
                // Calcular totales
                decimal totalRepuestos = 0;
                foreach (var r in _repuestos)
                    if (r.Incluido) totalRepuestos += r.Precio * r.Cantidad;

                decimal total = totalRepuestos + precioServicio;
                string estado = (cmbEstado.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Sin Empezar";
                int productoID = _repuestos.Count > 0 ? _repuestos[0].ProductoID : 0;

                _db.AgregarOrden(
                    _clienteDNI, _vehiculoPlaca, productoID, estado,
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

        // ─────────────────────────────────────────────────────────────
        // ACTUALIZAR — ORDEN EXISTENTE
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida los campos editables y actualiza una orden de trabajo existente.
        /// Bloquea la actualización si la orden pertenece a un mes anterior.
        /// Valida fechas, precio de servicio, observaciones y foto antes de guardar.
        /// </summary>
        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            // — Validación completa antes de tocar la BD —
            if (!clsValidacionesOrden.ValidarFormularioActualizar(
                    dpFecha.SelectedDate,
                    dpEntrega.SelectedDate,
                    txtPrecioServicio.Text,
                    txtObservaciones.Text,
                    _rutaFoto,
                    out decimal precioServicio))
                return;

            try
            {
                // Calcular totales
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

        // ─────────────────────────────────────────────────────────────
        // FOTO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Abre un diálogo de selección de archivo para adjuntar una foto del vehículo.
        /// Valida la extensión y el tamaño del archivo antes de cargarlo en la interfaz.
        /// Formatos permitidos: JPG, JPEG, PNG, BMP. Tamaño máximo: 5 MB.
        /// </summary>
        private void AdjuntarFoto_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Imágenes|*.jpg;*.jpeg;*.png;*.bmp",
                Title = "Seleccionar foto del vehículo"
            };

            if (dialog.ShowDialog() == true)
            {
                // Validar el archivo seleccionado antes de cargarlo
                if (!clsValidacionesOrden.ValidarFoto(dialog.FileName))
                    return;

                _rutaFoto = dialog.FileName;
                imgFoto.Source = new BitmapImage(new Uri(_rutaFoto));
                imgFoto.Visibility = Visibility.Visible;
                txtFotoPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // REPUESTOS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Abre la ventana de selección de repuestos y agrega el repuesto elegido
        /// a la lista de la orden. Recalcula los precios automáticamente al añadir.
        /// </summary>
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

        // ─────────────────────────────────────────────────────────────
        // CANCELAR
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Cierra la ventana sin guardar ningún cambio.
        /// </summary>
        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        // ─────────────────────────────────────────────────────────────
        // CÁLCULO DE PRECIOS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Recalcula y actualiza en pantalla el total de repuestos incluidos
        /// y el costo total de la orden (repuestos + servicio).
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

        // ─────────────────────────────────────────────────────────────
        // HELPERS DE INTERFAZ
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Muestra el panel de error con el mensaje especificado.
        /// </summary>
        /// <param name="mensaje">Texto del error a mostrar.</param>
        private void MostrarError(string mensaje)
        {
            borderError.Visibility = Visibility.Visible;
            txtError.Text = mensaje;
        }

        /// <summary>
        /// Oculta los paneles de cliente, vehículo y error,
        /// y limpia las variables internas de selección.
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
        /// Reinicia completamente el formulario a su estado inicial,
        /// como si se fuera a crear una nueva orden desde cero.
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