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
    public partial class AgregarGasto : Window
    {
        clsConsultasBD db = new clsConsultasBD();

        public AgregarGasto()
        {
            InitializeComponent();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void txtPrecio_LostFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.FormatearPrecio(txtPrecio.Text);
        }

        private void txtPrecio_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPrecio.Text = clsValidaciones.LimpiarPrefijoPrecio(txtPrecio.Text);
            txtPrecio.CaretIndex = txtPrecio.Text.Length;
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (!clsValidaciones.ValidarComboSeleccionado(cmbTipoGasto.SelectedItem, "tipo de gasto")) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreGasto.Text, "nombre del gasto")) return;
            if (!clsValidaciones.ValidarPrecio(txtPrecio.Text, out decimal precio)) return;

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