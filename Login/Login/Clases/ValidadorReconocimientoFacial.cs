using System.Text.RegularExpressions;
using Drawing = System.Drawing;

namespace Login.Clases
{

    public static partial class ValidadorReconocimientoFacial
    {
        // ── Umbral de reconocimiento ──────────────────────────────────────────────

        public const double UmbralReconocimiento = 115.0;

        public static bool EsReconocimientoValido(int label, double distance, int labelEsperado) =>
            distance <= UmbralReconocimiento && label == labelEsperado;

        [GeneratedRegex(@"^[\p{L}]+( [\p{L}]+)*$")]
        private static partial Regex RegexNombre();

        public static (bool Ok, string Mensaje) ValidarNombre(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return (false, "⚠ Ingresa el nombre de la persona que estás registrando.");

            nombre = nombre.Trim();

            if (nombre.Length < 2)
                return (false, $"⚠ \"{nombre}\" es muy corto; el nombre debe tener al menos 2 caracteres.");

            if (nombre.Length > 100)
                return (false, $"⚠ El nombre tiene {nombre.Length} caracteres; el máximo permitido es 100.");

            if (!RegexNombre().IsMatch(nombre))
                return (false, "⚠ El nombre solo puede contener letras y un espacio simple entre palabras (sin números ni símbolos).");

            return (true, string.Empty);
        }

        public static (bool Ok, string Mensaje) ValidarFotoCapturada(Drawing.Bitmap foto) =>
            foto == null
                ? (false, "⚠ Aún no has capturado ninguna foto. Presiona \"Capturar\" con el rostro frente a la cámara.")
                : (true, string.Empty);

        public static (bool Ok, string Mensaje) ValidarCamaraDisponible(int totalCamaras) =>
            totalCamaras == 0
                ? (false, "⚠ No se detectó ninguna cámara conectada. Conecta una cámara o verifica los permisos del sistema.")
                : (true, string.Empty);

        public static (bool Ok, string Mensaje) ValidarRostroDetectado(bool rostroDetectado) =>
            !rostroDetectado
                ? (false, "⚠ No se detecta ningún rostro frente a la cámara. Acércate y asegúrate de tener buena iluminación.")
                : (true, string.Empty);

        public static (bool Ok, string Mensaje) ValidarModoRegistroActivo(bool esModoRegistro) =>
            !esModoRegistro
                ? (false, "⚠ El Modo Registro no está activo. Actívalo antes de capturar una foto.")
                : (true, string.Empty);
    }
}