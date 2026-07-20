#nullable enable
using Login.Clases;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Contabilidad
{
    /// <summary>
    /// Ventana modal para registrar un nuevo pago o editar uno existente.
    /// El modo se determina por el constructor usado: 1 argumento = agregar,
    /// 6 argumentos = editar. Todas las consultas a la base de datos se
    /// ejecutan en segundo plano para no congelar la interfaz.
    /// </summary>
    public partial class GestiónIngresos : Window
    {
        #region Constantes y caché estática

        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(180));
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        private const string TituloExito = "Éxito";
        private const string TituloError = "Error";

        /// <summary>Texto por defecto mostrado cuando aún no hay un monto calculado.</summary>
        private const string MontoPorDefecto = "L 0.00";

        #endregion

        #region Regex generado en tiempo de compilación

        // GeneratedRegex evita el costo de compilar la expresión en tiempo de
        // ejecución (reflection); el código se genera en build, es más rápido
        // y elimina la advertencia SYSLIB1045.
        [GeneratedRegex(@"^\d+$")]
        private static partial Regex SoloDigitosRegex();

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private readonly MenúPrincipalIngresos _menuRef;
        private readonly CancellationTokenSource _cts = new();

        private readonly bool _esEdicion;
        private readonly int _pagoId;
        private DateTime _fechaRegistro;

        /// <summary>Debounce para no consultar el total de la orden en cada tecla.</summary>
        private readonly DispatcherTimer _debounceOrdenAgregar;
        private readonly DispatcherTimer _debounceOrdenEditar;

        private bool _cerrandoConAnimacion;
        private bool _ventanaCerrada;

        /// <summary>Evita doble envío mientras una búsqueda o guardado está en curso.</summary>
        private bool _operacionEnCurso;

        #endregion

        // ── Constructor AGREGAR (1 argumento) ────────────────────────
        public GestiónIngresos(MenúPrincipalIngresos menuRef)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _esEdicion = false;

            _debounceOrdenAgregar = CrearDebounce(async () => await ActualizarMontoAgregarAsync());
            _debounceOrdenEditar = CrearDebounce(async () => await ActualizarMontoEditarAsync());

            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
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

            _debounceOrdenAgregar = CrearDebounce(async () => await ActualizarMontoAgregarAsync());
            _debounceOrdenEditar = CrearDebounce(async () => await ActualizarMontoEditarAsync());

            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
            MostrarModoActualizar(dni, ordenId, monto, fecha);
        }

        #region Ciclo de vida y transición de entrada/salida

        /// <summary>Aplica un fade-in suave al mostrar la ventana (entra con Opacity="0" desde XAML).</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Intercepta el cierre para reproducir un fade-out antes de cerrar de verdad,
        /// igual que en las ventanas principales, para que abrir/cerrar este diálogo
        /// se sienta parte de la misma transición fluida.
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_cerrandoConAnimacion) return;

            e.Cancel = true;
            _cerrandoConAnimacion = true;

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        private void LiberarRecursos()
        {
            try
            {
                _debounceOrdenAgregar.Stop();
                _debounceOrdenEditar.Stop();
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            {
                // Liberación best-effort al cerrar la ventana; no debe interrumpir el cierre.
            }
        }

        // Sin dependencias del estado de la instancia: puede ser static (CA1822).
        private static DispatcherTimer CrearDebounce(Action accion)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                accion();
            };
            return timer;
        }

        #endregion

        #region MODO AGREGAR

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
            => e.Handled = !SoloDigitosRegex().IsMatch(e.Text);

        // Permite disparar la búsqueda con Enter, sin necesidad de tocar el mouse.
        private void txtDNI_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnBuscar_Click(sender, e);
        }

        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtNombre.Text = "";
            panelOrdenes.Visibility = Visibility.Collapsed;
            dgOrdenes.ItemsSource = null;
            txtOrdenID.Text = "";
            txtMonto.Text = "";
            OcultarMensajeAgregar();
        }

        private async void btnBuscar_Click(object sender, RoutedEventArgs e)
        {
            if (_operacionEnCurso) return;

            OcultarMensajeAgregar();
            string dni = txtDNI.Text.Trim();
            if (!ValidadorContabilidad.ValidarDNIBusqueda(dni)) return;

            await BuscarClienteAgregarAsync(dni);
        }

        /// <summary>Busca el cliente y, si existe, sus órdenes pendientes de pago, sin bloquear la UI.</summary>
        private async Task BuscarClienteAgregarAsync(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni)) return;

            SetOperacionEnCurso(true);
            try
            {
                var (nombres, apellidos) = await Task.Run(
                    () => _db.BuscarNombreCliente(dni), _cts.Token);

                if (_ventanaCerrada) return;

                if (nombres != null)
                {
                    txtNombre.Text = $"{nombres} {apellidos}";
                    txtNombre.Foreground = Brushes.White;
                    await CargarOrdenesClienteAsync(dni);
                }
                else
                {
                    txtNombre.Text = "";
                    panelOrdenes.Visibility = Visibility.Collapsed;
                    MostrarMensajeAgregar("No se encontró ningún cliente con ese DNI.");
                }
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada) MostrarMensajeAgregar("Error al buscar cliente: " + ex.Message);
            }
            finally
            {
                SetOperacionEnCurso(false);
            }
        }

        private async Task CargarOrdenesClienteAsync(string dni)
        {
            try
            {
                DataTable dt = await Task.Run(
                    () => _db.ObtenerOrdenesFinalizadasSinPago(dni), _cts.Token);

                if (_ventanaCerrada) return;

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
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada) MostrarMensajeAgregar("Error al cargar órdenes: " + ex.Message);
            }
        }

        private void dgOrdenes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgOrdenes.SelectedItem is DataRowView row)
                txtOrdenID.Text = row["Orden_ID"].ToString();
        }

        // Reinicia el debounce en cada tecla; el total solo se consulta 300ms
        // después de que el usuario deja de escribir el ID de la orden.
        private void txtOrdenID_TextChanged(object sender, TextChangedEventArgs e)
        {
            OcultarMensajeAgregar();
            _debounceOrdenAgregar.Stop();
            _debounceOrdenAgregar.Start();
        }

        private async Task ActualizarMontoAgregarAsync()
        {
            if (!int.TryParse(txtOrdenID.Text.Trim(), out int ordenId))
            {
                txtMonto.Text = MontoPorDefecto;
                return;
            }
            try
            {
                decimal? total = await Task.Run(() => _db.ObtenerTotalOrden(ordenId), _cts.Token);
                if (_ventanaCerrada) return;
                txtMonto.Text = total.HasValue ? "L " + total.Value.ToString("N2") : MontoPorDefecto;
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch
            {
                if (!_ventanaCerrada) txtMonto.Text = MontoPorDefecto;
            }
        }

        #endregion

        #region MODO EDITAR

        private void MostrarModoActualizar(string dni, int ordenId, decimal monto, DateTime fecha)
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

            _ = BuscarNombreEditarAsync(dni);
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
            => _ = BuscarNombreEditarAsync(txtDNI_Edit.Text.Trim());

        private async Task BuscarNombreEditarAsync(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                txtNombreCliente.Text = "";
                return;
            }
            try
            {
                var (nombres, apellidos) = await Task.Run(
                    () => _db.BuscarNombreCliente(dni), _cts.Token);

                if (_ventanaCerrada) return;

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
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada) MostrarMensajeEditar("Error: " + ex.Message);
            }
        }

        private void txtOrdenID_Edit_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceOrdenEditar.Stop();
            _debounceOrdenEditar.Start();
        }

        private async Task ActualizarMontoEditarAsync()
        {
            if (!int.TryParse(txtOrdenID_Edit.Text.Trim(), out int ordenId))
            {
                txtPrecio.Text = MontoPorDefecto;
                return;
            }
            try
            {
                decimal? total = await Task.Run(() => _db.ObtenerTotalOrden(ordenId), _cts.Token);
                if (_ventanaCerrada) return;
                txtPrecio.Text = total.HasValue ? "L " + total.Value.ToString("N2") : MontoPorDefecto;
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch
            {
                if (!_ventanaCerrada) txtPrecio.Text = MontoPorDefecto;
            }
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
            => txtPrecio.Text = ValidadorContabilidad.FormatearPrecioGasto(txtPrecio.Text);

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = ValidadorContabilidad.LimpiarPrecioGasto(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        #endregion

        #region Guardar

        /// <summary>Valida y registra un pago nuevo en segundo plano, sin congelar la ventana.</summary>
        private async Task GuardarNuevoAsync()
        {
            OcultarMensajeAgregar();
            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();

            if (!ValidadorContabilidad.ValidarFormularioVacio(dni, ordenStr)) return;
            if (!ValidadorContabilidad.ValidarClienteBuscado(dni, txtNombre.Text)) return;
            if (!ValidacionesGenerales.ValidarTextoRequerido(txtNombre.Text,
                    "⚠ Busca un cliente válido antes de guardar.", MostrarMensajeAgregar)) return;
            if (!ValidadorContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;

            SetOperacionEnCurso(true);
            try
            {
                decimal? total = await Task.Run(() => _db.ObtenerTotalOrden(ordenId), _cts.Token);
                if (_ventanaCerrada) return;

                if (!total.HasValue)
                {
                    MostrarMensajeAgregar("⚠ No se encontró la orden especificada.");
                    return;
                }

                await Task.Run(() =>
                {
                    _db.RegistrarPago(dni, ordenId, total.Value);
                    _db.RegistrarBitacora(SesionActual.Email, "Ingresos", "Agregar",
                        $"Pago Orden #{ordenId} - Cliente {dni}, L {total.Value:N2}");
                }, _cts.Token);

                if (_ventanaCerrada) return;

                MessageBox.Show("✅ ¡Pago registrado correctamente!", TituloExito,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarPago();
                Close();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada) MostrarMensajeAgregar("⚠ " + ex.Message);
            }
            finally
            {
                SetOperacionEnCurso(false);
            }
        }

        /// <summary>Valida y aplica los cambios de un pago existente en segundo plano.</summary>
        private async Task GuardarEdicionAsync()
        {
            OcultarMensajeEditar();
            string dni = txtDNI_Edit.Text.Trim();
            string ordenStr = txtOrdenID_Edit.Text.Trim();

            if (!ValidadorContabilidad.ValidarFormularioVacio(dni, ordenStr, txtPrecio.Text)) return;
            if (!ValidadorContabilidad.ValidarDNIPago(dni, MostrarMensajeEditar)) return;
            if (!ValidacionesGenerales.ValidarTextoRequerido(txtNombreCliente.Text,
                    "⚠ Ingresa un DNI válido.", MostrarMensajeEditar)) return;
            if (!ValidadorContabilidad.ValidarOrdenId(ordenStr, out int ordenId)) return;
            if (!ValidadorContabilidad.ValidarMontoPago(txtPrecio.Text, out decimal monto)) return;

            SetOperacionEnCurso(true);
            try
            {
                await Task.Run(() =>
                {
                    _db.ActualizarPago(_pagoId, dni, ordenId, monto);
                    _db.RegistrarBitacora(SesionActual.Email, "Ingresos", "Actualizar",
                        $"Pago #{_pagoId} - Orden #{ordenId}, L {monto:N2}");
                }, _cts.Token);

                if (_ventanaCerrada) return;

                MessageBox.Show("✅ ¡Pago actualizado correctamente!", TituloExito,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                _menuRef.CargarPago();
                Close();
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada) MostrarMensajeEditar("⚠ Error al actualizar: " + ex.Message);
            }
            finally
            {
                SetOperacionEnCurso(false);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_operacionEnCurso) return;

            if (_esEdicion)
                await GuardarEdicionAsync();
            else
                await GuardarNuevoAsync();
        }

        #endregion

        #region Mensajes y estado de UI

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

        /// <summary>
        /// Bloquea/desbloquea los botones de acción mientras hay una operación de
        /// BD en curso, evitando doble clic (y por tanto pagos duplicados).
        /// </summary>
        private void SetOperacionEnCurso(bool enCurso)
        {
            _operacionEnCurso = enCurso;
            btnGuardar.IsEnabled = !enCurso;
            btnBuscar.IsEnabled = !enCurso;
            btnCancelar.IsEnabled = !enCurso;
            Cursor = enCurso ? Cursors.Wait : Cursors.Arrow;
        }

        #endregion

        #region Helpers

        /// <summary>Devuelve un pincel cacheado y congelado para el color hex indicado.</summary>
        private static SolidColorBrush Pincel(string hex)
        {
            if (_cachePinceles.TryGetValue(hex, out var existente))
                return existente;

            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            _cachePinceles[hex] = brush;
            return brush;
        }

        #endregion
    }
}