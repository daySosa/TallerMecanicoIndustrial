using System.Text.RegularExpressions;
using Drawing = System.Drawing;

namespace Login.Clases
{
    public static class clsValidacionReconocimiento
    {
        // El umbral se mantiene en 115.0 para permitir que el análisis de 5 segundos
        // sea estable incluso si la distancia detectada ronda los 90-100.
        public const double UmbralReconocimiento = 115.0;

        public static (bool Ok, string Mensaje) ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return (false, "⚠ Ingresa el nombre de la persona.");

            nombre = nombre.Trim();

            if (nombre.Length < 2)
                return (false, "⚠ El nombre debe tener al menos 2 caracteres.");

            if (nombre.Length > 100)
                return (false, "⚠ El nombre no puede superar los 100 caracteres.");

            if (!Regex.IsMatch(nombre, @"^[\p{L}]+( [\p{L}]+)*$"))
                return (false, "⚠ El nombre solo puede contener letras y espacios simples entre palabras.");

            return (true, string.Empty);
        }

        public static (bool Ok, string Mensaje) ValidarFotoCapturada(Drawing.Bitmap? foto)
        {
            if (foto == null)
                return (false, "⚠ Primero captura una foto del rostro.");

            return (true, string.Empty);
        }

        /// <summary>
        /// Comprueba si el rostro analizado coincide con uno registrado.
        /// Si devuelve True, el cronómetro de 5 segundos en la interfaz seguirá corriendo.
        /// </summary>
        public static bool EsReconocimientoValido(int label, double distance, int totalPersonas)
        {
            // 1. Validamos que el label sea un índice válido.
            if (label < 0 || label >= totalPersonas)
                return false;

            // 2. Comparamos contra el umbral.
            // Mientras la distancia sea menor o igual a 115, el análisis es "Exitoso".
            return distance <= UmbralReconocimiento;
        }

        public static (bool Ok, string Mensaje) ValidarCamaraDisponible(int totalCamaras)
        {
            if (totalCamaras == 0)
                return (false, "No se encontró ninguna cámara disponible.");

            return (true, string.Empty);
        }

        public static (bool Ok, string Mensaje) ValidarRostroDetectado(bool rostroDetectado)
        {
            if (!rostroDetectado)
                return (false, "No hay ningún rostro detectado frente a la cámara.");

            return (true, string.Empty);
        }

        public static (bool Ok, string Mensaje) ValidarModoRegistroActivo(bool esModoRegistro)
        {
            if (!esModoRegistro)
                return (false, "Activa el Modo Registro antes de capturar una foto.");

            return (true, string.Empty);
        }
    }
}