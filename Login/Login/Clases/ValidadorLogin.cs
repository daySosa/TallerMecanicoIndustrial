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
        /// Reglas de bloqueo progresivo, ordenadas ascendentemente por "Umbral":
        /// al llegar a "Umbral" intentos fallidos, se bloquea por "Minutos" minutos.
        /// Única fuente de verdad — agregar o modificar un escalón de bloqueo solo
        /// requiere tocar esta tabla.
        /// </summary>
        /// <remarks>
        /// El arreglo es intencionalmente pequeño (recorrido lineal es más rápido
        /// y más simple que una búsqueda binaria para 3-10 elementos). Si algún día
        /// crece a decenas de escalones, considerar Array.BinarySearch aprovechando
        /// que ya está ordenado.
        /// </remarks>
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
            {
                var faltantes = LongitudMinimaContrasena - contrasena.Length;
                return $"⚠ Faltan {faltantes} caracter(es); " +
                       $"la contraseña debe tener al menos {LongitudMinimaContrasena}.";
            }

            return null;
        }

        /// <summary>
        /// Minutos de bloqueo que corresponden exactamente al llegar a "intentos"
        /// fallidos. Devuelve 0 si "intentos" no coincide con ningún umbral
        /// (es decir, aún no toca bloquear en este intento).
        /// </summary>
        public static int MinutosDeBloqueo(int intentos)
        {
            intentos = NormalizarIntentos(intentos);

            foreach (var (umbral, minutos) in _reglasBloqueo)
            {
                if (intentos == umbral)
                    return minutos;
            }

            var (ultimoUmbral, ultimosMinutos) = _reglasBloqueo[^1];
            return intentos > ultimoUmbral ? ultimosMinutos : 0;
        }

        /// <summary>
        /// Cuántos intentos le quedan al usuario antes de alcanzar el próximo
        /// umbral de bloqueo. Devuelve 0 si ya superó (o igualó) el último umbral.
        /// </summary>
        public static int IntentosRestantesParaBloqueo(int intentos)
        {
            intentos = NormalizarIntentos(intentos);

            foreach (var (umbral, _) in _reglasBloqueo)
            {
                if (intentos < umbral)
                    return umbral - intentos;
            }

            return 0;
        }

        /// <summary>
        /// Evita que un valor negativo (dato corrupto o error previo) rompa
        /// las comparaciones de las reglas de bloqueo.
        /// </summary>
        private static int NormalizarIntentos(int intentos) => Math.Max(intentos, 0);
    }
}