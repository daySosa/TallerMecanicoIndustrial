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
    public partial class ActualizarPago : Window
    {
        private MenuDePagos _menuRef;
        private int _pagoId;
        clsConsultasBD db = new clsConsultasBD();

        public ActualizarPago(MenuDePagos menuRef, int pagoId, string dni, int ordenId, decimal monto, DateTime fecha)
        {
            InitializeComponent();
            _menuRef = menuRef;
            _pagoId = pagoId;

            txtDNI.Text = dni;
            txtOrdenID.Text = ordenId.ToString();
            txtPrecio.Text = "L " + monto.ToString("N2");
            txtFecha.Text = fecha.ToString("dd/MM/yyyy hh:mm tt",
                              new System.Globalization.CultureInfo("es-ES"));

            BuscarNombre(dni);

            txtOrdenID.TextChanged += txtOrdenID_TextChanged;
            txtDNI.TextChanged += txtDNI_TextChanged;
        }

        private void txtDNI_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuscarNombre(txtDNI.Text.Trim());
        }

        private void BuscarNombre(string dni)
        {
            if (string.IsNullOrEmpty(dni))
            {
                txtNombreCliente.Text = "";
                return;
            }

            try
            {
                var (nombres, apellidos) = db.BuscarNombreCliente(dni);

                if (nombres != null)
                {
                    txtNombreCliente.Text = nombres + " " + apellidos;
                    txtNombreCliente.Foreground = System.Windows.Media.Brushes.White;
                    OcultarMensaje();
                }
                else
                {
                    txtNombreCliente.Text = "";
                    MostrarMensaje("No se encontró ningún cliente con ese DNI.");
                }
            }
            catch (Exception ex)
            {
                MostrarMensaje("Error: " + ex.Message);
            }
        }

        private void txtOrdenID_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!int.TryParse(txtOrdenID.Text.Trim(), out int ordenId))
            {
                txtPrecio.Text = "L 0.00";
                return;
            }

            try
            {
                decimal? total = db.ObtenerTotalOrden(ordenId);
                txtPrecio.Text = total.HasValue
                    ? "L " + total.Value.ToString("N2")
                    : "L 0.00";
            }
            catch
            {
                txtPrecio.Text = "L 0.00";
            }
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
            OcultarMensaje();

            string dni = txtDNI.Text.Trim();
            string ordenStr = txtOrdenID.Text.Trim();
            string montoStr = txtPrecio.Text.Replace("L", "").Replace(" ", "").Trim();

            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ El DNI es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarSoloDigitos(dni, "⚠ El DNI solo debe contener números.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(txtNombreCliente.Text, "⚠ Ingresa un DNI válido.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(ordenStr, "⚠ El ID de la orden es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarEntero(ordenStr, out int ordenId, "⚠ El ID de la orden debe ser un número entero.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarTextoRequerido(montoStr, "⚠ El monto es obligatorio.", MostrarMensaje)) return;
            if (!clsValidaciones.ValidarPrecio(montoStr, out decimal monto, MostrarMensaje)) return;

            try
            {
                db.ActualizarPago(_pagoId, dni, ordenId, monto);

                MessageBox.Show("¡Pago actualizado correctamente!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _menuRef.CargarPago();
                this.Close();
            }
            catch (Exception ex)
            {
                MostrarMensaje("⚠ " + ex.Message);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void MostrarMensaje(string msg)
        {
            txtMensajeDNI.Text = msg;
            txtMensajeDNI.Visibility = Visibility.Visible;
        }

        private void OcultarMensaje()
        {
            txtMensajeDNI.Visibility = Visibility.Collapsed;
        }
    }
}