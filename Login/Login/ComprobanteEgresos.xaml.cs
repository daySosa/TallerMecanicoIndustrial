using System.Windows;
using System.Windows.Media;

namespace Contabilidad
{
    /// <summary>
    /// Ventana encargada de mostrar un comprobante de gasto (egreso).
    /// Presenta la información básica como ID, nombre, precio, fecha, tipo y observaciones.
    /// </summary>
    public partial class ComprobanteEgresos : Window
    {
        // Pinceles cacheados como estáticos para no crear instancias nuevas en cada apertura de ventana.
        private static readonly SolidColorBrush BrushRepuesto = new(Color.FromRgb(59, 130, 246));
        private static readonly SolidColorBrush BrushAdicional = new(Color.FromRgb(245, 158, 11));

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ComprobanteEgresos"/> y carga los datos del gasto.
        /// </summary>
        /// <param name="id">Identificador del gasto.</param>
        /// <param name="tipo">Tipo de gasto ("Gasto en Repuesto" o "Gasto Adicional").</param>
        /// <param name="nombre">Nombre o descripción del gasto.</param>
        /// <param name="precio">Monto del gasto.</param>
        /// <param name="fecha">Fecha y hora del registro.</param>
        /// <param name="observaciones">Observaciones adicionales del gasto.</param>
        public ComprobanteEgresos(int id, string tipo, string nombre, decimal precio,
                                   DateTime fecha, string observaciones)
        {
            InitializeComponent();
            CargarDatos(id, tipo, nombre, precio, fecha, observaciones);
        }

        /// <summary>
        /// Carga y muestra los datos del comprobante en la interfaz gráfica.
        /// </summary>
        private void CargarDatos(int id, string tipo, string nombre, decimal precio,
                                  DateTime fecha, string observaciones)
        {
            lblID.Text = "#" + id;
            lblNombre.Text = nombre;
            lblPrecio.Text = "- L " + precio.ToString("N2");
            lblFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                                            new System.Globalization.CultureInfo("es-ES"));
            lblTipo.Text = tipo;
            lblObservaciones.Text = string.IsNullOrWhiteSpace(observaciones)
                ? "Sin observaciones registradas."
                : observaciones;

            borderTipo.Background = tipo switch
            {
                "Gasto en Repuesto" => BrushRepuesto,
                _ => BrushAdicional
            };
            lblTipo.Foreground = Brushes.White;
        }

        /// <summary>
        /// Cierra la ventana del comprobante.
        /// </summary>
        private void btnCerrar_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}