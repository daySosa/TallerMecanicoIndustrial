using LiveCharts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dasboard_Prueba.ViewModels
{
    class DashboardViewModel
    {
        public ChartValues<double> BalanceValues { get; set; }
        public ChartValues<double> OrderValues { get; set; }
        public ChartValues<double> GastosValues { get; set; }

        public DashboardViewModel()
        {
            BalanceValues = new ChartValues<double> { 4, 7, 3, 8, 5, 9, 6 };
            OrderValues = new ChartValues<double> { 2, 5, 8, 4, 7, 3, 10 };
            GastosValues = new ChartValues<double> { 6, 3, 9, 5, 7, 4, 8 };
        }

    }
}
