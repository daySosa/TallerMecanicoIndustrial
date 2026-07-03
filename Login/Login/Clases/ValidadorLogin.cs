namespace Login.Clases
{
    /// <summary>
    /// Validación de formato del formulario de login y reglas de cálculo de bloqueo
    /// por intentos fallidos. El conteo de intentos y la fecha de bloqueo viven en
    /// la tabla LOGIN; esta clase solo calcula reglas puras a partir de esos valores.
    /// </summary>
    public static class ValidadorLogin
    {
        private const int PrimerUmbral = 3;
        private const int SegundoUmbral = 6;
        private const int TercerUmbral = 9;

        private const int MinutosPrimerBloqueo = 1;
        private const int MinutosSegundoBloqueo = 3;
        private const int MinutosTercerBloqueo = 5;

        public static string ValidarCorreo(string correo)
        {
            if (!ValidacionesGenerales.EsRequerido(correo))
                return "⚠ Ingresa tu correo electrónico.";

            if (!ValidacionesGenerales.EsCorreoValido(correo))
                return "⚠ El correo no tiene un formato válido.";

            return null;
        }

        public static string ValidarContrasena(string contrasena)
        {
            if (!ValidacionesGenerales.EsRequerido(contrasena))
                return "⚠ Ingresa tu contraseña.";

            if (!ValidacionesGenerales.TieneLongitudMinima(contrasena, 6))
                return "⚠ La contraseña debe tener al menos 6 caracteres.";

            return null;
        }

        public static int MinutosDeBloqueo(int intentos)
        {
            if (intentos == 3) return 1;  
            if (intentos == 6) return 3;  
            if (intentos == 9) return 5;  
            return 0;                      
        }

        public static int IntentosRestantesParaBloqueo(int intentos)
        {
            if (intentos < 3) return 3 - intentos;
            if (intentos < 6) return 6 - intentos;
            if (intentos < 9) return 9 - intentos;
            return 0;
        }
    }
}