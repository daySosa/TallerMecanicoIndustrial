namespace Login.Clases
{
    /// <summary>
    /// Toda la validación específica del formulario de login: mensajes de error
    /// por campo y control de bloqueo por intentos fallidos (anti fuerza bruta).
    /// El estado de intentos es estático porque debe persistir mientras la app
    /// está abierta, sin importar qué ventana esté activa.
    /// </summary>
    public static class ValidadorLogin
    {
        private const int MaxIntentos = 3;
        private const int MinutosBloqueo = 5;

        private static int _intentosFallidos = 0;
        private static DateTime? _bloqueadoHasta = null;

        /// <summary>Devuelve el mensaje de error del correo, o null si es válido.</summary>
        public static string ValidarCorreo(string correo)
        {
            if (!ValidacionesGenerales.EsRequerido(correo))
                return "⚠ Ingresa tu correo electrónico.";

            if (!ValidacionesGenerales.EsCorreoValido(correo))
                return "⚠ El correo no tiene un formato válido.";

            return null;
        }

        /// <summary>Devuelve el mensaje de error de la contraseña, o null si es válida.</summary>
        public static string ValidarContrasena(string contrasena)
        {
            if (!ValidacionesGenerales.EsRequerido(contrasena))
                return "⚠ Ingresa tu contraseña.";

            if (!ValidacionesGenerales.TieneLongitudMinima(contrasena, 6))
                return "⚠ La contraseña debe tener al menos 6 caracteres.";

            return null;
        }

        public static void ResetearIntentos()
        {
            _intentosFallidos = 0;
            _bloqueadoHasta = null;
        }

        public static void RegistrarIntentoFallido()
        {
            _intentosFallidos++;
            if (_intentosFallidos >= MaxIntentos)
                _bloqueadoHasta = DateTime.Now.AddMinutes(MinutosBloqueo);
        }

        public static bool EstaBloqueado()
        {
            if (_bloqueadoHasta is null) return false;

            if (DateTime.Now >= _bloqueadoHasta)
            {
                ResetearIntentos(); // el bloqueo expiró, se resetea solo
                return false;
            }

            return true;
        }

        public static TimeSpan TiempoRestanteBloqueo()
        {
            if (_bloqueadoHasta is null || DateTime.Now >= _bloqueadoHasta)
                return TimeSpan.Zero;

            return _bloqueadoHasta.Value - DateTime.Now;
        }

        public static int IntentosRestantes() =>
            Math.Max(0, MaxIntentos - _intentosFallidos);

        public static int MinutosDeBloqueo(int intentos)
        {
            if (intentos >= 9) return 5;
            if (intentos >= 6) return 3;
            if (intentos >= 3) return 1;
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