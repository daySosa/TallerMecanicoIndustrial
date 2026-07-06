namespace Login.Clases
{
    /// <summary>
    /// Validación de formato del formulario de login y reglas de cálculo de bloqueo
    /// por intentos fallidos. El conteo de intentos y la fecha de bloqueo viven en
    /// la tabla LOGIN; esta clase solo calcula reglas puras a partir de esos valores.
    /// </summary>
    public static class ValidadorLogin
    {
        /// <summary>
        /// Reglas de bloqueo progresivo: al llegar a "Umbral" intentos fallidos,
        /// se bloquea por "Minutos" minutos. Única fuente de verdad — agregar o
        /// modificar un escalón de bloqueo solo requiere tocar esta tabla.
        /// </summary>
        private static readonly (int Umbral, int Minutos)[] _reglasBloqueo =
        [
            (3, 1),
            (6, 3),
            (9, 5),
        ];

        private const int LongitudMinimaContrasena = 6;

        public static string ValidarCorreo(string correo)
        {
            if (!ValidacionesGenerales.EsRequerido(correo))
                return "⚠ Ingresa tu correo electrónico para iniciar sesión.";

            if (!ValidacionesGenerales.EsCorreoValido(correo))
                return $"⚠ \"{correo.Trim()}\" no parece un correo válido. Revisa que tenga el formato usuario@dominio.com.";

            return null;
        }

        public static string ValidarContrasena(string contrasena)
        {
            if (!ValidacionesGenerales.EsRequerido(contrasena))
                return "⚠ Ingresa tu contraseña.";

            if (!ValidacionesGenerales.TieneLongitudMinima(contrasena, LongitudMinimaContrasena))
                return $"⚠ Faltan {LongitudMinimaContrasena - contrasena.Length} caracter(es); " +
                       $"la contraseña debe tener al menos {LongitudMinimaContrasena}.";

            return null;
        }

        /// <summary>
        /// Minutos de bloqueo que corresponden exactamente al llegar a "intentos"
        /// fallidos. Devuelve 0 si "intentos" no coincide con ningún umbral
        /// (es decir, aún no toca bloquear en este intento).
        /// </summary>
        public static int MinutosDeBloqueo(int intentos)
        {
            foreach (var (umbral, minutos) in _reglasBloqueo)
            {
                if (intentos == umbral) return minutos;
            }

            // Superó el último umbral definido: se mantiene el bloqueo más severo.
            var (ultimoUmbral, ultimosMinutos) = _reglasBloqueo[^1];
            return intentos > ultimoUmbral ? ultimosMinutos : 0;
        }

        /// <summary>
        /// Cuántos intentos le quedan al usuario antes de alcanzar el próximo
        /// umbral de bloqueo.
        /// </summary>
        public static int IntentosRestantesParaBloqueo(int intentos)
        {
            foreach (var (umbral, _) in _reglasBloqueo)
            {
                if (intentos < umbral) return umbral - intentos;
            }

            return 0;
        }
    }
}