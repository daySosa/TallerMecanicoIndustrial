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
using System.Windows.Shapes;

namespace Contabilidad
{
  
    public partial class MostrarComprobante : Window
    {
        public MostrarComprobante(int id, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            CargarDatos(id, tipo, nombre, precio, fecha, observaciones);
        }

        private void CargarDatos(int id, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            lblID.Text = "#" + id.ToString();
            lblNombre.Text = nombre;
            lblPrecio.Text = "- L " + precio.ToString("F2");
            lblFecha.Text = fecha.ToString("dd/MM/yyyy HH:mm");

            lblTipo.Text = tipo;
            if (tipo == "Gasto en Repuesto")
            {
                borderTipo.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(59, 130, 246)); 
                lblTipo.Foreground = System.Windows.Media.Brushes.White;
            }
            else
            {
                borderTipo.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(245, 158, 11)); 
                lblTipo.Foreground = System.Windows.Media.Brushes.White;
            }

            // Observaciones
            //lblObservaciones.Text = string.IsNullOrWhiteSpace(observaciones)
                //? "Sin observaciones registradas."
                //: observaciones;
        }

        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
