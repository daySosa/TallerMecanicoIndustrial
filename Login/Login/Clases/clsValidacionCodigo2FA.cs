namespace Login.Clases
{
    public static class clsValidacionCodigo2FA
    {
        public const int LongitudCodigo = 6;

        public static (bool Ok, string Mensaje) ValidarCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                return (false, "⚠ Ingresa el código de verificación.");

            if (!EsSoloNumeros(codigo))
                return (false, "⚠ El código solo debe contener números.");

            if (codigo.Length != LongitudCodigo)
                return (false, $"⚠ El código debe tener exactamente {LongitudCodigo} dígitos.");

            return (true, string.Empty);
        }

        private static bool EsSoloNumeros(string texto)
        {
            foreach (char c in texto)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }
    }
}