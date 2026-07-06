using Login.Clases;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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

        // Colores reutilizables (UI 2D: tabs, botones de tipo de vehículo, etc.)
        private static readonly SolidColorBrush BrushActivo =
            new(Color.FromRgb(0x4f, 0x6e, 0xf7));
        private static readonly SolidColorBrush BrushInactivo =
            new(Color.FromRgb(0x1e, 0x22, 0x35));
        private static readonly SolidColorBrush BrushTextoInactivo =
            new(Color.FromRgb(0x50, 0x58, 0x80));

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

            // Vehículo 3D inicial: sedán/SUV por defecto
            MostrarVehiculo3D("sedan");

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

                // Mostrar el vehículo 3D correspondiente al tipo cargado
                MostrarVehiculo3D(orden.vehiculoTipo);

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

                MostrarVehiculo3D(r.vehiculoTipo);

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

            MostrarVehiculo3D(v.Tipo);
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

        // ══════════════════════════════════════════════════════════════
        // ── DIAGNÓSTICO VISUAL 3D (vista 360° orbitable) ────────────────
        // ══════════════════════════════════════════════════════════════

        private readonly Dictionary<GeometryModel3D, string> _zonaPorModelo = new();
        private readonly Dictionary<string, List<GeometryModel3D>> _modelosPorZona = new();
        private readonly HashSet<string> _zonasSeleccionadas = new();

        private static readonly Material MaterialZonaNormal =
            CrearMaterialZona(Color.FromArgb(40, 0x4f, 0x6e, 0xf7));
        private static readonly Material MaterialZonaDañada =
            CrearMaterialZona(Color.FromArgb(170, 0xff, 0x53, 0x53));

        private static Material CrearMaterialZona(Color color)
        {
            var grupo = new MaterialGroup();
            grupo.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            return grupo;
        }

        private enum Eje { X, Y, Z }

        // ── Cámara orbital ──

        private bool _arrastrando3D = false;
        private bool _huboArrastreReal = false;
        private System.Windows.Point _puntoInicioArrastre3D;
        private double _thetaInicioArrastre, _phiInicioArrastre;

        private double _orbitTheta = 35;   // ángulo horizontal (grados)
        private double _orbitPhi = 18;     // ángulo de elevación (grados)
        private double _orbitRadius = 480; // distancia de la cámara

        private void ActualizarCamara()
        {
            double thetaRad = _orbitTheta * Math.PI / 180.0;
            double phiRad = _orbitPhi * Math.PI / 180.0;

            double x = _orbitRadius * Math.Cos(phiRad) * Math.Sin(thetaRad);
            double y = _orbitRadius * Math.Sin(phiRad) + 40;
            double z = _orbitRadius * Math.Cos(phiRad) * Math.Cos(thetaRad);

            var posicion = new Point3D(x, y, z);
            var objetivo = new Point3D(0, 40, 0);

            camaraOrbit.Position = posicion;
            camaraOrbit.LookDirection = objetivo - posicion;
        }

        private void Viewport3D_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _arrastrando3D = true;
            _huboArrastreReal = false;
            _puntoInicioArrastre3D = e.GetPosition(this);
            _thetaInicioArrastre = _orbitTheta;
            _phiInicioArrastre = _orbitPhi;
            ((UIElement)sender).CaptureMouse();
        }

        private void Viewport3D_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_arrastrando3D) return;

            var puntoActual = e.GetPosition(this);
            double deltaX = puntoActual.X - _puntoInicioArrastre3D.X;
            double deltaY = puntoActual.Y - _puntoInicioArrastre3D.Y;

            if (Math.Abs(deltaX) > 4 || Math.Abs(deltaY) > 4)
                _huboArrastreReal = true;

            _orbitTheta = _thetaInicioArrastre - deltaX * 0.35;
            _orbitPhi = Math.Clamp(_phiInicioArrastre + deltaY * 0.25, 4, 80);

            ActualizarCamara();
        }

        private void Viewport3D_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_arrastrando3D) return;

            _arrastrando3D = false;
            ((UIElement)sender).ReleaseMouseCapture();

            if (!_huboArrastreReal)
            {
                // No hubo arrastre real: se interpreta como un clic → intentar seleccionar zona
                var punto = e.GetPosition(viewport3D);
                var resultado = VisualTreeHelper.HitTest(viewport3D, punto) as RayMeshGeometry3DHitTestResult;

                if (resultado?.ModelHit is GeometryModel3D modelo &&
                    _zonaPorModelo.TryGetValue(modelo, out string zona))
                {
                    AlternarZona3D(zona);
                }
            }
        }

        private void Viewport3D_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_arrastrando3D)
            {
                _arrastrando3D = false;
                ((UIElement)sender).ReleaseMouseCapture();
            }
        }

        private void Viewport3D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _orbitRadius = Math.Clamp(_orbitRadius - e.Delta * 0.4, 220, 900);
            ActualizarCamara();
        }

        // ── Selección de zonas ──

        private void AlternarZona3D(string zona)
        {
            if (string.IsNullOrEmpty(zona) || !_modelosPorZona.TryGetValue(zona, out var modelos))
                return;

            bool estabaSeleccionada = _zonasSeleccionadas.Contains(zona);

            if (estabaSeleccionada)
            {
                _zonasSeleccionadas.Remove(zona);
                foreach (var modelo in modelos)
                    modelo.Material = modelo.BackMaterial = MaterialZonaNormal;

                var bloque = panelZonasDañadas.Children
                    .OfType<TextBlock>()
                    .FirstOrDefault(tb => tb.Tag as string == zona);
                if (bloque != null) panelZonasDañadas.Children.Remove(bloque);
            }
            else
            {
                _zonasSeleccionadas.Add(zona);
                foreach (var modelo in modelos)
                    modelo.Material = modelo.BackMaterial = MaterialZonaDañada;

                panelZonasDañadas.Children.Add(new TextBlock
                {
                    Text = $"• {zona}",
                    Tag = zona,
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
            foreach (var zona in _zonasSeleccionadas.ToList())
            {
                if (_modelosPorZona.TryGetValue(zona, out var modelos))
                    foreach (var modelo in modelos)
                        modelo.Material = modelo.BackMaterial = MaterialZonaNormal;
            }
            _zonasSeleccionadas.Clear();
            panelZonasDañadas.Children.Clear();
            txtSinZonas.Visibility = Visibility.Visible;
        }

        // ── Construcción de modelos 3D por tipo de vehículo ──

        /// <summary>Construye y muestra el modelo 3D según el tipo de vehículo (sedán/suv, pickup, moto/mototaxi).</summary>
        private void MostrarVehiculo3D(string? tipoVehiculo)
        {
            string tipo = (tipoVehiculo ?? string.Empty).Trim().ToLowerInvariant();

            _zonaPorModelo.Clear();
            _modelosPorZona.Clear();
            _zonasSeleccionadas.Clear();
            panelZonasDañadas.Children.Clear();
            txtSinZonas.Visibility = Visibility.Visible;

            Model3DGroup nuevoModelo;
            if (tipo.Contains("pickup"))
                nuevoModelo = ConstruirPickup();
            else if (tipo.Contains("mototaxi") || tipo.Contains("moto"))
                nuevoModelo = ConstruirMoto();
            else
                nuevoModelo = ConstruirAuto(); // sedán / SUV / por defecto

            visualVehiculo.Content = nuevoModelo;

            // Reiniciar cámara a la posición inicial
            _orbitTheta = 35;
            _orbitPhi = 18;
            _orbitRadius = 480;
            ActualizarCamara();
        }

        private void RegistrarZona(Model3DGroup grupo, GeometryModel3D modelo, string zona)
        {
            grupo.Children.Add(modelo);
            _zonaPorModelo[modelo] = zona;

            if (!_modelosPorZona.TryGetValue(zona, out var lista))
            {
                lista = new List<GeometryModel3D>();
                _modelosPorZona[zona] = lista;
            }
            lista.Add(modelo);
        }

        private Model3DGroup ConstruirAuto()
        {
            var grupo = new Model3DGroup();

            var matCarroceria = MaterialSolido(Color.FromRgb(0x2e, 0x32, 0x50));
            var matCabina = MaterialSolido(Color.FromArgb(215, 0x10, 0x13, 0x1c));
            var matRueda = MaterialSolido(Color.FromRgb(0x0a, 0x0c, 0x14));
            var matRin = MaterialSolido(Color.FromRgb(0x4f, 0x6e, 0xf7));

            // Piso de referencia
            grupo.Children.Add(CrearCilindro(0, -1, 0, 220, 2, MaterialSolido(Color.FromRgb(0x13, 0x16, 0x1f))));

            // Chasis y cabina
            grupo.Children.Add(CrearCaja(0, 35, 0, 150, 70, 360, matCarroceria));
            grupo.Children.Add(CrearCaja(0, 97, -20, 108, 54, 170, matCabina));

            // Ruedas + rines
            double[] xsRueda = { -68, 68 };
            double[] zsRueda = { 125, -125 };
            foreach (var x in xsRueda)
                foreach (var z in zsRueda)
                {
                    grupo.Children.Add(CrearCilindro(x, 34, z, 34, 24, matRueda, Eje.X));
                    grupo.Children.Add(CrearCilindro(x * 0.94, 34, z, 15, 25, matRin, Eje.X));
                }

            // ── Zonas de diagnóstico ──
            RegistrarZona(grupo, CrearCaja(0, 55, 140, 140, 60, 80, MaterialZonaNormal), "Motor");
            RegistrarZona(grupo, CrearCaja(0, 25, 0, 120, 40, 140, MaterialZonaNormal), "Transmisión");
            RegistrarZona(grupo, CrearCaja(0, 55, -145, 140, 60, 75, MaterialZonaNormal), "Cajuela / Baúl");

            RegistrarZona(grupo, CrearCaja(-68, 34, 125, 46, 46, 46, MaterialZonaNormal), "Freno Del. Izq.");
            RegistrarZona(grupo, CrearCaja(68, 34, 125, 46, 46, 46, MaterialZonaNormal), "Freno Del. Der.");
            RegistrarZona(grupo, CrearCaja(-68, 34, -125, 46, 46, 46, MaterialZonaNormal), "Freno Tras. Izq.");
            RegistrarZona(grupo, CrearCaja(68, 34, -125, 46, 46, 46, MaterialZonaNormal), "Freno Tras. Der.");

            RegistrarZona(grupo, CrearCaja(0, 12, 105, 170, 16, 50, MaterialZonaNormal), "Suspensión Del.");
            RegistrarZona(grupo, CrearCaja(0, 12, -105, 170, 16, 50, MaterialZonaNormal), "Suspensión Tras.");

            RegistrarZona(grupo, CrearCilindro(60, 14, 0, 9, 170, MaterialZonaNormal, Eje.Z), "Sistema de Escape");

            return grupo;
        }

        private Model3DGroup ConstruirPickup()
        {
            var grupo = new Model3DGroup();

            var matCarroceria = MaterialSolido(Color.FromRgb(0x2e, 0x32, 0x50));
            var matCabina = MaterialSolido(Color.FromArgb(215, 0x10, 0x13, 0x1c));
            var matRueda = MaterialSolido(Color.FromRgb(0x0a, 0x0c, 0x14));
            var matRin = MaterialSolido(Color.FromRgb(0x4f, 0x6e, 0xf7));
            var matCaja = MaterialSolido(Color.FromRgb(0x22, 0x25, 0x40));

            grupo.Children.Add(CrearCilindro(0, -1, 0, 220, 2, MaterialSolido(Color.FromRgb(0x13, 0x16, 0x1f))));

            // Chasis delantero (motor + cabina)
            grupo.Children.Add(CrearCaja(0, 35, 60, 150, 70, 220, matCarroceria));
            grupo.Children.Add(CrearCaja(0, 92, 40, 108, 48, 150, matCabina));

            // Caja de carga (parte trasera abierta)
            grupo.Children.Add(CrearCaja(0, 45, -110, 150, 50, 160, matCaja));

            double[] xsRueda = { -68, 68 };
            double[] zsRueda = { 100, -110 };
            foreach (var x in xsRueda)
                foreach (var z in zsRueda)
                {
                    grupo.Children.Add(CrearCilindro(x, 34, z, 34, 24, matRueda, Eje.X));
                    grupo.Children.Add(CrearCilindro(x * 0.94, 34, z, 15, 25, matRin, Eje.X));
                }

            RegistrarZona(grupo, CrearCaja(0, 55, 130, 140, 60, 80, MaterialZonaNormal), "Motor");
            RegistrarZona(grupo, CrearCaja(0, 55, -110, 158, 60, 168, MaterialZonaNormal), "Caja de Carga");

            RegistrarZona(grupo, CrearCaja(-68, 34, 100, 46, 46, 46, MaterialZonaNormal), "Freno Del. Izq.");
            RegistrarZona(grupo, CrearCaja(68, 34, 100, 46, 46, 46, MaterialZonaNormal), "Freno Del. Der.");
            RegistrarZona(grupo, CrearCaja(-68, 34, -110, 46, 46, 46, MaterialZonaNormal), "Freno Tras. Izq.");
            RegistrarZona(grupo, CrearCaja(68, 34, -110, 46, 46, 46, MaterialZonaNormal), "Freno Tras. Der.");

            return grupo;
        }

        private Model3DGroup ConstruirMoto()
        {
            var grupo = new Model3DGroup();

            var matRueda = MaterialSolido(Color.FromRgb(0x0a, 0x0c, 0x14));
            var matRin = MaterialSolido(Color.FromRgb(0x4f, 0x6e, 0xf7));
            var matMarco = MaterialSolido(Color.FromRgb(0x2e, 0x32, 0x50));
            var matAsiento = MaterialSolido(Color.FromRgb(0x13, 0x16, 0x1f));

            grupo.Children.Add(CrearCilindro(0, -1, 0, 170, 2, MaterialSolido(Color.FromRgb(0x13, 0x16, 0x1f))));

            // Ruedas (delantera y trasera)
            grupo.Children.Add(CrearCilindro(0, 40, 95, 40, 20, matRueda, Eje.X));
            grupo.Children.Add(CrearCilindro(0, 40, 95, 16, 21, matRin, Eje.X));
            grupo.Children.Add(CrearCilindro(0, 40, -95, 40, 20, matRueda, Eje.X));
            grupo.Children.Add(CrearCilindro(0, 40, -95, 16, 21, matRin, Eje.X));

            // Marco central, asiento y manubrio
            grupo.Children.Add(CrearCaja(0, 55, 0, 18, 14, 190, matMarco));
            grupo.Children.Add(CrearCaja(0, 68, -35, 26, 12, 60, matAsiento));
            grupo.Children.Add(CrearCilindro(0, 85, 90, 4, 60, matMarco, Eje.X));

            // ── Zonas de diagnóstico ──
            RegistrarZona(grupo, CrearCaja(0, 55, 40, 80, 55, 70, MaterialZonaNormal), "Motor");
            RegistrarZona(grupo, CrearCaja(0, 45, 0, 30, 20, 150, MaterialZonaNormal), "Transmisión / Cadena");
            RegistrarZona(grupo, CrearCaja(0, 40, 95, 60, 55, 55, MaterialZonaNormal), "Freno Delantero");
            RegistrarZona(grupo, CrearCaja(0, 40, -95, 60, 55, 55, MaterialZonaNormal), "Freno Trasero");
            RegistrarZona(grupo, CrearCilindro(25, 25, -40, 6, 90, MaterialZonaNormal, Eje.Z), "Sistema de Escape");

            return grupo;
        }

        // ── Helpers de geometría 3D ──

        private static Material MaterialSolido(Color color)
        {
            var grupo = new MaterialGroup();
            grupo.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            grupo.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)), 30));
            return grupo;
        }

        private static GeometryModel3D CrearCaja(double centroX, double centroY, double centroZ,
            double ancho, double alto, double profundo, Material material)
        {
            var mesh = ObtenerMeshCaja(ancho, alto, profundo);
            var modelo = new GeometryModel3D(mesh, material) { BackMaterial = material };
            modelo.Transform = new TranslateTransform3D(centroX, centroY, centroZ);
            return modelo;
        }

        private static GeometryModel3D CrearCilindro(double centroX, double centroY, double centroZ,
            double radio, double altura, Material material, Eje eje = Eje.Y)
        {
            var mesh = ObtenerMeshCilindro(radio, altura);
            var modelo = new GeometryModel3D(mesh, material) { BackMaterial = material };

            var grupoTransform = new Transform3DGroup();
            switch (eje)
            {
                case Eje.X:
                    grupoTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
                    break;
                case Eje.Z:
                    grupoTransform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
                    break;
            }
            grupoTransform.Children.Add(new TranslateTransform3D(centroX, centroY, centroZ));
            modelo.Transform = grupoTransform;
            return modelo;
        }

        private static MeshGeometry3D ObtenerMeshCaja(double dx, double dy, double dz)
        {
            double x = dx / 2, y = dy / 2, z = dz / 2;
            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(-x, -y, -z)); // 0
            mesh.Positions.Add(new Point3D(x, -y, -z));  // 1
            mesh.Positions.Add(new Point3D(x, y, -z));   // 2
            mesh.Positions.Add(new Point3D(-x, y, -z));  // 3
            mesh.Positions.Add(new Point3D(-x, -y, z));  // 4
            mesh.Positions.Add(new Point3D(x, -y, z));   // 5
            mesh.Positions.Add(new Point3D(x, y, z));    // 6
            mesh.Positions.Add(new Point3D(-x, y, z));   // 7

            int[] tris =
            {
                0,1,2, 0,2,3, // atrás
                5,4,7, 5,7,6, // frente
                4,0,3, 4,3,7, // izquierda
                1,5,6, 1,6,2, // derecha
                3,2,6, 3,6,7, // arriba
                4,5,1, 4,1,0  // abajo
            };
            foreach (var i in tris) mesh.TriangleIndices.Add(i);

            return mesh;
        }

        private static MeshGeometry3D ObtenerMeshCilindro(double radio, double altura, int segmentos = 16)
        {
            var mesh = new MeshGeometry3D();
            double mitad = altura / 2;

            for (int i = 0; i < segmentos; i++)
            {
                double angulo = 2 * Math.PI * i / segmentos;
                double xx = radio * Math.Cos(angulo);
                double zz = radio * Math.Sin(angulo);
                mesh.Positions.Add(new Point3D(xx, -mitad, zz)); // anillo inferior
                mesh.Positions.Add(new Point3D(xx, mitad, zz));  // anillo superior
            }

            // Caras laterales
            for (int i = 0; i < segmentos; i++)
            {
                int sig = (i + 1) % segmentos;
                int b0 = i * 2, t0 = i * 2 + 1, b1 = sig * 2, t1 = sig * 2 + 1;
                mesh.TriangleIndices.Add(b0); mesh.TriangleIndices.Add(b1); mesh.TriangleIndices.Add(t0);
                mesh.TriangleIndices.Add(t0); mesh.TriangleIndices.Add(b1); mesh.TriangleIndices.Add(t1);
            }

            // Tapas
            int centroInf = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(0, -mitad, 0));
            int centroSup = mesh.Positions.Count;
            mesh.Positions.Add(new Point3D(0, mitad, 0));

            for (int i = 0; i < segmentos; i++)
            {
                int sig = (i + 1) % segmentos;
                int b0 = i * 2, b1 = sig * 2;
                mesh.TriangleIndices.Add(centroInf); mesh.TriangleIndices.Add(b0); mesh.TriangleIndices.Add(b1);

                int t0 = i * 2 + 1, t1 = sig * 2 + 1;
                mesh.TriangleIndices.Add(centroSup); mesh.TriangleIndices.Add(t1); mesh.TriangleIndices.Add(t0);
            }

            return mesh;
        }

        // ── TIPO DE VEHÍCULO ─────────────────────────────────────────

        private Border? _tipoSeleccionado;

        private void TipoVehiculo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;

            if (_tipoSeleccionado != null)
                _tipoSeleccionado.Background = BrushInactivo;

            border.Background = BrushActivo;
            _tipoSeleccionado = border;

            string tipo = border.Tag as string ?? "sedan";
            MostrarVehiculo3D(tipo);
        }

        // ── AUTOCOMPLETADO ───────────────────────────────────────────

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            int limite = _buscarPorDNI ? 13 : 7;
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
            SetModoNuevo();
            txtContador.Text = "0 / 13";

            if (_tipoSeleccionado != null)
                _tipoSeleccionado.Background = BrushInactivo;
            _tipoSeleccionado = null;

            MostrarVehiculo3D("sedan");
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