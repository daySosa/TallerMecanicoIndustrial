using System.Text.RegularExpressions;

namespace Login.Clases
{
    /// <summary>
    /// Validador centralizado de contraseñas. Aplica todas las reglas de seguridad
    /// para la nueva contraseña en el flujo de recuperación. Reutilizable en
    /// registro o cambio de contraseña.
    /// </summary>
    internal static partial class ValidadorRecuperarContrasenia
    {
        public const int LongitudMinima = 8;
        public const int LongitudMaxima = 64;

        // Contraseñas triviales/mas usadas. Comparación case-insensitive.
        private static readonly HashSet<string> ContrasenasComunes = new(
        [
            "12345678", "123456789", "password", "contraseña", "qwerty123",
            "11111111", "abc12345", "admin123", "iloveyou", "letmein123",
            "password1", "qwertyui", "00000000", "asdfghjk", "1q2w3e4r",
            "contrasena", "12345678910", "taller123", "osm12345"
        ], StringComparer.OrdinalIgnoreCase);

        private static readonly string[] SecuenciasBase =
        [
            "0123456789", "qwertyuiop", "asdfghjkl", "zxcvbnm"
        ];

        [GeneratedRegex(@"[A-ZÁÉÍÓÚÑ]")]
        private static partial Regex RegexMayuscula();

        [GeneratedRegex(@"[a-záéíóúñ]")]
        private static partial Regex RegexMinuscula();

        [GeneratedRegex(@"\d")]
        private static partial Regex RegexDigito();

        [GeneratedRegex(@"[!@#$%^&*()_\-+=\[\]{}|\\:;""'<>,.?/~`]")]
        private static partial Regex RegexEspecial();

        [GeneratedRegex(@"\s")]
        private static partial Regex RegexEspacios();

        /// <summary>
        /// Valida la contraseña y devuelve el primer error encontrado, o null si es válida.
        /// </summary>
        public static string Validar(string contrasena, string correo = null)
        {
            if (string.IsNullOrEmpty(contrasena))
                return "⚠ Ingresa tu nueva contraseña.";

            if (RegexEspacios().IsMatch(contrasena))
                return "⚠ La contraseña no puede contener espacios en blanco.";

            if (contrasena.Length < LongitudMinima)
                return $"⚠ Te faltan {LongitudMinima - contrasena.Length} caracter(es). " +
                       $"La contraseña debe tener al menos {LongitudMinima}.";

            if (contrasena.Length > LongitudMaxima)
                return $"⚠ La contraseña tiene {contrasena.Length} caracteres; " +
                       $"el máximo permitido es {LongitudMaxima}.";

            if (!RegexMayuscula().IsMatch(contrasena))
                return "⚠ Falta al menos una letra mayúscula (A-Z).";

            if (!RegexMinuscula().IsMatch(contrasena))
                return "⚠ Falta al menos una letra minúscula (a-z).";

            if (!RegexDigito().IsMatch(contrasena))
                return "⚠ Falta al menos un número (0-9).";

            if (!RegexEspecial().IsMatch(contrasena))
                return "⚠ Falta al menos un carácter especial, por ejemplo: ! @ # $ % &";

            if (TieneCaracteresRepetidos(contrasena, 3))
                return "⚠ Tiene un carácter repetido 3 o más veces seguidas (ej: \"aaa\"). Varíalo un poco.";

            if (ContieneSecuencia(contrasena, 4))
                return "⚠ Contiene una secuencia predecible como \"1234\" o \"qwerty\". Elige algo menos obvio.";

            if (ContrasenasComunes.Contains(contrasena))
                return "⚠ Esta contraseña aparece en listas de contraseñas comunes filtradas. Elige una distinta.";

            if (!string.IsNullOrWhiteSpace(correo))
            {
                string usuario = correo.Split('@')[0];
                if (!string.IsNullOrEmpty(usuario) && usuario.Length >= 3 &&
                    contrasena.Contains(usuario, StringComparison.OrdinalIgnoreCase))
                {
                    return "⚠ No uses tu correo o nombre de usuario dentro de la contraseña; " +
                           "es fácil de adivinar.";
                }
            }

            return null;
        }

        private static bool TieneCaracteresRepetidos(string texto, int cantidad)
        {
            int repetidos = 1;
            for (int i = 1; i < texto.Length; i++)
            {
                repetidos = texto[i] == texto[i - 1] ? repetidos + 1 : 1;
                if (repetidos >= cantidad) return true;
            }
            return false;
        }

        private static bool ContieneSecuencia(string texto, int longitudMinima)
        {
            string textoLower = texto.ToLowerInvariant();

            foreach (var secuencia in SecuenciasBase)
            {
                for (int i = 0; i <= secuencia.Length - longitudMinima; i++)
                {
                    string sub = secuencia.Substring(i, longitudMinima);
                    if (textoLower.Contains(sub)) return true;

                    string subInvertida = new string([.. sub.Reverse()]);
                    if (textoLower.Contains(subInvertida)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Puntaje de fortaleza de 0 (vacío) a 4 (muy fuerte), para indicador visual.
        /// </summary>
        public static int CalcularFortaleza(string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena)) return 0;

            int puntaje = 0;
            if (contrasena.Length >= LongitudMinima) puntaje++;
            if (contrasena.Length >= 12) puntaje++;
            if (RegexMayuscula().IsMatch(contrasena) && RegexMinuscula().IsMatch(contrasena)) puntaje++;
            if (RegexDigito().IsMatch(contrasena)) puntaje++;
            if (RegexEspecial().IsMatch(contrasena)) puntaje++;

            return Math.Min(puntaje, 4);
        }
    }
}