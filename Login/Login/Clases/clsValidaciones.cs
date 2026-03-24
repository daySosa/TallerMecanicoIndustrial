using System;
using System.Linq;
using System.Windows;
using System.Text.RegularExpressions;

namespace Login.Clases
{
    public class clsValidaciones
    {
        public clsValidaciones() { }

        // ── Validaciones de Actualizar Gasto ────────────────────────────────

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

        public static string FormatearPrecio(string texto)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (decimal.TryParse(limpio, out decimal valor) && valor > 0)
                return "L " + valor.ToString("N2");
            return "";
        }

        public static string LimpiarPrefijoPrecio(string texto)
        {
            return texto.Replace("L", "").Replace(" ", "").Trim();
        }

        // ── Validaciones Actualizar Pago ─────────────────────────────────────

        // Sobrecarga de ValidarTextoRequerido con Action<string>
        public static bool ValidarTextoRequerido(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        public static bool ValidarSoloDigitos(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (!texto.All(char.IsDigit))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        public static bool ValidarEntero(string texto, out int resultado, string mensaje, Action<string> mostrarMensaje)
        {
            if (!int.TryParse(texto, out resultado))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // Sobrecarga de ValidarPrecio con Action<string>
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

        // ── Validaciones Agregar Repuesto ────────────────────────────────────

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

        // ✅ Valida que el DNI no esté vacío y tenga exactamente 13 dígitos
        public static bool ValidarDNIHondureño(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni) || !Regex.IsMatch(dni, @"^\d{13}$"))
            {
                MessageBox.Show("⚠ Ingrese un DNI válido de 13 dígitos.", "DNI inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un texto solo contenga letras y espacios
        public static bool ValidarSoloLetras(string texto, string nombreCampo)
        {
            if (!texto.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show($"⚠ El {nombreCampo} solo puede contener letras y espacios.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida formato de correo electrónico (opcional, solo si no está vacío)
        public static bool ValidarCorreo(string correo)
        {
            if (!string.IsNullOrWhiteSpace(correo) &&
                !Regex.IsMatch(correo.Trim(), @"^[^@]+@[^@]+\.[^@]+$"))
            {
                MessageBox.Show("⚠ El correo no tiene un formato válido.\nEjemplo: nombre@dominio.com",
                    "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un teléfono tenga la longitud mínima requerida
        public static bool ValidarTelefono(string telefono, int longitudMinima)
        {
            if (string.IsNullOrWhiteSpace(telefono) || telefono.Length < longitudMinima)
            {
                MessageBox.Show("⚠ Ingrese un teléfono válido (ej: 9999-9999).", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}