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

    /// <summary>
    /// Ventana encargada de mostrar un comprobante de ingreso o gasto.
    /// Presenta la información básica como ID, nombre, precio, fecha y tipo de transacción.
    /// </summary>
    public partial class MostrarComprobante : Window
    {
        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="MostrarComprobante"/>.
        /// </summary>
        /// <param name="id">Identificador del comprobante.</param>
        /// <param name="tipo">Tipo de comprobante (por ejemplo: ingreso o gasto).</param>
        /// <param name="nombre">Nombre o descripción del comprobante.</param>
        /// <param name="precio">Monto asociado al comprobante.</param>
        /// <param name="fecha">Fecha y hora del comprobante.</param>
        /// <param name="observaciones">Observaciones adicionales del comprobante.</param>
        public MostrarComprobante(int id, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            InitializeComponent();
            CargarDatos(id, tipo, nombre, precio, fecha, observaciones);
        }

        /// <summary>
        /// Carga y muestra los datos del comprobante en la interfaz gráfica.
        /// </summary>
        /// <param name="id">Identificador del comprobante.</param>
        /// <param name="tipo">Tipo de comprobante.</param>
        /// <param name="nombre">Nombre o descripción.</param>
        /// <param name="precio">Monto del comprobante.</param>
        /// <param name="fecha">Fecha del comprobante.</param>
        /// <param name="observaciones">Observaciones adicionales (no se muestran actualmente).</param>
        private void CargarDatos(int id, string tipo, string nombre, decimal precio, DateTime fecha, string observaciones)
        {
            // Asigna el ID formateado
            lblID.Text = "#" + id.ToString();
            // Asigna el nombre o descripción
            lblNombre.Text = nombre;
            // Asigna el precio con formato monetario
            lblPrecio.Text = "- L " + precio.ToString("F2");
            // Asigna la fecha en formato día/mes/año hora:minutos
            lblFecha.Text = fecha.ToString("dd/MM/yyyy HH:mm");

            // Asigna el tipo de comprobante
            lblTipo.Text = tipo;
            // Cambia el estilo visual según el tipo de comprobante
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


        }

        /// <summary>
        /// Evento click del botón cerrar.
        /// Cierra la ventana actual del comprobante.
        /// </summary>
        /// <param name="sender">Origen del evento.</param>
        /// <param name="e">Información del evento de clic.</param>
        private void btnCerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

