using System.Collections.Concurrent;
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
                return (false, "⚠ Ingresa el nombre de la persona.");

            nombre = nombre.Trim();

            if (nombre.Length < 2)
                return (false, "⚠ El nombre debe tener al menos 2 caracteres.");

            if (nombre.Length > 100)
                return (false, "⚠ El nombre no puede superar los 100 caracteres.");

            if (!RegexNombre().IsMatch(nombre))
                return (false, "⚠ El nombre solo puede contener letras y espacios simples entre palabras.");

            return (true, string.Empty);
        }

        public static (bool Ok, string Mensaje) ValidarFotoCapturada(Drawing.Bitmap foto) =>
            foto == null
                ? (false, "⚠ Primero captura una foto del rostro.")
                : (true, string.Empty);

        public static (bool Ok, string Mensaje) ValidarCamaraDisponible(int totalCamaras) =>
            totalCamaras == 0
                ? (false, "No se encontró ninguna cámara disponible.")
                : (true, string.Empty);


        public static (bool Ok, string Mensaje) ValidarRostroDetectado(bool rostroDetectado) =>
            !rostroDetectado
                ? (false, "No hay ningún rostro detectado frente a la cámara.")
                : (true, string.Empty);


        public static (bool Ok, string Mensaje) ValidarModoRegistroActivo(bool esModoRegistro) =>
            !esModoRegistro
                ? (false, "Activa el Modo Registro antes de capturar una foto.")
                : (true, string.Empty);
    }
}
