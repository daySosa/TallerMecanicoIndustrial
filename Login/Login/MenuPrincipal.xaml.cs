using Dasboard_Prueba.ViewModels;
using LiveCharts;
using System.Globalization;
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
using Login;
using Órdenes_de_Trabajo;
using Vehículos;
using InterfazInventario;
using InterfazClientes;

namespace Dasboard_Prueba
{


    public partial class MenuPrincipal : Window
    {
        private DateTime _mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public ChartValues<double> IngresosSemanalValues { get; set; }
        public string[] IngresosSemanalLabels { get; set; }
        public MenuPrincipal()
        {
            IngresosSemanalValues = new ChartValues<double> { 45, 60, 35, 50, 40, 55 };
            IngresosSemanalLabels = new[] { "24 Jan", "25 Jan", "26 Jan", "27 Jan", "28 Jan", "29 Jan" };

            DataContext = this;

            InitializeComponent();
            GenerarCalendario();
        }

        private void btnAnterior_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(-1);
            GenerarCalendario();
        }

        private void btnSiguiente_Click(object sender, RoutedEventArgs e)
        {
            _mesActual = _mesActual.AddMonths(1);
            GenerarCalendario();
        }

        private void GenerarCalendario()
        {
            var cultura = new System.Globalization.CultureInfo("es-HN");
            string titulo = _mesActual.ToString("MMMM, yyyy", cultura);
            txtMesAnio.Text = char.ToUpper(titulo[0]) + titulo.Substring(1);

            gridDias.Children.Clear();

            string[] diasSemana = { "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom" };
            foreach (string dia in diasSemana)
            {
                gridDias.Children.Add(new TextBlock
                {
                    Text = dia,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B9BB4")),
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                });
            }

            int primerDia = (int)_mesActual.DayOfWeek;
            primerDia = primerDia == 0 ? 6 : primerDia - 1;

            int diasEnMes = DateTime.DaysInMonth(_mesActual.Year, _mesActual.Month);
            DateTime mesAnterior = _mesActual.AddMonths(-1);
            int diasMesAnterior = DateTime.DaysInMonth(mesAnterior.Year, mesAnterior.Month);

            for (int i = 0; i < 42; i++)
            {
                int dia;
                bool esDelMes = true;

                if (i < primerDia)
                {
                    dia = diasMesAnterior - primerDia + 1 + i;
                    esDelMes = false;
                }
                else if (i >= primerDia + diasEnMes)
                {
                    dia = i - primerDia - diasEnMes + 1;
                    esDelMes = false;
                }
                else
                {
                    dia = i - primerDia + 1;
                }

                bool esHoy = esDelMes &&
                             dia == DateTime.Today.Day &&
                             _mesActual.Month == DateTime.Today.Month &&
                             _mesActual.Year == DateTime.Today.Year;

                Border celda = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = esHoy
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4757"))
                        : Brushes.Transparent,
                    Margin = new Thickness(2)
                };

                TextBlock txt = new TextBlock
                {
                    Text = dia.ToString(),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = esHoy ? Brushes.White
                                : esDelMes ? Brushes.White
                                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4A5568"))
                };

                celda.Child = txt;
                gridDias.Children.Add(celda);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MenúPrincipalOrdenes Ordenes = new MenúPrincipalOrdenes();
            Ordenes.Show();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MenúPrincipalVehículos vehi = new MenúPrincipalVehículos();
            vehi.Show();
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MenúPrincipalInventario invent = new MenúPrincipalInventario();
            invent.Show();
            this.Close();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MenúPrincipalClientes cli = new MenúPrincipalClientes();
            cli.Show();
            this.Close();
        }
    }
}


