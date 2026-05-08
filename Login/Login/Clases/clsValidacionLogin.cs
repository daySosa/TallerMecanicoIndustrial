namespace Login.Clases
{
    public static class clsValidacionLogin
    {
        // ── Intentos fallidos ─────────────────────────────────────────────────
        private static int _intentosFallidos = 0;
        private static DateTime? _tiempoBloqueo = null;
        private const int MaxIntentos = 3;
        private const int MinutosBloqueo = 5;

        // ── Validación de credenciales ────────────────────────────────────────
        public static (bool Ok, string Mensaje) ValidarCredenciales(string correo, string contrasena)
        {
            if (!clsValidaciones.EsRequerido(correo))
                return (false, "⚠ Ingresa tu correo electrónico.");

            if (!clsValidaciones.EsCorreoValido(correo))
                return (false, "⚠ El correo no tiene un formato válido.");

            if (!clsValidaciones.EsRequerido(contrasena))
                return (false, "⚠ Ingresa tu contraseña.");

            if (!clsValidaciones.TieneLongitudMinima(contrasena, 6))
                return (false, "⚠ La contraseña debe tener al menos 6 caracteres.");

            return (true, string.Empty);
        }

        // ── Control de bloqueo ────────────────────────────────────────────────
        public static string? ValidarTodo(string correo, string contrasena)
        {
            var (ok, mensaje) = ValidarCredenciales(correo, contrasena);
            return ok ? null : mensaje;
        }

        public static void ResetearIntentos()
        {
            _intentosFallidos = 0;
            _tiempoBloqueo = null;
        }

        public static void RegistrarIntentoFallido()
        {
            _intentosFallidos++;
            if (_intentosFallidos >= MaxIntentos)
                _tiempoBloqueo = DateTime.Now.AddMinutes(MinutosBloqueo);
        }

        public static bool EstasBloqueado()
        {
            if (_tiempoBloqueo == null) return false;

            if (DateTime.Now >= _tiempoBloqueo)
            {
                // Bloqueo expirado — resetear automáticamente
                ResetearIntentos();
                return false;
            }

            return true;
        }

        public static TimeSpan TiempoRestanteBloqueo()
        {
            if (_tiempoBloqueo == null || DateTime.Now >= _tiempoBloqueo)
                return TimeSpan.Zero;

            return _tiempoBloqueo.Value - DateTime.Now;
        }

        public static int IntentosRestantes()
            => Math.Max(0, MaxIntentos - _intentosFallidos);
    }
}