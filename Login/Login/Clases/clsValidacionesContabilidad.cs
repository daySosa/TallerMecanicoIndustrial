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
        // GASTO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el nombre del gasto no supere los 100 caracteres,
        /// inicie con letra, no sea solo números y no tenga caracteres especiales.
        /// </summary>
        public static bool ValidarNombreGasto(string texto)
        {
            string limpio = texto.Trim();

            if (string.IsNullOrWhiteSpace(limpio))
            {
                MessageBox.Show("⚠ Escribe el nombre del gasto.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.ValidarIniciaConLetra(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(limpio, "nombre del gasto")) return false;
            if (!clsValidaciones.ValidarTextoConCaracteresPermitidos(limpio, "nombre del gasto")) return false;
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
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("⚠ Escribe las observaciones del gasto.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(texto, 300, "observaciones")) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // ORDEN / PAGO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el ID de la orden sea un número entero positivo mayor a cero.
        /// </summary>
        /// 

        public static bool ValidarDNIBusqueda(string dni)
        {
            return clsValidaciones.ValidarFormatoDNI(dni);
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
        public static bool ValidarMontoPagoContraOrden(decimal montoPago, decimal totalOrden)
        {
            if (montoPago > totalOrden)
            {
                MessageBox.Show(
                    $"⚠ El monto del pago (L {montoPago:N2}) no puede superar el total de la orden (L {totalOrden:N2}).",
                    "Monto excedido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS CONTABLES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que la fecha del gasto no sea futura ni anterior a 5 años.
        /// </summary>
        public static bool ValidarFechaGasto(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                MessageBox.Show("⚠ Selecciona una fecha para el gasto.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fecha.Value.Date > DateTime.Today)
            {
                MessageBox.Show("⚠ La fecha del gasto no puede ser futura.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fecha.Value.Date < DateTime.Today.AddYears(-5))
            {
                MessageBox.Show("⚠ La fecha del gasto no puede ser anterior a 5 años.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida que el rango de fechas para filtrar reportes sea coherente.
        /// </summary>
        public static bool ValidarRangoFechasReporte(DateTime? fechaInicio, DateTime? fechaFin)
        {
            if (fechaInicio.HasValue && fechaFin.HasValue &&
                fechaFin.Value.Date < fechaInicio.Value.Date)
            {
                MessageBox.Show("⚠ La fecha de fin no puede ser anterior a la fecha de inicio.",
                    "Rango inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
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

        /// <summary>
        /// Valida que el número de comprobante o factura tenga formato alfanumérico
        /// válido y no supere los 50 caracteres.
        /// </summary>
        public static bool ValidarNumeroComprobante(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true; // es opcional

            string limpio = texto.Trim();

            if (!Regex.IsMatch(limpio, @"^[a-zA-Z0-9\-\/]+$"))
            {
                MessageBox.Show("⚠ El número de comprobante solo puede contener letras, números, guiones y barras.",
                    "Comprobante inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.ValidarLongitudMaxima(limpio, 50, "número de comprobante")) return false;

            return true;
        }
    }
}