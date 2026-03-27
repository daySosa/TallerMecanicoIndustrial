using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Órdenes_de_Trabajo.Controls
{
    /// <summary>
    /// Control personalizado tipo DatePicker en español.
    /// Permite seleccionar fechas mediante un calendario interactivo,
    /// mostrando los meses y días en formato localizado.
    /// </summary>
    public partial class EspañolDatePicker : UserControl
    {
        /// <summary>
        /// Propiedad de dependencia que almacena la fecha seleccionada.
        /// Permite el enlace bidireccional (TwoWay Binding) con otros controles.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(EspañolDatePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Obtiene o establece la fecha seleccionada en el control.
        /// </summary>
        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Lista de nombres de los meses en español.
        /// </summary>
        private DateTime _mesActual = DateTime.Today;

        /// <summary>
        /// Lista de nombres de los meses en español.
        /// </summary>
        private static readonly string[] _meses = {
            "Enero","Febrero","Marzo","Abril","Mayo","Junio",
            "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"
        };

        /// <summary>
        /// Inicializa una nueva instancia del control <see cref="EspañolDatePicker"/>
        /// y carga el calendario inicial.
        /// </summary>
        public EspañolDatePicker()
        {
            InitializeComponent();
            ActualizarCalendario();
        }

        /// <summary>
        /// Muestra u oculta el calendario al hacer clic en el campo de fecha.
        /// </summary>
        private void TxtFecha_Click(object sender, MouseButtonEventArgs e)
        {
            popupCalendario.IsOpen = !popupCalendario.IsOpen;
        }

        /// <summary>
        /// Cambia al mes anterior en el calendario.
        /// </summary>
        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(-1);
            ActualizarCalendario();
        }

        /// <summary>
        /// Cambia al mes siguiente en el calendario.
        /// </summary>
        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(1);
            ActualizarCalendario();
        }

        /// <summary>
        /// Genera y actualiza la visualización del calendario,
        /// incluyendo días del mes actual y días de meses adyacentes.
        /// </summary>
        private void ActualizarCalendario()
        {
            txtMesAnio.Text = $"{_meses[_mesActual.Month - 1]} {_mesActual.Year}";

            var dias = new List<DiaItem>();

            var primerDia = new DateTime(_mesActual.Year, _mesActual.Month, 1);

            // Lunes=0, Martes=1, ... Sabado=5, Domingo=6
            int diaSemana = (int)primerDia.DayOfWeek;
            int offset = diaSemana == 0 ? 6 : diaSemana - 1;

            // Días del mes anterior
            for (int i = offset - 1; i >= 0; i--)
                dias.Add(new DiaItem(primerDia.AddDays(-i - 1), SelectedDate, true));

            // Días del mes actual
            int totalDias = DateTime.DaysInMonth(_mesActual.Year, _mesActual.Month);
            for (int i = 1; i <= totalDias; i++)
                dias.Add(new DiaItem(new DateTime(_mesActual.Year, _mesActual.Month, i), SelectedDate, false));

            // Completar hasta 42 celdas
            int restantes = 42 - dias.Count;
            var ultimoDia = new DateTime(_mesActual.Year, _mesActual.Month, totalDias);
            for (int i = 1; i <= restantes; i++)
                dias.Add(new DiaItem(ultimoDia.AddDays(i), SelectedDate, true));

            icDias.ItemsSource = dias;
        }

        /// <summary>
        /// Maneja la selección de un día en el calendario,
        /// actualizando la fecha seleccionada y cerrando el popup.
        /// </summary>
        private void DiaClic(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DateTime fecha)
            {
                SelectedDate = fecha;
                txtFecha.Text = fecha.ToString("dd/MM/yyyy");
                popupCalendario.IsOpen = false;
                ActualizarCalendario();
            }
        }
    }

    /// <summary>
    /// Representa un día dentro del calendario,
    /// incluyendo información de estado para su visualización.
    /// </summary>
    public class DiaItem
    {
        /// <summary>
        /// Número del día en formato texto.
        /// </summary>
        public string Dia { get; }

        /// <summary>
        /// Fecha completa asociada al día.
        /// </summary>
        public DateTime Fecha { get; }

        /// <summary>
        /// Indica si el día pertenece a otro mes distinto al actual.
        /// </summary>
        public bool EsOtroMes { get; }

        /// <summary>
        /// Indica si el día corresponde a la fecha actual (hoy).
        /// </summary>
        public bool EsHoy { get; }

        /// <summary>
        /// Indica si el día está seleccionado.
        /// </summary>
        public bool EsSeleccionado { get; }

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="DiaItem"/>.
        /// </summary>
        /// <param name="fecha">Fecha representada.</param>
        /// <param name="seleccionada">Fecha seleccionada actualmente.</param>
        /// <param name="esOtroMes">Indica si pertenece a otro mes.</param>
        public DiaItem(DateTime fecha, DateTime? seleccionada, bool esOtroMes)
        {
            Fecha = fecha;
            Dia = fecha.Day.ToString();
            EsOtroMes = esOtroMes;
            EsHoy = fecha.Date == DateTime.Today;
            EsSeleccionado = seleccionada.HasValue && fecha.Date == seleccionada.Value.Date;
        }
    }
}
