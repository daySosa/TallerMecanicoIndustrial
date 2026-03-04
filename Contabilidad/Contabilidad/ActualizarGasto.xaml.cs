using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Data.SqlClient;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Contabilidad
{
    /// <summary>
    /// Lógica de interacción para ActualizarGasto.xaml
    /// </summary>
    public partial class ActualizarGasto : Window
    {

        private string connectionString = @"Data Source=(localdb)\papu;Initial Catalog=Taller_Mecanico_Sistema;Integrated Security=True;";

        public ActualizarGasto(int gastoId, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
