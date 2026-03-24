using System;

public class clsValidaciones
{
	public clsValidaciones()
	{
        //Validaciones de Actualizar gasto
            public static bool ValidarComboSeleccionado(object selectedItem, string nombreCampo)
        {
            if (selectedItem == null)
            {
                MessageBox.Show($"⚠ Selecciona un {nombreCampo}.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un texto no esté vacío
        public static bool ValidarTextoRequerido(string texto, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show($"⚠ Escribe el {nombreCampo}.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un precio sea decimal válido y mayor a 0
        public static bool ValidarPrecio(string texto, out decimal precio)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (!decimal.TryParse(limpio, out precio) || precio <= 0)
            {
                MessageBox.Show("⚠ Ingresa un precio válido mayor a 0.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que una fecha tenga el formato dd/MM/yyyy HH:mm
        public static bool ValidarFecha(string texto, out DateTime fecha)
        {
            if (!DateTime.TryParseExact(texto.Trim(), "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out fecha))
            {
                MessageBox.Show("⚠ Formato de fecha inválido. Usa dd/MM/yyyy HH:mm\nEjemplo: 13/03/2026 14:30",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Formatea un precio con prefijo "L " al perder el foco
        public static string FormatearPrecio(string texto)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (decimal.TryParse(limpio, out decimal valor) && valor > 0)
                return "L " + valor.ToString("N2");
            return "";
        }

        // ✅ Limpia el prefijo "L " al recibir el foco
        public static string LimpiarPrefijoPrecio(string texto)
        {
            return texto.Replace("L", "").Replace(" ", "").Trim();
        }

        //Validaciones Actualizar Pago

        // ✅ Valida que un texto no esté vacío (mostrando mensaje en pantalla)
        public static bool ValidarTextoRequerido(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // ✅ Valida que un texto solo contenga dígitos
        public static bool ValidarSoloDigitos(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (!texto.All(char.IsDigit))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // ✅ Valida que un texto sea un entero válido
        public static bool ValidarEntero(string texto, out int resultado, string mensaje, Action<string> mostrarMensaje)
        {
            if (!int.TryParse(texto, out resultado))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // ✅ Valida que un precio sea decimal válido y mayor a 0 (mostrando mensaje en pantalla)
        public static bool ValidarPrecio(string texto, out decimal precio, Action<string> mostrarMensaje)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (!decimal.TryParse(limpio, out precio) || precio <= 0)
            {
                mostrarMensaje("⚠ El monto debe ser un número mayor a 0.");
                return false;
            }
            return true;
        }

        //Validaciones Agregar repuesto
        // ✅ Valida que un texto sea un entero válido y mayor a 0 (con MessageBox)
        public static bool ValidarEnteroPositivo(string texto, out int resultado, string titulo)
        {
            if (!int.TryParse(texto.Trim(), out resultado) || resultado <= 0)
            {
                MessageBox.Show("⚠ La cantidad debe ser un número entero mayor a 0.",
                    titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que una cantidad no exceda el stock disponible
        public static bool ValidarStock(int cantidad, int stockDisponible)
        {
            if (cantidad > stockDisponible)
            {
                MessageBox.Show(
                    $"⚠ Stock insuficiente. Solo hay {stockDisponible} unidades disponibles.",
                    "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}
