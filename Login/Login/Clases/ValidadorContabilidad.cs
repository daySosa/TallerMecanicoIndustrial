using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Validaciones específicas del módulo de Contabilidad.
    /// Reglas de negocio que solo aplican a egresos, ingresos y pagos.
    /// </summary>
    class ValidadorContabilidad
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(params string[] campos)
        {
            return ValidacionesGenerales.ValidarFormularioVacio(campos);
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

            if (!ValidacionesGenerales.ValidarTextoRequerido(limpio, "nombre del gasto")) return false;
            if (!ValidacionesGenerales.ValidarNoEsSoloNumeros(limpio, "nombre del gasto")) return false;
            if (!ValidacionesGenerales.ValidarIniciaConLetra(limpio, "nombre del gasto")) return false;
            if (!ValidacionesGenerales.ValidarSinRepeticionExcesiva(limpio, "nombre del gasto")) return false;
            if (!ValidacionesGenerales.ValidarLongitudMaxima(limpio, 100, "nombre del gasto")) return false;

            return true;
        }

        /// <summary>
        /// Valida que las observaciones sean obligatorias, no tengan caracteres
        /// repetidos excesivamente y no superen los 300 caracteres.
        /// </summary>
        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;

            if (!ValidacionesGenerales.ValidarIniciaConLetra(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarNoEsSoloNumeros(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarLongitudMaxima(texto.Trim(), 300, "observaciones")) return false;

            return true;
        }

        public static bool ValidarPrecioGasto(string texto, out decimal precio)
        {
            return ValidacionesGenerales.ValidarPrecio(texto, out precio);
        }

        // ─────────────────────────────────────────────────────────────
        // ORDEN / PAGO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteBuscado(string dni, string nombreCliente)
        {
            return ValidacionesGenerales.ValidarClienteDNI(dni, nombreCliente);
        }

        public static bool ValidarDNIBusqueda(string dni)
        {
            return ValidacionesGenerales.ValidarFormatoDNI(dni);
        }

        public static bool ValidarDNIPago(string dni, Action<string> mostrarMensaje)
        {
            if (!ValidacionesGenerales.ValidarTextoRequerido(dni, "⚠ El DNI es obligatorio para registrar el pago.", mostrarMensaje)) return false;
            if (!ValidacionesGenerales.ValidarSoloDigitos(dni, "⚠ El DNI solo debe contener números, sin guiones ni espacios.", mostrarMensaje)) return false;
            return true;
        }

        /// <summary>
        /// Valida que el ID de la orden sea un número entero positivo mayor a cero.
        /// </summary>
        public static bool ValidarOrdenId(string texto, out int ordenId)
        {
            ordenId = 0;
            if (!int.TryParse(texto.Trim(), out ordenId) || ordenId <= 0)
            {
                MessageBox.Show(
                    "⚠ No hay ninguna orden seleccionada.\n\n" +
                    "Elige una orden de la lista antes de registrar el pago.",
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
                MessageBox.Show(
                    "⚠ Ingresa el monto que el cliente está pagando; este campo es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out monto) || monto <= 0)
            {
                MessageBox.Show(
                    $"⚠ \"{texto.Trim()}\" no es un monto válido; debe ser un número mayor a 0.",
                    "Monto inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (monto > 999_999.99m)
            {
                MessageBox.Show(
                    $"⚠ El monto ingresado (L {monto:N2}) supera el límite permitido de L 999,999.99.",
                    "Monto inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static string FormatearPrecioGasto(string texto)
        {
            return ValidacionesGenerales.FormatearPrecio(texto);
        }

        public static string LimpiarPrecioGasto(string texto)
        {
            return ValidacionesGenerales.LimpiarPrefijoPrecio(texto);
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS CONTABLES
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFechaGasto(string texto, out DateTime fecha)
        {
            return ValidacionesGenerales.ValidarFecha(texto, out fecha);
        }

        // ─────────────────────────────────────────────────────────────
        // CATEGORÍA / TIPO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que se haya seleccionado una categoría de gasto o tipo de movimiento.
        /// </summary>
        public static bool ValidarCategoriaSeleccionada(object selectedItem)
        {
            return ValidacionesGenerales.ValidarComboSeleccionado(selectedItem, "tipo de gasto");
        }

    }
}