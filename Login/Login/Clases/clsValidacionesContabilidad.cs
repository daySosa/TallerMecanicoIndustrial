using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Validaciones específicas del módulo de Contabilidad.
    /// Reglas de negocio que solo aplican a egresos, ingresos y pagos.
    /// </summary>
    class clsValidacionesContabilidad
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(params string[] campos)
        {
            return clsValidaciones.ValidarFormularioVacio(campos);
        }

        // ─────────────────────────────────────────────────────────────
        // GASTO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el nombre del gasto no supere los 100 caracteres,
        /// inicie con letra, no sea solo números y no tenga caracteres especiales.
        /// </summary>
        public static bool ValidarNombreGasto(string texto)
        {
            string limpio = texto.Trim();

            if (!clsValidaciones.ValidarTextoRequerido(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarIniciaConLetra(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(limpio, 100, "nombre del gasto")) return false;

            return true;
        }

        /// <summary>
        /// Valida que las observaciones sean obligatorias, no tengan caracteres
        /// repetidos excesivamente y no superen los 300 caracteres.
        /// </summary>
        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;

            if (!clsValidaciones.ValidarIniciaConLetra(texto.Trim(), "observaciones")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(texto.Trim(), "observaciones")) return false; 
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(texto.Trim(), 300, "observaciones")) return false;

            return true;
        }

        public static bool ValidarPrecioGasto(string texto, out decimal precio)
        {
            return clsValidaciones.ValidarPrecio(texto, out precio);
        }

        // ─────────────────────────────────────────────────────────────
        // ORDEN / PAGO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el ID de la orden sea un número entero positivo mayor a cero.
        /// </summary>
        /// 

        public static bool ValidarClienteBuscado(string dni, string nombreCliente)
        {
            return clsValidaciones.ValidarClienteDNI(dni, nombreCliente);
        }

        public static bool ValidarDNIBusqueda(string dni)
        {
            return clsValidaciones.ValidarFormatoDNI(dni);
        }

        public static bool ValidarDNIPago(string dni, Action<string> mostrarMensaje)
        {
            if (!clsValidaciones.ValidarTextoRequerido(dni, "⚠ El DNI es obligatorio.", mostrarMensaje)) return false;
            if (!clsValidaciones.ValidarSoloDigitos(dni, "⚠ El DNI solo debe contener números.", mostrarMensaje)) return false;
            return true;
        }

        public static bool ValidarOrdenId(string texto, out int ordenId)
        {
            ordenId = 0;
            if (!int.TryParse(texto.Trim(), out ordenId) || ordenId <= 0)
            {
                MessageBox.Show("⚠ Selecciona una orden de la lista antes de guardar.",
                    "Orden no seleccionada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que el monto del pago sea un número mayor a cero y no exceda
        /// el límite razonable para un pago de taller (L 999,999.99).
        /// </summary>
        public static bool ValidarMontoPago(string texto, out decimal monto)
        {
            monto = 0;
            string limpio = texto.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(limpio))
            {
                MessageBox.Show("⚠ El monto del pago no puede estar vacío.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out monto) || monto <= 0)
            {
                MessageBox.Show("⚠ El monto debe ser un número mayor a 0.",
                    "Monto inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (monto > 999_999.99m)
            {
                MessageBox.Show("⚠ El monto ingresado supera el límite permitido (L 999,999.99).",
                    "Monto inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida que el monto del pago no supere el total de la orden,
        /// previniendo pagos parciales o en exceso no autorizados.
        /// </summary>

        public static string FormatearPrecioGasto(string texto)
        {
            return clsValidaciones.FormatearPrecio(texto);
        }

        public static string LimpiarPrecioGasto(string texto)
        {
            return clsValidaciones.LimpiarPrefijoPrecio(texto);
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS CONTABLES
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFechaGasto(string texto, out DateTime fecha)
        {
            return clsValidaciones.ValidarFecha(texto, out fecha);
        }

        // ─────────────────────────────────────────────────────────────
        // CATEGORÍA / TIPO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que se haya seleccionado una categoría de gasto o tipo de movimiento.
        /// </summary>
        public static bool ValidarCategoriaSeleccionada(object selectedItem)
        {
            return clsValidaciones.ValidarComboSeleccionado(selectedItem, "tipo de gasto");
        }

    }
}