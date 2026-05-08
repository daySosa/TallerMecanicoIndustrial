namespace Login.Clases
{
    public static class clsValidacionCodigo2FA
    {
        public const int LongitudCodigo = 6;

        public static (bool Ok, string Mensaje) ValidarCodigo(string codigo)
        {
            // Usa clsValidaciones — requerido
            if (!clsValidaciones.EsRequerido(codigo))
                return (false, "⚠ Ingresa el código de verificación.");

            // Usa clsValidaciones — solo números
            if (!clsValidaciones.EsSoloNumeros(codigo))
                return (false, "⚠ El código solo debe contener números.");

            // Usa clsValidaciones — longitud exacta
            if (!clsValidaciones.TieneLongitudExacta(codigo, LongitudCodigo))
                return (false, $"⚠ El código debe tener exactamente {LongitudCodigo} dígitos.");

            return (true, string.Empty);
        }
    }
}