#nullable enable
using Login.Clases;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace InterfazClientes
{
    /// <summary>
    /// Ventana modal para registrar o editar un cliente. Valida los campos,
    /// verifica duplicados de DNI/teléfono contra la base de datos y
    /// devuelve el cliente creado en <see cref="ClienteResultado"/>.
    /// </summary>
    public partial class ClientesWindow : Window
    {
        #region Constantes y caché estática

        /// <summary>Duración de la transición de entrada/salida de la ventana.</summary>
        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(200));

        /// <summary>Caché de pinceles ya congelados, para no crear un SolidColorBrush nuevo en cada cambio de estado.</summary>
        private static readonly Dictionary<string, SolidColorBrush> _cachePinceles = new();

        private const string ColorActivo = "#4CAF50";
        private const string ColorInactivo = "#E74C3C";

        #endregion

        #region Estado interno

        private RepositorioSql? _db;
        private readonly CancellationTokenSource _cts = new();

        private string _dniEditando = string.Empty;
        private volatile bool _ventanaCerrada;

        /// <summary>Evita el reingreso a Window_Closing mientras se reproduce el fade-out.</summary>
        private bool _cerrandoConAnimacion;

        /// <summary>Cliente recién creado, disponible tras un alta exitosa mediante <see cref="BtnAgregar_Click"/>.</summary>
        public clsCliente? ClienteResultado { get; private set; }

        #endregion

        public ClientesWindow()
        {
            InitializeComponent();

            btnActualizar.Visibility = Visibility.Collapsed;
            btnAgregar.Visibility = Visibility.Visible;
            btnAgregar.IsEnabled = false;
            btnActualizar.IsEnabled = false;
            btnActualizar.Opacity = 0.4;

            Loaded += ClientesWindow_Loaded;
            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida

        private async void ClientesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _db = await Task.Run(() => new RepositorioSql(), _cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _db = null;
                if (!_ventanaCerrada)
                    MessageBox.Show(
                        "No se pudo conectar con la base de datos. No podrás guardar cambios.\n\n" + ex.Message,
                        "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (_ventanaCerrada) return;

            bool listo = _db is not null;
            btnAgregar.IsEnabled = listo && btnAgregar.Visibility == Visibility.Visible;
            btnActualizar.IsEnabled = listo && btnActualizar.Visibility == Visibility.Visible;
            btnActualizar.Opacity = btnActualizar.IsEnabled ? 1 : 0.4;
        }

        /// <summary>Aplica un fade-in suave al mostrar la ventana (requiere Opacity="0" en el XAML).</summary>
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
        /// igual que en VentanaBiometria, para que todas las ventanas de la app se
        /// sientan consistentes al abrir y cerrar. La primera vez cancela el cierre;
        /// al completarse la animación se vuelve a llamar Close() con la bandera activada
        /// (el DialogResult ya asignado por BtnAgregar_Click/BtnActualizar_Click se conserva).
        /// </summary>
        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (_cerrandoConAnimacion) return;

            _ventanaCerrada = true;

            e.Cancel = true;
            _cerrandoConAnimacion = true;

            var fadeOut = new DoubleAnimation(1d, 0d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }

        /// <summary>Libera los recursos propios (token de cancelación, repositorio) al cerrar la ventana.</summary>
        private void LiberarRecursos()
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            { }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
            catch (InvalidOperationException)
            { }
        }

        #endregion

        #region Validaciones de entrada (formato de teclas)

        private void txtDPI_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");

        private void txtNombre_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");

        private void txtApellido_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ\s]+$");

        private void txtDireccion_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\-\.\,\(\)\/]+$");

        private void txtTelefono_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"^\d+$"))
            {
                e.Handled = true;
                return;
            }

            string soloNumeros = SoloDigitos(txtTelefono.Text);
            if (soloNumeros.Length >= 8)
                e.Handled = true;
        }

        private void txtTelefono_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            txtTelefono.TextChanged -= txtTelefono_TextChanged;

            string soloNumeros = SoloDigitos(txtTelefono.Text);
            if (soloNumeros.Length > 8)
                soloNumeros = soloNumeros[..8];

            string formateado = FormatearTelefono(soloNumeros);

            int caretPos = txtTelefono.CaretIndex;
            txtTelefono.Text = formateado;

            int nuevoCaret = caretPos > 4
                ? Math.Min(caretPos + 1, formateado.Length)
                : Math.Min(caretPos, formateado.Length);

            txtTelefono.CaretIndex = nuevoCaret;
            txtTelefono.TextChanged += txtTelefono_TextChanged;
        }

        #endregion

        #region Carga para edición

        /// <summary>Rellena el formulario con los datos de un cliente existente y habilita el modo edición.</summary>
        public void CargarClienteParaEditar(clsCliente c)
        {
            _dniEditando = c.Cliente_DPI;

            txtDPI.Text = c.Cliente_DPI;
            txtNombre.Text = c.Cliente_Nombre;
            txtApellido.Text = c.Cliente_Apellido;
            txtTelefono.Text = FormatearTelefono(SoloDigitos(c.Cliente_Telefono));
            txtCorreo.Text = c.Cliente_Correo;
            txtDireccion.Text = c.Cliente_Direccion;
            toggleActivo.IsChecked = c.Cliente_Activo;

            btnAgregar.Visibility = Visibility.Collapsed;
            btnActualizar.Visibility = Visibility.Visible;

            bool dbListo = _db is not null;
            btnActualizar.IsEnabled = dbListo;
            btnActualizar.Opacity = dbListo ? 1 : 0.4;
        }

        #endregion

        #region Toggle estado

        private void ToggleActivo_Checked(object sender, RoutedEventArgs e)
            => ActualizarVisualEstado(activo: true);

        private void ToggleActivo_Unchecked(object sender, RoutedEventArgs e)
            => ActualizarVisualEstado(activo: false);

        private void ActualizarVisualEstado(bool activo)
        {
            if (txtEstadoLabel == null) return;

            string color = activo ? ColorActivo : ColorInactivo;
            var pincel = Pincel(color);

            txtEstadoLabel.Text = activo ? "El cliente está activo" : "El cliente está inactivo";
            txtEstadoLabel.Foreground = pincel;
            iconEstado.Foreground = pincel;
            iconEstado.Kind = activo
                ? MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline
                : MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;
        }

        #endregion

        #region Acciones

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => Close();

        /// <summary>Ejecuta todas las validaciones de formulario y de negocio sobre los campos actuales.</summary>
        private bool ValidarCampos(RepositorioSql db, string dni, string telefonoLimpio, string dniActual = "")
        {
            if (!ValidadorCliente.ValidarFormularioVacio(
                dni, txtNombre.Text, txtApellido.Text,
                telefonoLimpio, txtDireccion.Text)) return false;

            if (!ValidadorCliente.ValidarDNIHondureño(dni)) return false;
            if (!ValidadorCliente.ValidarLongitudNombre(txtNombre.Text, "nombre")) return false;
            if (!ValidadorCliente.ValidarLongitudNombre(txtApellido.Text, "apellido")) return false;
            if (!ValidacionesGenerales.Telefono(telefonoLimpio)) return false;
            if (!ValidadorCliente.ValidarLongitudCorreo(txtCorreo.Text)) return false;
            if (!ValidadorCliente.ValidarDireccion(txtDireccion.Text)) return false;
            if (!ValidadorCliente.ValidarDNINoDuplicado(dni, dniActual, db)) return false;
            if (!ValidadorCliente.ValidarTelefonoNoDuplicado(telefonoLimpio, dniActual, db)) return false;

            return true;
        }

        private void BtnAgregar_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null)
            {
                MessageBox.Show("La conexión con la base de datos aún no está lista. Intenta de nuevo en un momento.",
                    "Sin conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!SesionActual.HaySesionActiva)
            {
                MessageBox.Show("⚠ No hay una sesión activa. Vuelve a iniciar sesión antes de continuar.",
                    "Sesión no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnAgregar.IsEnabled = false;

            string dpi = txtDPI.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string telefonoLimpio = SoloDigitos(txtTelefono.Text);
            string correo = txtCorreo.Text.Trim();
            string direccion = txtDireccion.Text.Trim();

            if (!ValidarCampos(_db, dpi, telefonoLimpio))
            {
                btnAgregar.IsEnabled = true;
                return;
            }

            try
            {
                bool insertado = _db.AgregarCliente(dpi, nombre, apellido, telefonoLimpio, correo, direccion);

                if (!insertado)
                {
                    MessageBox.Show("Ya existe un cliente con ese DNI.",
                        "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    btnAgregar.IsEnabled = true;
                    return;
                }

                _db.RegistrarBitacora(SesionActual.Email, "Clientes", "Agregar",
                    $"Cliente {dpi} - {nombre} {apellido}");

                ClienteResultado = new clsCliente
                {
                    Cliente_DPI = dpi,
                    Cliente_Nombre = nombre,
                    Cliente_Apellido = apellido,
                    Cliente_Telefono = telefonoLimpio,
                    Cliente_Correo = correo,
                    Cliente_Direccion = direccion,
                    Cliente_Activo = true
                };

                MessageBox.Show("Cliente guardado correctamente.",
                    "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                btnAgregar.IsEnabled = true;
                MessageBox.Show("Error al agregar cliente:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnActualizar_Click(object sender, RoutedEventArgs e)
        {
            if (_db is null)
            {
                MessageBox.Show("La conexión con la base de datos aún no está lista. Intenta de nuevo en un momento.",
                    "Sin conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_dniEditando))
            {
                MessageBox.Show("No hay ningún cliente cargado para actualizar.",
                    "Sin selección", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!SesionActual.HaySesionActiva)
            {
                MessageBox.Show("No hay una sesión activa. Vuelve a iniciar sesión antes de continuar.",
                    "Sesión no válida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnActualizar.IsEnabled = false;

            string nuevoDni = txtDPI.Text.Trim();
            string nombre = txtNombre.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string telefonoLimpio = SoloDigitos(txtTelefono.Text);
            string correo = txtCorreo.Text.Trim();
            string direccion = txtDireccion.Text.Trim();

            if (!ValidarCampos(_db, nuevoDni, telefonoLimpio, dniActual: _dniEditando))
            {
                btnActualizar.IsEnabled = true;
                return;
            }

            try
            {
                _db.ActualizarCliente(
                    _dniEditando, nombre, apellido, telefonoLimpio, correo, direccion,
                    toggleActivo.IsChecked == true, nuevoDni);

                _db.RegistrarBitacora(SesionActual.Email, "Clientes", "Actualizar",
                    $"Cliente {nuevoDni} - {nombre} {apellido}");

                _dniEditando = nuevoDni;

                MessageBox.Show("Cliente actualizado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                btnActualizar.IsEnabled = true;
                MessageBox.Show("Error al actualizar:\n" + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        /// <summary>Extrae únicamente los dígitos de una cadena (usado para limpiar el teléfono).</summary>
        private static string SoloDigitos(string texto) => Regex.Replace(texto, @"\D", "");

        /// <summary>Formatea 8 dígitos como NNNN-NNNN; deja el texto tal cual si aún no llega a 4 dígitos.</summary>
        private static string FormatearTelefono(string soloDigitos) =>
            soloDigitos.Length <= 4 ? soloDigitos : $"{soloDigitos[..4]}-{soloDigitos[4..]}";

        /// <summary>
        /// Devuelve un <see cref="SolidColorBrush"/> congelado para el color hex indicado,
        /// reutilizándolo desde caché en vez de crear una instancia nueva en cada llamada.
        /// </summary>
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