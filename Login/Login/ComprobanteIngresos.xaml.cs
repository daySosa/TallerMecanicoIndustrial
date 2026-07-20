#nullable enable
using Login.Clases;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Animation;

namespace Contabilidad
{
    /// <summary>
    /// Ventana que muestra el comprobante de un pago ya registrado.
    /// La consulta a la base de datos se ejecuta en segundo plano para
    /// que la apertura de la ventana no congele la interfaz.
    /// </summary>
    public partial class ComprobantePago : Window
    {
        #region Constantes

        private static readonly Duration DuracionTransicion = new(TimeSpan.FromMilliseconds(180));
        private static readonly CultureInfo CulturaFecha = new("es-ES");
        private const string TituloAviso = "Aviso";
        private const string TituloError = "Error";

        #endregion

        #region Estado interno

        private readonly RepositorioSql _db = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly int _pagoId;

        private bool _cerrandoConAnimacion;
        private bool _ventanaCerrada;

        /// <summary>
        /// Indica si la ventana debe cerrarse en cuanto termine de aparecer
        /// (comprobante no encontrado o error al cargarlo). Se difiere el
        /// cierre para no interrumpir el fade-in con un cierre en seco.
        /// </summary>
        private bool _cerrarAlTerminarCarga;

        #endregion

        public ComprobantePago(int pagoId)
        {
            InitializeComponent();
            _pagoId = pagoId;

            Closed += (_, _) => { _ventanaCerrada = true; LiberarRecursos(); };
        }

        #region Ciclo de vida y transición de entrada/salida

        /// <summary>
        /// Aplica el fade-in y, una vez visible, dispara la carga asíncrona
        /// del comprobante (para que la ventana se sienta instantánea).
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeIn = new DoubleAnimation(0d, 1d, DuracionTransicion)
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);

            await CargarComprobanteAsync(_pagoId);

            if (_cerrarAlTerminarCarga && !_ventanaCerrada)
                Close();
        }

        /// <summary>
        /// Intercepta el cierre para reproducir un fade-out antes de cerrar de
        /// verdad, manteniendo la misma transición fluida del resto del módulo.
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
                _cts.Cancel();
                _cts.Dispose();
                (_db as IDisposable)?.Dispose();
            }
            catch
            {
                // Liberación best-effort al cerrar la ventana; no debe interrumpir el cierre.
            }
        }

        #endregion

        #region Carga de datos

        /// <summary>Consulta el comprobante en un hilo de fondo y llena la ventana con sus datos.</summary>
        private async Task CargarComprobanteAsync(int pagoId)
        {
            try
            {
                DataRow? fila = await Task.Run(() => _db.ObtenerComprobantePago(pagoId), _cts.Token);
                if (_ventanaCerrada) return;

                if (fila != null)
                    MostrarDatos(fila);
                else
                    ProgramarCierrePorAviso("No se encontró el comprobante.");
            }
            catch (OperationCanceledException)
            {
                // Cancelación esperada por cierre de ventana; no requiere acción.
            }
            catch (Exception ex)
            {
                if (!_ventanaCerrada)
                    ProgramarCierrePorError("Error al cargar comprobante: " + ex.Message);
            }
        }

        /// <summary>Vuelca los datos de la fila del pago en los controles de la ventana.</summary>
        private void MostrarDatos(DataRow fila)
        {
            lblPagoID.Text = "#" + fila["Pago_ID"];
            lblDNI.Text = fila["Cliente_DNI"].ToString();
            lblOrdenID.Text = "#" + fila["Orden_ID"];

            string nombres = fila["Cliente_Nombres"].ToString() ?? string.Empty;
            string apellidos = fila["Cliente_Apellidos"].ToString() ?? string.Empty;
            string inicial = apellidos.Length > 0 ? apellidos[0] + "." : string.Empty;
            lblNombre.Text = $"{nombres} {inicial}".TrimEnd();

            decimal monto = Convert.ToDecimal(fila["Precio_Pago"]);
            lblMonto.Text = "L " + monto.ToString("N2");

            DateTime fecha = Convert.ToDateTime(fila["Fecha_Pago"]);
            lblFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt", CulturaFecha);
        }

        /// <summary>Muestra un aviso y marca la ventana para cerrarse en cuanto el fade-in termine.</summary>
        private void ProgramarCierrePorAviso(string mensaje)
        {
            _cerrarAlTerminarCarga = true;
            MessageBox.Show(mensaje, TituloAviso, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>Muestra un error y marca la ventana para cerrarse en cuanto el fade-in termine.</summary>
        private void ProgramarCierrePorError(string mensaje)
        {
            _cerrarAlTerminarCarga = true;
            MessageBox.Show(mensaje, TituloError, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        private void btnCerrar_Click(object sender, RoutedEventArgs e) => Close();
    }
}