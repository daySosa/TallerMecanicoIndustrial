using Login.Clases;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Contabilidad
{
    /// <summary>
    /// Ventana de gestión de gastos (egresos): permite registrar un nuevo gasto o editar uno existente.
    /// La ventana nace invisible (Opacity="0" en el Window) y su Border raíz nace ligeramente reducido
    /// (ScaleTransform en el XAML) para una transición de apertura fluida sin parpadeos; se anima
    /// simétricamente al cerrarse.
    /// </summary>
    public partial class GestiónEgresos : Window
    {
        // ===== Recursos cacheados (estáticos) =====
        private static readonly SolidColorBrush BrushAcentoNuevo = new(Color.FromRgb(0x4f, 0x6e, 0xf7));
        private static readonly SolidColorBrush BrushFondoNuevo = new(Color.FromRgb(0x1a, 0x1d, 0x35));
        private static readonly SolidColorBrush BrushAcentoEditar = new(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush BrushFondoEditar = new(Color.FromRgb(0x1a, 0x2b, 0x1a));
        private static readonly CultureInfo CulturaFecha = new("es-HN");

        // Duración compartida para las animaciones de apertura/cierre.
        private static readonly Duration DuracionAnimacion = new(TimeSpan.FromMilliseconds(220));

        private readonly RepositorioSql _db = new();
        private readonly MenúPrincipalEgresos _menuRef;

        private bool _esEdicion;
        private int _gastoId;
        private DateTime _fechaRegistro;

        // ════════════════════════════════════════════════════════════
        // CONSTRUCTORES
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Crea la ventana en modo <b>Agregar</b>, para registrar un nuevo gasto.
        /// </summary>
        /// <param name="menuRef">Referencia al menú principal, usada para refrescar la lista al guardar.</param>
        public GestiónEgresos(MenúPrincipalEgresos menuRef)
        {
            InitializeComponent();
            _menuRef = menuRef;

            MostrarModoAgregar();
            Loaded += GestiónEgresos_Loaded;
        }

        /// <summary>
        /// Crea la ventana en modo <b>Editar</b>, precargando los datos del gasto indicado.
        /// </summary>
        /// <param name="menuRef">Referencia al menú principal, usada para refrescar la lista al guardar.</param>
        /// <param name="gastoId">Identificador del gasto a editar.</param>
        /// <param name="tipo">Tipo de gasto actual ("Gasto en Repuesto" o "Gasto Adicional").</param>
        /// <param name="nombre">Nombre o descripción actual del gasto.</param>
        /// <param name="precio">Monto actual del gasto.</param>
        /// <param name="fecha">Fecha de registro original del gasto (determina el bloqueo por antigüedad).</param>
        /// <param name="observaciones">Observaciones actuales del gasto.</param>
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
            Loaded += GestiónEgresos_Loaded;
        }

        // ════════════════════════════════════════════════════════════
        // TRANSICIÓN DE APERTURA / CIERRE
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Dispara la animación de entrada una sola vez, apenas la ventana termina de cargar
        /// (ya renderizada en su estado inicial invisible definido en el XAML).
        /// </summary>
        private void GestiónEgresos_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= GestiónEgresos_Loaded; // Solo debe ocurrir una vez.
            AnimarEntrada();
        }

        /// <summary>
        /// Anima la opacidad del <see cref="Window"/> (0→1) y la escala del Border raíz
        /// <c>rootScale</c> (0.94→1, vía <c>scaleEntrada</c>), con <see cref="QuadraticEase"/>
        /// para un movimiento natural y fluido. El Window solo anima Opacity: WPF no soporta
        /// de forma confiable un RenderTransform de escala aplicado al Window mismo.
        /// </summary>
        private void AnimarEntrada()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var fadeIn = new DoubleAnimation(0, 1, DuracionAnimacion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(0.94, 1, DuracionAnimacion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(0.94, 1, DuracionAnimacion) { EasingFunction = easing };

            BeginAnimation(OpacityProperty, fadeIn);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        /// <summary>
        /// Reproduce la animación inversa a la de entrada y, al completarse, cierra la ventana.
        /// Se usa para <b>toda</b> salida de la ventana (Cancelar, Guardar exitoso), así la
        /// transición es consistente sin importar el camino de cierre.
        /// </summary>
        private void AnimarSalidaYCerrar()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            var fadeOut = new DoubleAnimation(1, 0, DuracionAnimacion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };

            fadeOut.Completed += (_, _) => Close();

            BeginAnimation(OpacityProperty, fadeOut);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
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
            txtBadge.Foreground = BrushAcentoNuevo;
            badgeModo.Background = BrushFondoNuevo;
            btnGuardar.Content = "Guardar Gasto";
        }

        private void txtNombreGasto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Intencionalmente vacío: sin restricciones de entrada para este campo.
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio.Text = ValidadorContabilidad.FormatearPrecioGasto(txtPrecio.Text);

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = ValidadorContabilidad.LimpiarPrecioGasto(txtPrecio.Text);
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
            txtBadge.Foreground = BrushAcentoEditar;
            badgeModo.Background = BrushFondoEditar;
            btnGuardar.Content = "Guardar Cambios";

            SeleccionarTipoEnCombo(cmbTipoGasto_Edit, tipo);

            txtNombre_Edit.Text = nombre;
            txtPrecio_Edit.Text = "L " + precio.ToString("N2");
            txtFecha_Edit.Text = fecha.ToString("dd/MM/yyyy hh:mm tt", CulturaFecha);
            txtObservaciones_Edit.Text = observaciones ?? "";

            VerificarBloqueoEdicion();
        }

        private static void SeleccionarTipoEnCombo(ComboBox combo, string tipo)
        {
            foreach (var obj in combo.Items)
            {
                if (obj is ComboBoxItem item && item.Content?.ToString() == tipo)
                {
                    combo.SelectedItem = item;
                    return;
                }
            }
        }

        private void VerificarBloqueoEdicion()
        {
            if ((DateTime.Now - _fechaRegistro).TotalDays < 1)
                return;

            cmbTipoGasto_Edit.IsEnabled = false;
            txtNombre_Edit.IsEnabled = false;
            txtPrecio_Edit.IsEnabled = false;
            txtObservaciones_Edit.IsEnabled = false;
            btnGuardar.IsEnabled = false;
            borderBloqueado.Visibility = Visibility.Visible;
        }

        private void txtPrecio_Edit_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio_Edit.Text = ValidadorContabilidad.FormatearPrecioGasto(txtPrecio_Edit.Text);

        private void txtPrecio_Edit_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio_Edit.Text = ValidadorContabilidad.LimpiarPrecioGasto(txtPrecio_Edit.Text);
            txtPrecio_Edit.CaretIndex = txtPrecio_Edit.Text.Length;
        }

        // ════════════════════════════════════════════════════════════
        // GUARDAR
        // ════════════════════════════════════════════════════════════

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
            if (!ValidadorContabilidad.ValidarMontoPago(txtPrecio.Text, out decimal precio))
            {
                MostrarMensajeAgregar("⚠ Ingresa un precio válido.");
                return;
            }

            try
            {
                _db.AgregarGasto(tipo, nombre, observaciones, precio);

                _db.RegistrarBitacora(SesionActual.Email, "Egresos", "Agregar",
                    $"Gasto {nombre} - L {precio:N2}");

                MessageBox.Show("✅ ¡Gasto registrado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _menuRef.CargarEgreso();
                AnimarSalidaYCerrar();
            }
            catch (Exception ex)
            {
                MostrarMensajeAgregar("⚠ " + ex.Message);
            }
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
            if (!ValidadorContabilidad.ValidarMontoPago(txtPrecio_Edit.Text, out decimal precio))
            {
                MostrarMensajeEditar("⚠ Ingresa un precio válido.");
                return;
            }

            try
            {
                _db.ActualizarGasto(_gastoId, tipo, nombre, observaciones, precio, _fechaRegistro);

                _db.RegistrarBitacora(SesionActual.Email, "Egresos", "Actualizar",
                    $"Gasto #{_gastoId} - {nombre}, L {precio:N2}");

                MessageBox.Show("✅ ¡Gasto actualizado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _menuRef.CargarEgreso();
                AnimarSalidaYCerrar();
            }
            catch (Exception ex)
            {
                MostrarMensajeEditar("⚠ Error al actualizar: " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
            => AnimarSalidaYCerrar();

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_esEdicion)
                GuardarEdicion();
            else
                GuardarNuevo();
        }

        // ════════════════════════════════════════════════════════════
        // MENSAJES DE VALIDACIÓN
        // ════════════════════════════════════════════════════════════

        private void MostrarMensajeAgregar(string mensaje)
        {
            txtMensaje.Text = mensaje;
            borderMensajeAgregar.Visibility = Visibility.Visible;
        }

        private void OcultarMensajeAgregar()
            => borderMensajeAgregar.Visibility = Visibility.Collapsed;

        private void MostrarMensajeEditar(string mensaje)
        {
            txtMensajeEditar.Text = mensaje;
            borderMensajeEditar.Visibility = Visibility.Visible;
        }

        private void OcultarMensajeEditar()
            => borderMensajeEditar.Visibility = Visibility.Collapsed;
    }
}