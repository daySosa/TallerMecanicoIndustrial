using Login.Clases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Contabilidad
{
    public partial class GestiónEgresos : Window
    {
        private readonly clsConsultasBD _db = new clsConsultasBD();
        private readonly MenúPrincipalEgresos _menuRef;
        private bool _esEdicion = false;
        private int _gastoId = 0;
        private DateTime _fechaRegistro;

        // ── Constructor AGREGAR ──────────────────────────────────────
        public GestiónEgresos(MenúPrincipalEgresos menuRef)
        {
            InitializeComponent();
            _menuRef = menuRef;
            MostrarModoAgregar();
        }

        // ── Constructor EDITAR ───────────────────────────────────────
        public GestiónEgresos(MenúPrincipalEgresos menuRef,
                               int gastoId, string tipo, string nombre,
                               decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _esEdicion = true;
            _gastoId = gastoId;
            _fechaRegistro = fecha;
            MostrarModoActualizar(gastoId, tipo, nombre, precio, fecha, observaciones);
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
            txtTitulo.Text = "Nuevo Gasto";
            txtSubtitulo.Text = "Completa los datos para registrar";
            txtBadge.Text = "NUEVO";
            txtBadge.Foreground = Pincel("#4f6ef7");
            badgeModo.Background = Pincel("#1a1d35");
            btnGuardar.Content = "Guardar Gasto";
        }

        private void txtNombreGasto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Sin restricciones; el nombre puede incluir letras, números y símbolos.
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio.Text = clsValidacionesContabilidad.FormatearPrecioGasto(txtPrecio.Text);

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidacionesContabilidad.LimpiarPrecioGasto(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        // ════════════════════════════════════════════════════════════
        // MODO EDITAR
        // ════════════════════════════════════════════════════════════

        private void MostrarModoActualizar(int gastoId, string tipo, string nombre,
                                            decimal precio, DateTime fecha, string observaciones)
        {
            panelAgregar.Visibility = Visibility.Collapsed;
            panelEditar.Visibility = Visibility.Visible;
            iconAgregar.Visibility = Visibility.Collapsed;
            iconEditar.Visibility = Visibility.Visible;
            txtTitulo.Text = "Editar Gasto";
            txtSubtitulo.Text = "Modifica los datos del gasto";
            txtBadge.Text = "EDITAR";
            txtBadge.Foreground = Pincel("#4CAF50");
            badgeModo.Background = Pincel("#1a2b1a");
            btnGuardar.Content = "Guardar Cambios";

            foreach (ComboBoxItem item in cmbTipoGasto_Edit.Items)
            {
                if (item.Content?.ToString() == tipo)
                {
                    cmbTipoGasto_Edit.SelectedItem = item;
                    break;
                }
            }

            txtNombre_Edit.Text = nombre;
            txtPrecio_Edit.Text = "L " + precio.ToString("N2");
            txtFecha_Edit.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                                             new System.Globalization.CultureInfo("es-ES"));
            txtObservaciones_Edit.Text = observaciones ?? "";

            VerificarBloqueoEdicion();
        }

        private void VerificarBloqueoEdicion()
        {
            if ((DateTime.Now - _fechaRegistro).TotalDays >= 1)
            {
                cmbTipoGasto_Edit.IsEnabled = false;
                txtNombre_Edit.IsEnabled = false;
                txtPrecio_Edit.IsEnabled = false;
                txtObservaciones_Edit.IsEnabled = false;
                btnGuardar.IsEnabled = false;
                borderBloqueado.Visibility = Visibility.Visible;
            }
        }

        private void txtPrecio_Edit_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio_Edit.Text = clsValidacionesContabilidad.FormatearPrecioGasto(txtPrecio_Edit.Text);

        private void txtPrecio_Edit_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio_Edit.Text = clsValidacionesContabilidad.LimpiarPrecioGasto(txtPrecio_Edit.Text);
            txtPrecio_Edit.CaretIndex = txtPrecio_Edit.Text.Length;
        }

        // ════════════════════════════════════════════════════════════
        // GUARDAR
        // ════════════════════════════════════════════════════════════

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_esEdicion) GuardarEdicion();
            else GuardarNuevo();
        }

        private void GuardarNuevo()
        {
            OcultarMensajeAgregar();

            string tipo = (cmbTipoGasto.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string nombre = txtNombreGasto.Text.Trim();
            string observaciones = txtObservaciones.Text.Trim();

            if (string.IsNullOrWhiteSpace(tipo))
            {
                MostrarMensajeAgregar("⚠ Selecciona el tipo de gasto.");
                return;
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarMensajeAgregar("⚠ Ingresa el nombre del gasto.");
                return;
            }
            if (!clsValidacionesContabilidad.ValidarMontoPago(txtPrecio.Text, out decimal precio))
            {
                MostrarMensajeAgregar("⚠ Ingresa un precio válido.");
                return;
            }

            try
            {
                _db.AgregarGasto(tipo, nombre, observaciones, precio);
                MessageBox.Show("✅ ¡Gasto registrado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarEgreso();
                this.Close();
            }
            catch (Exception ex) { MostrarMensajeAgregar("⚠ " + ex.Message); }
        }

        private void GuardarEdicion()
        {
            OcultarMensajeEditar();

            string tipo = (cmbTipoGasto_Edit.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string nombre = txtNombre_Edit.Text.Trim();
            string observaciones = txtObservaciones_Edit.Text.Trim();

            if (string.IsNullOrWhiteSpace(tipo))
            {
                MostrarMensajeEditar("⚠ Selecciona el tipo de gasto.");
                return;
            }
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MostrarMensajeEditar("⚠ Ingresa el nombre del gasto.");
                return;
            }
            if (!clsValidacionesContabilidad.ValidarMontoPago(txtPrecio_Edit.Text, out decimal precio))
            {
                MostrarMensajeEditar("⚠ Ingresa un precio válido.");
                return;
            }

            try
            {
                _db.ActualizarGasto(_gastoId, tipo, nombre, observaciones, precio, _fechaRegistro);
                MessageBox.Show("✅ ¡Gasto actualizado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarEgreso();
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
            txtMensajeEditar.Text = msg;
            borderMensajeEditar.Visibility = Visibility.Visible;
        }

        private void OcultarMensajeEditar()
            => borderMensajeEditar.Visibility = Visibility.Collapsed;

        // ── Helper ───────────────────────────────────────────────────
        private static SolidColorBrush Pincel(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));
    }
}