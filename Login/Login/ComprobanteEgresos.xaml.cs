using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Contabilidad
{
    /// <summary>
    /// Ventana encargada de mostrar un comprobante de gasto (egreso).
    /// Presenta la información básica del gasto: ID, nombre, precio, fecha, tipo y observaciones.
    /// La ventana nace invisible (Opacity="0" en el Window) y su Border raíz nace ligeramente
    /// reducido (ScaleTransform en el XAML) para una transición de apertura fluida sin parpadeos;
    /// se anima simétricamente al cerrar.
    /// </summary>
    public partial class ComprobanteEgresos : Window
    {
        // ===== Constantes de tipo de gasto =====
        private const string TipoRepuesto = "Gasto en Repuesto";

        // ===== Recursos cacheados (estáticos) =====
        // Se crean una sola vez para todo el ciclo de vida de la app, no por cada apertura de ventana.
        private static readonly SolidColorBrush BrushRepuesto = new(Color.FromRgb(59, 130, 246));
        private static readonly SolidColorBrush BrushAdicional = new(Color.FromRgb(245, 158, 11));
        private static readonly CultureInfo CulturaFecha = new("es-HN");

        // Duración compartida para las animaciones de apertura/cierre.
        private static readonly Duration DuracionAnimacion = new(TimeSpan.FromMilliseconds(220));

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ComprobanteEgresos"/> y carga los datos del gasto.
        /// La animación de entrada se dispara automáticamente cuando la ventana termina de cargar.
        /// </summary>
        /// <param name="id">Identificador del gasto.</param>
        /// <param name="tipo">Tipo de gasto ("Gasto en Repuesto" o "Gasto Adicional").</param>
        /// <param name="nombre">Nombre o descripción del gasto.</param>
        /// <param name="precio">Monto del gasto.</param>
        /// <param name="fecha">Fecha y hora del registro.</param>
        /// <param name="observaciones">Observaciones adicionales del gasto (puede ser nulo o vacío).</param>
        public ComprobanteEgresos(int id, string tipo, string nombre, decimal precio,
                                   DateTime fecha, string observaciones)
        {
            InitializeComponent();

            CargarDatos(id, tipo, nombre, precio, fecha, observaciones);

            // La ventana ya nace en Opacity="0" y su Border raíz en escala 0.94 (definido en el XAML),
            // así que aquí solo disparamos la animación hacia el estado final visible.
            Loaded += ComprobanteEgresos_Loaded;
        }

        /// <summary>
        /// Carga y muestra los datos del comprobante en la interfaz gráfica.
        /// Aplica formato de moneda local (Lempiras) y de fecha regional.
        /// </summary>
        private void CargarDatos(int id, string tipo, string nombre, decimal precio,
                                  DateTime fecha, string observaciones)
        {
            lblID.Text = $"#{id}";
            lblNombre.Text = string.IsNullOrWhiteSpace(nombre) ? "Sin nombre" : nombre;
            lblPrecio.Text = $"- L {precio:N2}";
            lblFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt", CulturaFecha);
            lblTipo.Text = string.IsNullOrWhiteSpace(tipo) ? "Sin tipo" : tipo;
            lblObservaciones.Text = string.IsNullOrWhiteSpace(observaciones)
                ? "Sin observaciones registradas."
                : observaciones;

            bool esRepuesto = tipo == TipoRepuesto;
            borderTipo.Background = esRepuesto ? BrushRepuesto : BrushAdicional;
            lblTipo.Foreground = Brushes.White;
        }

        /// <summary>
        /// Dispara la animación de entrada una sola vez, apenas la ventana termina de cargar
        /// (ya renderizada en su estado inicial invisible definido en el XAML).
        /// </summary>
        private void ComprobanteEgresos_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ComprobanteEgresos_Loaded; // Solo debe ocurrir una vez.
            AnimarEntrada();
        }

        /// <summary>
        /// Anima la opacidad del <see cref="Window"/> (0→1) y la escala del Border raíz
        /// <c>rootScale</c> (0.94→1, vía <c>scaleEntrada</c>), con <see cref="QuadraticEase"/>
        /// para un movimiento natural y fluido ("como mantequilla"). El Window solo anima
        /// Opacity: WPF no soporta de forma confiable un RenderTransform de escala aplicado
        /// al Window mismo.
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
        /// Cierra la ventana con una animación de salida (fade + scale) antes de destruirla,
        /// manteniendo consistencia visual con la apertura.
        /// </summary>
        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            AnimarSalidaYCerrar();
        }

        /// <summary>
        /// Reproduce la animación inversa a la de entrada y, al completarse, cierra la ventana.
        /// Evita cierres abruptos que rompen la sensación de fluidez.
        /// </summary>
        private void AnimarSalidaYCerrar()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            var fadeOut = new DoubleAnimation(1, 0, DuracionAnimacion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };

            // Solo se cierra la ventana cuando termina la animación de opacidad,
            // así apertura y cierre se ven simétricos y no hay "salto" final.
            fadeOut.Completed += (_, _) => Close();

            BeginAnimation(OpacityProperty, fadeOut);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }
    }
}