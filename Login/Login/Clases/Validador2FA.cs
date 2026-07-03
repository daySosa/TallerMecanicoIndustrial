#nullable enable
namespace Login.Clases
{
    /// <summary>
    /// Valida el formato del código OTP de verificación en dos pasos (2FA).
    /// Esta clase solo valida FORMATO (longitud, caracteres permitidos, etc.).
    /// La validación contra el código real emitido se realiza contra la base de datos.
    /// </summary>
    public static class Validador2FA
    {
        /// <summary>Longitud exacta que debe tener el código OTP.</summary>
        public const int LongitudCodigo = 6;

        private const string MsgVacio = "⚠ Ingresa el código de verificación.";
        private const string MsgCaracteresInvalidos = "⚠ El código solo debe contener números.";
        private const string MsgLongitudInvalida = "⚠ El código debe tener exactamente 6 dígitos.";

        /// <summary>
        /// Valida que el código cumpla con el formato esperado: no vacío,
        /// longitud exacta y compuesto únicamente por dígitos ASCII (0-9).
        /// </summary>
        /// <param name="codigo">Código ingresado por el usuario (puede ser null).</param>
        /// <returns>
        /// Tupla (Ok, Mensaje): Ok = true si el formato es válido; Mensaje contiene
        /// el motivo del rechazo cuando Ok = false, o cadena vacía si es válido.
        /// </returns>
        public static (bool Ok, string Mensaje) ValidarCodigo(string? codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return (false, MsgVacio);

            ReadOnlySpan<char> span = codigo.AsSpan().Trim();

            if (span.Length != LongitudCodigo)
                return (false, MsgLongitudInvalida);

            return SonSoloDigitosAscii(span)
                ? (true, string.Empty)
                : (false, MsgCaracteresInvalidos);
        }

        /// <summary>
        /// Variante rápida que solo indica si el código es válido, sin mensaje.
        /// Útil para validaciones internas donde no se necesita el detalle del error.
        /// </summary>
        public static bool EsCodigoValido(string? codigo) => ValidarCodigo(codigo).Ok;

        private static bool SonSoloDigitosAscii(ReadOnlySpan<char> texto)
        {
            foreach (char c in texto)
            {
                if (!char.IsAsciiDigit(c))
                    return false;
            }
            return true;
        }
    }
}