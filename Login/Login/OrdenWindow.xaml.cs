using Login.Clases;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Órdenes_de_Trabajo
{
    public partial class OrdenWindow : Window
    {
        private readonly RepositorioSql _db = new();
        private string _clienteDNI = string.Empty;
        private string _vehiculoPlaca = string.Empty;
        private bool _buscarPorDNI = true;
        private int _ordenIDEditar = 0;
        private readonly ObservableCollection<RepuestoOrden> _repuestos = new();

        // Colores reutilizables
        private static readonly SolidColorBrush BrushActivo =
            new(Color.FromRgb(0x4f, 0x6e, 0xf7));
        private static readonly SolidColorBrush BrushInactivo =
            new(Color.FromRgb(0x1e, 0x22, 0x35));
        private static readonly SolidColorBrush BrushTextoInactivo =
            new(Color.FromRgb(0x50, 0x58, 0x80));
        private static readonly SolidColorBrush BrushZonaDañada =
            new(Color.FromArgb(0x22, 0x4f, 0x6e, 0xf7));

        public OrdenWindow()
        {
            InitializeComponent();
            dgRepuestos.ItemsSource = _repuestos;

            // Idioma fechas
            var lang = System.Windows.Markup.XmlLanguage.GetLanguage("es-HN");
            dpFecha.Language = lang;
            dpEntrega.Language = lang;

            dpFecha.SelectedDate = DateTime.Today;
            dpFecha.IsEnabled = false;

            // Botones modo "nuevo": Añadir habilitado, Actualizar deshabilitado
            SetModoNuevo();

            // Diagrama inicial: sedán/SUV (canvasAuto) visible por defecto
            canvasAuto.Visibility = Visibility.Visible;
            canvasPickup.Visibility = Visibility.Collapsed;
            canvasMoto.Visibility = Visibility.Collapsed;

            // Validación de entrada en tiempo real
            txtBuscar.PreviewTextInput += TxtBuscar_PreviewTextInput;
            DataObject.AddPastingHandler(txtBuscar, TxtBuscar_Pasting);

            // Precio servicio: recalcular al cambiar
            txtPrecioServicio.TextChanged += (s, e) => RecalcularPrecios();

            txtPrecioServicio.LostFocus += (s, e) =>
            {
                if (ParsePrecio(txtPrecioServicio.Text, out decimal v))
                    txtPrecioServicio.Text = $"L {v:N2}";
                else
                    txtPrecioServicio.Text = "L 0.00";
            };

            txtPrecioServicio.GotFocus += (s, e) =>
            {
                string limpio = txtPrecioServicio.Text
                    .Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
                txtPrecioServicio.Text = limpio == "0.00" ? "0" : limpio;
                txtPrecioServicio.SelectAll();
            };
        }

        // ── VENTANA: TAMAÑO Y POSICIÓN ────────────────────────────────

        /// <summary>
        /// Ajusta la ventana al área de trabajo disponible y la centra.
        /// Se ejecuta al cargar y cada vez que la ventana vuelve al
        /// estado Normal (por ejemplo, tras minimizarla y restaurarla),
        /// evitando que quede pegada arriba o más grande que la pantalla.
        /// </summary>
        private void CentrarYAjustarAPantalla()
        {
            var areaTrabajo = SystemParameters.WorkArea;

            if (Height > areaTrabajo.Height)
                Height = areaTrabajo.Height - 20;
            if (Width > areaTrabajo.Width)
                Width = areaTrabajo.Width - 20;

            Left = areaTrabajo.Left + (areaTrabajo.Width - Width) / 2;
            Top = areaTrabajo.Top + (areaTrabajo.Height - Height) / 2;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CentrarYAjustarAPantalla();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Normal)
                CentrarYAjustarAPantalla();
        }

        // ── HELPERS DE MODO ─────────────────────────────────────────

        private void SetModoNuevo()
        {
            btnAñadir.IsEnabled = true;
            btnAñadir.Opacity = 1;
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;
            txtTituloVentana.Text = "Nueva Orden de Trabajo";
        }

        private void SetModoEditar()
        {
            btnAñadir.IsEnabled = false;
            btnAñadir.Opacity = 0.4;
            btnActualizar.IsEnabled = true;
            btnActualizar.Opacity = 1;
            txtTituloVentana.Text = "Editar Orden de Trabajo";
        }

        // ── VALIDACIÓN DE ENTRADA ────────────────────────────────────

        private void TxtBuscar_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _buscarPorDNI
                ? !ValidadorÓrden.EsCaracterValidoDNI(e.Text)
                : !ValidadorÓrden.EsCaracterValidoPlaca(e.Text);
        }

        private void TxtBuscar_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string))) { e.CancelCommand(); return; }
            string texto = (string)e.DataObject.GetData(typeof(string));
            bool valido = _buscarPorDNI
                ? ValidadorÓrden.EsCaracterValidoDNI(texto)
                : ValidadorÓrden.EsCaracterValidoPlaca(texto);
            if (!valido) e.CancelCommand();
        }

        // ── CARGAR ORDEN PARA EDITAR ─────────────────────────────────

        public async Task CargarOrdenParaEditar(int ordenID)
        {
            _ordenIDEditar = ordenID;
            try
            {
                var orden = _db.ObtenerOrdenParaEditar(ordenID);
                if (orden == default) return;

                if (!ValidadorÓrden.ValidarMesActualizacion(orden.fecha))
                {
                    Close();
                    return;
                }

                _clienteDNI = orden.clienteDNI;
                _vehiculoPlaca = orden.vehiculoPlaca;

                borderSelectorVehiculo.Visibility = Visibility.Collapsed;

                // Info cliente
                txtClienteNombre.Text = orden.nombreCompleto;
                txtClienteTelefono.Text = orden.telefono;
                txtClienteEmail.Text = orden.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                // Info vehículo
                txtVehiculoNombre.Text = orden.vehiculoNombre;
                txtVehiculoTipo.Text = orden.vehiculoTipo;
                txtVehiculoPropietario.Text = orden.vehiculoPlaca;
                borderVehiculoInfo.Visibility = Visibility.Visible;

                // Mostrar el diagrama correspondiente al tipo de vehículo cargado
                MostrarCanvasPorTipo(orden.vehiculoTipo);

                // Campos de la orden
                dpFecha.SelectedDate = orden.fecha;
                dpEntrega.SelectedDate = orden.fechaEntrega;
                txtObservaciones.Text = orden.observaciones;
                txtPrecioServicio.Text = $"L {orden.servicioPrecio:N2}";

                // Estado
                SeleccionarComboBoxPorContenido(cmbEstado, orden.estado);

                // Prioridad — la BD no guarda prioridad actualmente, se deja en "Normal"
                SeleccionarComboBoxPorContenido(cmbPrioridad, "Normal");

                // Repuestos
                foreach (var rep in _db.ObtenerRepuestosOrden(ordenID))
                {
                    rep.PropertyChanged += (s, e) => RecalcularPrecios();
                    _repuestos.Add(rep);
                }

                SetModoEditar();
                UpdateLayout();
                RecalcularPrecios();
            }
            catch (Exception ex)
            {
                MostrarError("Error al cargar la orden: " + ex.Message);
            }
        }

        /// <summary>Selecciona un ComboBoxItem cuyo Content coincida con <paramref name="valor"/>.</summary>
        private static void SeleccionarComboBoxPorContenido(ComboBox combo, string valor)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                if (item.Content?.ToString() == valor)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        // ── PESTAÑAS DE BÚSQUEDA ─────────────────────────────────────

        private void TabDNI_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = true;
            txtBuscar.MaxLength = 13;
            txtContador.Text = "0 / 13";
            tabDNI.Background = BrushActivo;
            tabPlaca.Background = BrushInactivo;
            if (tabPlaca.Child is TextBlock tb) tb.Foreground = BrushTextoInactivo;
            lblBuscar.Text = "DNI del Cliente";
            txtBuscar.Clear();
            borderError.Visibility = Visibility.Collapsed;
            borderAutocompletado.Visibility = Visibility.Collapsed;
        }

        private void TabPlaca_Click(object sender, MouseButtonEventArgs e)
        {
            _buscarPorDNI = false;
            txtBuscar.MaxLength = 7;
            txtContador.Text = "0 / 7";
            tabPlaca.Background = BrushActivo;
            tabDNI.Background = BrushInactivo;
            if (tabPlaca.Child is TextBlock tb) tb.Foreground = Brushes.White;
            lblBuscar.Text = "Placa del Vehículo";
            txtBuscar.Clear();
            borderError.Visibility = Visibility.Collapsed;
            borderAutocompletado.Visibility = Visibility.Collapsed;
        }

        // ── BUSCAR ───────────────────────────────────────────────────

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            string valor = txtBuscar.Text.Trim();

            if (!ValidadorÓrden.ValidarCampoBusqueda(valor, _buscarPorDNI))
            { txtBuscar.Focus(); return; }

            if (_buscarPorDNI && !ValidadorÓrden.ValidarFormatoDNIBusqueda(valor))
            { txtBuscar.Focus(); return; }

            if (!_buscarPorDNI && !ValidadorÓrden.ValidarFormatoPlacaBusqueda(valor))
            { txtBuscar.Focus(); return; }

            LimpiarResultados();

            if (_buscarPorDNI) BuscarPorDNI(valor);
            else BuscarPorPlaca(valor.ToUpper());
        }

        private void BuscarPorDNI(string dni)
        {
            try
            {
                var r = _db.BuscarClientePorDNI(dni);
                if (r == default)
                {
                    MostrarError($"No existe cliente con DNI '{dni}'.");
                    return;
                }

                if (!ValidadorÓrden.ValidarClienteActivo(r.activo, r.nombreCompleto)) return;

                _clienteDNI = dni;
                txtClienteNombre.Text = r.nombreCompleto;
                txtClienteTelefono.Text = r.telefono;
                txtClienteEmail.Text = r.email;
                borderClienteInfo.Visibility = Visibility.Visible;

                // Obtener TODOS los vehículos del cliente para decidir si mostrar el selector
                var vehiculos = _db.ObtenerVehiculosDeCliente(dni);

                if (vehiculos.Count == 0)
                {
                    _vehiculoPlaca = string.Empty;
                    borderVehiculoInfo.Visibility = Visibility.Collapsed;
                    borderSelectorVehiculo.Visibility = Visibility.Collapsed;
                    MostrarError("Este cliente no tiene vehículos registrados.");
                }
                else if (vehiculos.Count == 1)
                {
                    borderSelectorVehiculo.Visibility = Visibility.Collapsed;
                    SeleccionarVehiculo(vehiculos[0]);
                }
                else
                {
                    borderVehiculoInfo.Visibility = Visibility.Collapsed;
                    borderSelectorVehiculo.Visibility = Visibility.Visible;
                    lstVehiculos.ItemsSource = vehiculos;
                }

                UpdateLayout();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        private void BuscarPorPlaca(string placa)
        {
            try
            {
                var r = _db.BuscarVehiculoPorPlaca(placa);
                if (r == default)
                {
                    MostrarError($"No existe vehículo con placa '{placa}'.");
                    return;
                }

                if (!ValidadorÓrden.ValidarClienteActivo(r.activo, r.nombreCompleto)) return;
                if (!ValidadorÓrden.ValidarVehiculoActivo(r.vehiculoActivo, r.vehiculoNombre)) return;

                borderSelectorVehiculo.Visibility = Visibility.Collapsed;

                _vehiculoPlaca = placa;
                _clienteDNI = r.clienteDNI;

                txtVehiculoNombre.Text = r.vehiculoNombre;
                txtVehiculoTipo.Text = r.vehiculoTipo;
                txtVehiculoPropietario.Text = placa;
                txtClienteNombre.Text = r.nombreCompleto;
                txtClienteTelefono.Text = r.telefono;
                txtClienteEmail.Text = r.email;

                borderVehiculoInfo.Visibility = Visibility.Visible;
                borderClienteInfo.Visibility = Visibility.Visible;

                MostrarCanvasPorTipo(r.vehiculoTipo);

                UpdateLayout();
            }
            catch (Exception ex) { MostrarError(ex.Message); }
        }

        // ── SELECCIÓN DE VEHÍCULO (cuando el cliente tiene varios) ────

        private void SeleccionarVehiculo(RepositorioSql.VehiculoDeCliente v)
        {
            if (!ValidadorÓrden.ValidarVehiculoActivo(v.Activo, v.Descripcion)) return;

            _vehiculoPlaca = v.Placa;
            txtVehiculoNombre.Text = v.Descripcion;
            txtVehiculoTipo.Text = v.TipoAño;
            txtVehiculoPropietario.Text = v.Placa;
            borderVehiculoInfo.Visibility = Visibility.Visible;

            MostrarCanvasPorTipo(v.Tipo);
            UpdateLayout();
        }

        private void lstVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstVehiculos.SelectedItem is RepositorioSql.VehiculoDeCliente seleccionado)
            {
                SeleccionarVehiculo(seleccionado);
                borderSelectorVehiculo.Visibility = Visibility.Collapsed;
            }
        }

        // ── GUARDAR ──────────────────────────────────────────────────

        private void btnAniadir_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidadorÓrden.ValidarFormularioVacio(
                    _clienteDNI, _vehiculoPlaca, txtPrecioServicio.Text)) return;

            if (!ValidadorÓrden.ValidarClienteAsignado(_clienteDNI)) return;

            if (!ValidadorÓrden.ValidarFormularioAñadir(
                    _clienteDNI, _vehiculoPlaca,
                    cmbEstado.SelectedItem, cmbPrioridad.SelectedItem,
                    dpFecha.SelectedDate, dpEntrega.SelectedDate,
                    txtPrecioServicio.Text, txtObservaciones.Text,
                    string.Empty, _repuestos.Count,
                    out decimal precioServicio))
                return;

            var confirmar = MessageBox.Show(
                "¿Deseas guardar esta nueva orden de trabajo?",
                "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmar != MessageBoxResult.Yes) return;

            try
            {
                decimal totalRepuestos = _repuestos.Where(r => r.Incluido).Sum(r => r.Precio * r.Cantidad);
                decimal total = totalRepuestos + precioServicio;
                string estado = ObtenerContenidoCombo(cmbEstado) ?? "Sin Empezar";

                int ordenID = _db.AgregarOrden(
                    _clienteDNI, _vehiculoPlaca, null, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, string.Empty,
                    _repuestos.ToList());

                _db.RegistrarBitacora(SesionActual.Email, "Órdenes", "Agregar",
                    $"Orden #{ordenID} - Cliente {_clienteDNI}, Total L {total:N2}");

                MessageBox.Show("✅ Orden guardada correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                LimpiarFormulario();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar la orden:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidadorÓrden.ValidarFormularioVacio(
                    _clienteDNI, _vehiculoPlaca, txtPrecioServicio.Text)) return;

            if (!ValidadorÓrden.ValidarFormularioActualizar(
                    _clienteDNI, _vehiculoPlaca,
                    cmbEstado.SelectedItem, cmbPrioridad.SelectedItem,
                    dpFecha.SelectedDate, dpEntrega.SelectedDate,
                    txtPrecioServicio.Text, txtObservaciones.Text,
                    string.Empty, _repuestos.Count,
                    out decimal precioServicio))
                return;

            var confirmar = MessageBox.Show(
                "¿Deseas guardar los cambios en esta orden?",
                "Confirmar actualización", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmar != MessageBoxResult.Yes) return;

            try
            {
                decimal totalRepuestos = _repuestos.Where(r => r.Incluido).Sum(r => r.Precio * r.Cantidad);
                decimal total = totalRepuestos + precioServicio;
                string estado = ObtenerContenidoCombo(cmbEstado) ?? "Sin Empezar";

                _db.ActualizarOrden(
                    _ordenIDEditar, estado,
                    dpFecha.SelectedDate ?? DateTime.Today,
                    dpEntrega.SelectedDate,
                    txtObservaciones.Text.Trim(),
                    precioServicio, total, string.Empty,
                    _repuestos.ToList());

                _db.RegistrarBitacora(SesionActual.Email, "Órdenes", "Actualizar",
                    $"Orden #{_ordenIDEditar} - Estado: {estado}, Total L {total:N2}");

                MessageBox.Show("✅ Orden actualizada correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al actualizar la orden:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── REPUESTOS ────────────────────────────────────────────────

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarRepuesto { Owner = this };
            ventana.ShowDialog();

            if (ventana.RepuestoResultado != null)
            {
                ventana.RepuestoResultado.Numero = _repuestos.Count + 1;
                ventana.RepuestoResultado.PropertyChanged += (s, ev) => RecalcularPrecios();
                _repuestos.Add(ventana.RepuestoResultado);
                RecalcularPrecios();
            }
        }

        // ── CANCELAR ─────────────────────────────────────────────────

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            bool hayDatos = !string.IsNullOrWhiteSpace(_clienteDNI)
                            || !string.IsNullOrWhiteSpace(_vehiculoPlaca)
                            || _repuestos.Count > 0
                            || !string.IsNullOrWhiteSpace(txtObservaciones.Text);

            if (hayDatos)
            {
                var confirmar = MessageBox.Show(
                    "¿Seguro que deseas salir? Se perderán los datos ingresados.",
                    "Confirmar cancelación", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirmar != MessageBoxResult.Yes) return;
            }

            Close();
        }

        // ── CÁLCULO DE PRECIOS ───────────────────────────────────────

        private void RecalcularPrecios()
        {
            decimal totalRepuestos = _repuestos.Where(r => r.Incluido).Sum(r => r.Precio * r.Cantidad);
            txtPrecioRepuesto.Text = $"L {totalRepuestos:N2}";

            ParsePrecio(txtPrecioServicio.Text, out decimal servicio);
            txtCostoTotal.Text = $"L {(totalRepuestos + servicio):N2}";
        }

        // ── DIAGNÓSTICO VISUAL ───────────────────────────────────────

        private void Zona_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Shapes.Rectangle rect) return;
            string zona = rect.Tag as string ?? string.Empty;
            if (string.IsNullOrEmpty(zona)) return;

            bool seleccionada = rect.Fill is SolidColorBrush b && b.Color == BrushZonaDañada.Color;

            if (seleccionada)
            {
                rect.Fill = Brushes.Transparent;
                var bloque = panelZonasDañadas.Children
                    .OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Tag as string == zona);
                if (bloque != null) panelZonasDañadas.Children.Remove(bloque);
            }
            else
            {
                rect.Fill = BrushZonaDañada;
                panelZonasDañadas.Children.Add(new TextBlock
                {
                    Text = $"• {zona}",
                    Tag = zona,           // para poder quitarlo por nombre
                    Foreground = Brushes.White,
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 2)
                });
            }

            txtSinZonas.Visibility = panelZonasDañadas.Children.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LimpiarDiagnostico_Click(object sender, RoutedEventArgs e)
        {
            var todosLosCanvas = new[] { canvasAuto, canvasPickup, canvasMoto };
            foreach (var canvas in todosLosCanvas)
                foreach (var rect in canvas.Children.OfType<System.Windows.Shapes.Rectangle>())
                    rect.Fill = Brushes.Transparent;

            panelZonasDañadas.Children.Clear();
            txtSinZonas.Visibility = Visibility.Visible;
        }

        /// <summary>Muestra el canvas del diagrama según el tipo de vehículo (sedán/suv, pickup, moto/mototaxi).</summary>
        private void MostrarCanvasPorTipo(string? tipoVehiculo)
        {
            string tipo = (tipoVehiculo ?? string.Empty).Trim().ToLowerInvariant();

            canvasAuto.Visibility = Visibility.Collapsed;
            canvasPickup.Visibility = Visibility.Collapsed;
            canvasMoto.Visibility = Visibility.Collapsed;

            if (tipo.Contains("pickup"))
                canvasPickup.Visibility = Visibility.Visible;
            else if (tipo.Contains("mototaxi") || tipo.Contains("moto"))
                canvasMoto.Visibility = Visibility.Visible;
            else
                canvasAuto.Visibility = Visibility.Visible; // sedán / SUV / por defecto

            // Al cambiar de diagrama se limpia la selección de zonas dañadas
            panelZonasDañadas.Children.Clear();
            txtSinZonas.Visibility = Visibility.Visible;
        }

        // ── TIPO DE VEHÍCULO ─────────────────────────────────────────

        private Border? _tipoSeleccionado;

        private void TipoVehiculo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;

            // Deseleccionar anterior
            if (_tipoSeleccionado != null)
                _tipoSeleccionado.Background = BrushInactivo;

            // Seleccionar nuevo
            border.Background = BrushActivo;
            _tipoSeleccionado = border;

            string tipo = border.Tag as string ?? "sedan";

            canvasAuto.Visibility = Visibility.Collapsed;
            canvasPickup.Visibility = Visibility.Collapsed;
            canvasMoto.Visibility = Visibility.Collapsed;

            switch (tipo)
            {
                case "pickup":
                    canvasPickup.Visibility = Visibility.Visible;
                    break;
                case "moto":
                case "mototaxi":
                    canvasMoto.Visibility = Visibility.Visible;
                    break;
                default: // sedan, suv
                    canvasAuto.Visibility = Visibility.Visible;
                    break;
            }

            // Limpiar la selección de zonas dañadas al cambiar de tipo de vehículo
            panelZonasDañadas.Children.Clear();
            txtSinZonas.Visibility = Visibility.Visible;
            foreach (var rect in canvasAuto.Children.OfType<System.Windows.Shapes.Rectangle>())
                rect.Fill = Brushes.Transparent;
            foreach (var rect in canvasPickup.Children.OfType<System.Windows.Shapes.Rectangle>())
                rect.Fill = Brushes.Transparent;
            foreach (var rect in canvasMoto.Children.OfType<System.Windows.Shapes.Rectangle>())
                rect.Fill = Brushes.Transparent;
        }

        // ── AUTOCOMPLETADO ───────────────────────────────────────────

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            int limite = _buscarPorDNI ? 13 : 7;
            // Truncar si excede
            if (txtBuscar.Text.Length > limite)
            {
                txtBuscar.Text = txtBuscar.Text[..limite];
                txtBuscar.CaretIndex = limite;
            }
            txtContador.Text = $"{txtBuscar.Text.Length} / {limite}";

            if (txtBuscar.Text.Length < 2)
            {
                borderAutocompletado.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                if (_buscarPorDNI)
                {
                    // Autocompletado por DNI
                    var sugerencias = _db.BuscarClientesPorDNI(txtBuscar.Text.Trim());
                    if (sugerencias.Count == 0)
                    {
                        borderAutocompletado.Visibility = Visibility.Collapsed;
                        return;
                    }
                    lstAutocompletado.ItemsSource = sugerencias
                        .Select(s => s.DNI + " — " + s.NombreCompleto)
                        .ToList();
                    lstAutocompletado.Tag = sugerencias;
                    borderAutocompletado.Visibility = Visibility.Visible;
                }
                else
                {
                    // Autocompletado por Placa
                    var sugerencias = _db.BuscarVehiculosPorPlaca(txtBuscar.Text.Trim().ToUpper());
                    if (sugerencias.Count == 0)
                    {
                        borderAutocompletado.Visibility = Visibility.Collapsed;
                        return;
                    }
                    lstAutocompletado.ItemsSource = sugerencias
                        .Select(v => v.Placa + " — " + v.Modelo)
                        .ToList();
                    lstAutocompletado.Tag = sugerencias;
                    borderAutocompletado.Visibility = Visibility.Visible;
                }
            }
            catch
            {
                borderAutocompletado.Visibility = Visibility.Collapsed;
            }
        }

        private void lstAutocompletado_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstAutocompletado.SelectedIndex < 0) return;

            if (_buscarPorDNI && lstAutocompletado.Tag is List<RepositorioSql.ClienteSugerencia> listaClientes)
            {
                var seleccionado = listaClientes[lstAutocompletado.SelectedIndex];
                txtBuscar.Text = seleccionado.DNI;
                borderAutocompletado.Visibility = Visibility.Collapsed;
                lstAutocompletado.SelectedIndex = -1;
                BuscarPorDNI(seleccionado.DNI);
            }
            else if (!_buscarPorDNI && lstAutocompletado.Tag is List<RepositorioSql.VehiculoSugerencia> listaVehiculos)
            {
                var seleccionado = listaVehiculos[lstAutocompletado.SelectedIndex];
                txtBuscar.Text = seleccionado.Placa;
                borderAutocompletado.Visibility = Visibility.Collapsed;
                lstAutocompletado.SelectedIndex = -1;
                BuscarPorPlaca(seleccionado.Placa);
            }
        }

        // ── HELPERS ──────────────────────────────────────────────────

        private void MostrarError(string mensaje)
        {
            borderError.Visibility = Visibility.Visible;
            txtError.Text = mensaje;
        }

        private void LimpiarResultados()
        {
            borderClienteInfo.Visibility = Visibility.Collapsed;
            borderVehiculoInfo.Visibility = Visibility.Collapsed;
            borderSelectorVehiculo.Visibility = Visibility.Collapsed;
            borderError.Visibility = Visibility.Collapsed;
            _clienteDNI = string.Empty;
            _vehiculoPlaca = string.Empty;
        }

        private void LimpiarFormulario()
        {
            LimpiarResultados();
            txtBuscar.Clear();
            _repuestos.Clear();
            _ordenIDEditar = 0;
            txtPrecioRepuesto.Text = "L 0.00";
            txtPrecioServicio.Text = "L 0.00";
            txtCostoTotal.Text = "L 0.00";
            dpFecha.SelectedDate = DateTime.Today;
            dpEntrega.SelectedDate = null;
            txtObservaciones.Clear();
            cmbEstado.SelectedIndex = -1;
            cmbPrioridad.SelectedIndex = -1;
            LimpiarDiagnostico_Click(this, new RoutedEventArgs());
            SetModoNuevo();
            txtContador.Text = "0 / 13";

            // Reset diagrama a auto por defecto
            if (_tipoSeleccionado != null)
                _tipoSeleccionado.Background = BrushInactivo;
            _tipoSeleccionado = null;
            canvasAuto.Visibility = Visibility.Visible;
            canvasPickup.Visibility = Visibility.Collapsed;
            canvasMoto.Visibility = Visibility.Collapsed;
        }

        private static bool ParsePrecio(string texto, out decimal valor)
        {
            string limpio = texto.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            return decimal.TryParse(limpio,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out valor);
        }

        private static string? ObtenerContenidoCombo(ComboBox combo)
            => (combo.SelectedItem as ComboBoxItem)?.Content?.ToString();

        // ── ARRASTRE DE VENTANA ──────────────────────────────────────

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }


    }
}