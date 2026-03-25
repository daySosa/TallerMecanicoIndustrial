using System.Text.RegularExpressions;
using Drawing = System.Drawing;

namespace Login.Clases
{
    public static class clsValidacionReconocimiento
    {
        public const double UmbralReconocimiento = 55.0;

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

        public static bool EsReconocimientoValido(int label, double distance, int totalPersonas)
        {
            if (label < 0 || label >= totalPersonas)
                return false;

            if (distance >= UmbralReconocimiento)
                return false;

            return true;
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