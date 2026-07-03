using Login.Clases;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Contabilidad
{
    public partial class GestiónIngresos : Window
    {
        private readonly RepositorioSql _db = new RepositorioSql();
        private readonly MenúPrincipalIngresos _menuRef;
        private bool _esEdicion = false;
        private int _pagoId = 0;
        private DateTime _fechaRegistro;

        // ── Constructor AGREGAR (1 argumento) ────────────────────────
        public GestiónIngresos(MenúPrincipalIngresos menuRef)
        {
            InitializeComponent();
            _menuRef = menuRef;
            MostrarModoAgregar();
        }

        // ── Constructor EDITAR (6 argumentos) ───────────────────────
        public GestiónIngresos(MenúPrincipalIngresos menuRef,
                                int pagoId, string dni, int ordenId,
                                decimal monto, DateTime fecha)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _esEdicion = true;
            _pagoId = pagoId;
            _fechaRegistro = fecha;
            MostrarModoActualizar(pagoId, dni, ordenId, monto, fecha);
        }

        // ════════════════════════════════════════════════════════════
        // MODO AGREGAR
        // ════════════════════════════════════════════════════════════

        private void MostrarModoAgregar()
        {
            panelAgregar.Visibility = Visibility.Visible;
            panelEditar.Visibility = Visibility.Collapsed;
            iconAgregar.Visibility = Visibility.Visible;
            iconEditar.Visibility = Visibility.Collapsed;
            txtTitulo.Text = "Nuevo Pago";
            txtSubtitulo.Text = "Completa los datos para registrar";
            txtBadge.Text = "NUEVO";
            txtBadge.Foreground = Pincel("#4f6ef7");
            badgeModo.Background = Pincel("#1a1d35");
            btnGuardar.Content = "Guardar Pago";
        }

        private void txtDNI_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtNombre != null) txtNombre.Text = "";
            if (panelOrdenes != null) panelOrdenes.Visibility = Visibility.Collapsed;
            if (dgOrdenes != null) dgOrdenes.ItemsSource = null;
            if (txtOrdenID != null) txtOrdenID.Text = "";
            if (txtMonto != null) txtMonto.Text = "";
            OcultarMensajeAgregar();
        }

        private void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            OcultarMensajeAgregar();
            string dni = txtDNI.Text.Trim();
            if (!clsValidacionesContabilidad.ValidarDNIBusqueda(dni)) return;
            BuscarClienteAgregar(dni);
        }

        private void BuscarClienteAgregar(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni)) return;
            try
            {
                var (nombres, apellidos) = _db.BuscarNombreCliente(dni);
                if (nombres != null)
                {
                    txtNombre.Text = $"{nombres} {apellidos}";
                    txtNombre.Foreground = Brushes.White;
                    CargarOrdenesCliente(dni);
                }
                else
                {
                    txtNombre.Text = "";
                    panelOrdenes.Visibility = Visibility.Collapsed;
                    MostrarMensajeAgregar("No se encontró ningún cliente con ese DNI.");
                }
            }
            catch (Exception ex) { MostrarMensajeAgregar("Error al buscar cliente: " + ex.Message); }
        }

        private void CargarOrdenesCliente(string dni)
        {
            try
            {
                var dt = _db.ObtenerOrdenesFinalizadasSinPago(dni);

                if (dt.Rows.Count > 0)
                {
                    dgOrdenes.ItemsSource = dt.DefaultView;
                    panelOrdenes.Visibility = Visibility.Visible;
                }
                else
                {
                    dgOrdenes.ItemsSource = null;
                    panelOrdenes.Visibility = Visibility.Collapsed;
                    MostrarMensajeAgregar("Este cliente no tiene órdenes finalizadas sin pago.");
                }
            }
            catch (Exception ex) { MostrarMensajeAgregar("Error al cargar órdenes: " + ex.Message); }
        }

        private void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is DataRowView row)
                txtOrdenID.Text = row["Orden_ID"].ToString();
        }

        private void txtOrdenID_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarMensajeAgregar();
            if (!int.TryParse(txtOrdenID.Text.Trim(), out int ordenId))
            {
                txtMonto.Text = "L 0.00";
                return;
            }
            try
            {
                decimal? total = _db.ObtenerTotalOrden(ordenId);
                txtMonto.Text = total.HasValue ? "L " + total.Value.ToString("N2") : "L 0.00";
            }
            catch { txtMonto.Text = "L 0.00"; }
        }

        // ════════════════════════════════════════════════════════════
        // MODO EDITAR
        // ════════════════════════════════════════════════════════════

        private void MostrarModoActualizar(int pagoId, string dni,
                                            int ordenId, decimal monto, DateTime fecha)
        {
            panelAgregar.Visibility = Visibility.Collapsed;
            panelEditar.Visibility = Visibility.Visible;
            iconAgregar.Visibility = Visibility.Collapsed;
            iconEditar.Visibility = Visibility.Visible;
            txtTitulo.Text = "Editar Pago";
            txtSubtitulo.Text = "Modifica los datos del pago";
            txtBadge.Text = "EDITAR";
            txtBadge.Foreground = Pincel("#4CAF50");
            badgeModo.Background = Pincel("#1a2b1a");
            btnGuardar.Content = "Guardar Cambios";

            txtDNI_Edit.Text = dni;
            txtOrdenID_Edit.Text = ordenId.ToString();
            txtPrecio.Text = "L " + monto.ToString("N2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                                          new System.Globalization.CultureInfo("es-ES"));

            BuscarNombreEditar(dni);
            VerificarBloqueoEdicion();
        }

        private void VerificarBloqueoEdicion()
        {
            if ((DateTime.Now - _fechaRegistro).TotalDays >= 1)
            {
                txtDNI_Edit.IsEnabled = false;
                txtOrdenID_Edit.IsEnabled = false;
                txtPrecio.IsEnabled = false;
                btnGuardar.IsEnabled = false;
                borderBloqueado.Visibility = Visibility.Visible;
            }
        }

        private void txtDNI_Edit_TextChanged(object sender, TextChangedEventArgs e)
            => BuscarNombreEditar(txtDNI_Edit.Text.Trim());

        private void BuscarNombreEditar(string dni)
        {
            if (string.IsNullOrEmpty(dni)) { txtNombreCliente.Text = ""; return; }
            try
            {
                var (nombres, apellidos) = _db.BuscarNombreCliente(dni);
                if (nombres != null)
                {
                    txtNombreCliente.Text = $"{nombres} {apellidos}";
                    txtNombreCliente.Foreground = Brushes.White;
                    OcultarMensajeEditar();
                }
                else
                {
                    txtNombreCliente.Text = "";
                    MostrarMensajeEditar("No se encontró ningún cliente con ese DNI.");
                }
            }
            catch (Exception ex) { MostrarMensajeEditar("Error: " + ex.Message); }
        }

        private void txtOrdenID_Edit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(txtOrdenID_Edit.Text.Trim(), out int ordenId))
            {
                txtPrecio.Text = "L 0.00";
                return;
            }
            try
            {
                decimal? total = _db.ObtenerTotalOrden(ordenId);
                txtPrecio.Text = total.HasValue ? "L " + total.Value.ToString("N2") : "L 0.00";
            }
            catch { txtPrecio.Text = "L 0.00"; }
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio.Text = clsValidacionesContabilidad.FormatearPrecioGasto(txtPrecio.Text);

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidacionesContabilidad.LimpiarPrecioGasto(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        // ════════════════════════════════════════════════════════════
        // GUARDAR
        // ════════════════════════════════════════════════════════════

        private void GuardarNuevo()
        {
            OcultarMensajeAgregar();
            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();

            if (!clsValidacionesContabilidad.ValidarFormularioVacio(dni, ordenStr)) return;
            if (!clsValidacionesContabilidad.ValidarClienteBuscado(dni, txtNombre.Text)) return;
            if (!ValidacionesGenerales.ValidarTextoRequerido(txtNombre.Text,
                    "⚠ Busca un cliente válido antes de guardar.", MostrarMensajeAgregar)) return;
            if (!clsValidacionesContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;

            try
            {
                decimal? total = _db.ObtenerTotalOrden(ordenId);
                if (!total.HasValue)
                {
                    MostrarMensajeAgregar("⚠ No se encontró la orden especificada.");
                    return;
                }
                _db.RegistrarPago(dni, ordenId, total.Value);

                _db.RegistrarBitacora(SesionActual.Email, "Ingresos", "Agregar",
                    $"Pago Orden #{ordenId} - Cliente {dni}, L {total.Value:N2}");

                MessageBox.Show("✅ ¡Pago registrado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarPago();
                this.Close();
            }
            catch (Exception ex) { MostrarMensajeAgregar("⚠ " + ex.Message); }
        }

        private void GuardarEdicion()
        {
            OcultarMensajeEditar();
            string dni = txtDNI_Edit.Text.Trim();
            string ordenStr = txtOrdenID_Edit.Text.Trim();

            if (!clsValidacionesContabilidad.ValidarFormularioVacio(
                    dni, ordenStr, txtPrecio.Text)) return;
            if (!clsValidacionesContabilidad.ValidarDNIPago(dni, MostrarMensajeEditar)) return;
            if (!ValidacionesGenerales.ValidarTextoRequerido(txtNombreCliente.Text,
                    "⚠ Ingresa un DNI válido.", MostrarMensajeEditar)) return;
            if (!clsValidacionesContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;
            if (!clsValidacionesContabilidad.ValidarMontoPago(txtPrecio.Text, out decimal monto)) return;

            try
            {
                _db.ActualizarPago(_pagoId, dni, ordenId, monto);

                _db.RegistrarBitacora(SesionActual.Email, "Ingresos", "Actualizar",
                    $"Pago #{_pagoId} - Orden #{ordenId}, L {monto:N2}");

                MessageBox.Show("✅ ¡Pago actualizado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarPago();
                this.Close();
            }
            catch (Exception ex) { MostrarMensajeEditar("⚠ Error al actualizar: " + ex.Message); }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        // ── Mensajes ─────────────────────────────────────────────────
        private void MostrarMensajeAgregar(string msg)
        {
            txtMensaje.Text = msg;
            borderMensajeAgregar.Visibility = Visibility.Visible;
        }
        private void OcultarMensajeAgregar()
            => borderMensajeAgregar.Visibility = Visibility.Collapsed;

        private void MostrarMensajeEditar(string msg)
        {
            txtMensajeDNI.Text = msg;
            borderMensajeEditar.Visibility = Visibility.Visible;
        }
        private void OcultarMensajeEditar()
            => borderMensajeEditar.Visibility = Visibility.Collapsed;

        // ── Helpers ──────────────────────────────────────────────────
        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

    }
}