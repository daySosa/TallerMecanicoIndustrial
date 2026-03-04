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
    /// Lógica de interacción para EspañolDatePicker.xaml
    /// </summary>
    public partial class EspañolDatePicker : UserControl
    {
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register("SelectedDate", typeof(DateTime?), typeof(EspañolDatePicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        private DateTime _mesActual = DateTime.Today;

        private static readonly string[] _meses = {
            "Enero","Febrero","Marzo","Abril","Mayo","Junio",
            "Julio","Agosto","Septiembre","Octubre","Noviembre","Diciembre"
        };

        public EspañolDatePicker()
        {
            InitializeComponent();
            ActualizarCalendario();
        }

        private void TxtFecha_Click(object sender, MouseButtonEventArgs e)
        {
            popupCalendario.IsOpen = !popupCalendario.IsOpen;
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(-1);
            ActualizarCalendario();
        }

        private void BtnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(1);
            ActualizarCalendario();
        }

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

    public class DiaItem
    {
        public string Dia { get; }
        public DateTime Fecha { get; }
        public bool EsOtroMes { get; }
        public bool EsHoy { get; }
        public bool EsSeleccionado { get; }

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
