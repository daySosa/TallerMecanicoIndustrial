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
using Login.Clases;

namespace Contabilidad
{
    /// <summary>
    /// Ventana encargada de registrar un nuevo gasto en el sistema.
    /// Incluye validaciones de entrada y almacenamiento en la base de datos.
    /// </summary>
    public partial class AgregarGasto : Window
    {
        /// <summary>
        /// Instancia de la clase encargada de realizar operaciones en la base de datos.
        /// </summary>
        clsConsultasBD db = new clsConsultasBD();

        /// <summary>
        /// Inicializa una nueva instancia de la ventana <see cref="AgregarGasto"/>.
        /// </summary>
        public AgregarGasto()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Cancela la operación y cierra la ventana sin guardar información.
        /// </summary>
        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        /// <summary>
        /// Formatea el valor del precio al perder el foco, aplicando formato de moneda.
        /// </summary>
        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.FormatearPrecio(txtPrecio.Text);
        }

        /// <summary>
        /// Limpia el formato del precio al obtener el foco, permitiendo la edición del valor numérico.
        /// </summary>
        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.LimpiarPrefijoPrecio(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        /// <summary>
        /// Valida los datos ingresados y registra un nuevo gasto en la base de datos.
        /// Si la operación es exitosa, muestra un mensaje de confirmación y cierra la ventana.
        /// </summary>
        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarComboSeleccionado(cmbTipoGasto.SelectedItem, "tipo de gasto")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreGasto.Text, "nombre del gasto")) return;
            if (!clsValidacionesContabilidad.ValidarLongitudNombreGasto(txtNombreGasto.Text)) return;
            if (!clsValidaciones.ValidarPrecio(txtPrecio.Text, out decimal precio)) return;
            // Observaciones son opcionales, solo validar longitud si tiene contenido
            if (!clsValidacionesContabilidad.ValidarObservaciones(txtObservaciones.Text)) return;

            try
            {
                db.AgregarGasto(
                    ((ComboBoxItem)cmbTipoGasto.SelectedItem).Content.ToString(),
                    txtNombreGasto.Text.Trim(),
                    txtObservaciones.Text.Trim(),
                    precio
                );

                MessageBox.Show("✅ Gasto guardado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}