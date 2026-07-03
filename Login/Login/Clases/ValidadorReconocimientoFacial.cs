using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Drawing = System.Drawing;

namespace Login.Clases
{
    /// <summary>
    /// Contiene todas las validaciones del módulo de reconocimiento facial:
    /// biometría, bloqueo temporal por intentos fallidos y entradas de usuario.
    /// </summary>
    public static partial class ValidadorReconocimientoFacial
    {
        // ── Umbral de reconocimiento ──────────────────────────────────────────────

        /// <summary>
        /// Distancia LBPH máxima aceptada como coincidencia válida.
        /// Valores menores indican mayor similitud.
        /// </summary>
        public const double UmbralReconocimiento = 115.0;

        // ── Bloqueo temporal ──────────────────────────────────────────────────────

        /// <summary>
        /// Almacena (intentos acumulados, bloqueado hasta) indexado por correo de usuario.
        /// ConcurrentDictionary porque se accede tanto desde el hilo de UI como desde
        /// tareas de reconocimiento en segundo plano.
        /// </summary>
        private static readonly ConcurrentDictionary<string, (int Intentos, DateTime? BloqueadoHasta)>
            _intentosFallidos = new();

        private const int MaxIntentosFallidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Comprueba si la cuenta del usuario está bloqueada temporalmente.
        /// </summary>
        public static (bool Bloqueada, string Mensaje) VerificarBloqueo(string correo)
        {
            if (!_intentosFallidos.TryGetValue(correo, out var estado) || !estado.BloqueadoHasta.HasValue)
                return (false, string.Empty);

            var ahora = DateTime.UtcNow;
            if (ahora >= estado.BloqueadoHasta.Value)
                return (false, string.Empty);

            int minutosRestantes = (int)(estado.BloqueadoHasta.Value - ahora).TotalMinutes + 1;
            return (true, $"🔒 Cuenta bloqueada temporalmente. Intenta de nuevo en {minutosRestantes} min.");
        }

        /// <summary>
        /// Registra un intento fallido. Aplica bloqueo si se alcanza el límite.
        /// </summary>
        public static void RegistrarIntentoFallido(string correo)
        {
            _intentosFallidos.AddOrUpdate(
                correo,
                _ => (1, null),
                (_, estado) =>
                {
                    int nuevosIntentos = estado.Intentos + 1;
                    DateTime? nuevoBloqueadoHasta = null;

                    if (nuevosIntentos >= MaxIntentosFallidos)
                    {
                        nuevoBloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
                        nuevosIntentos = 0;
                    }

                    return (nuevosIntentos, nuevoBloqueadoHasta);
                });
        }

        /// <summary>Limpia el contador tras una autenticación exitosa.</summary>
        public static void LimpiarIntentosFallidos(string correo) =>
            _intentosFallidos.TryRemove(correo, out _);

        /// <summary>Devuelve los intentos fallidos acumulados del usuario.</summary>
        public static int ObtenerIntentosFallidos(string correo) =>
            _intentosFallidos.TryGetValue(correo, out var estado) ? estado.Intentos : 0;

        // ── Validación biométrica ─────────────────────────────────────────────────

        /// <summary>
        /// Valida que el rostro reconocido corresponda exactamente al usuario en sesión.
        /// No basta con que el rostro sea "conocido"; debe coincidir con el label
        /// registrado para la cuenta activa.
        /// </summary>
        /// <param name="label">Índice predicho por el reconocedor LBPH.</param><
        /// <param name="distance">Distancia LBPH (menor = más similar).</param>
        /// <param name="labelEsperado">Índice en _personas del usuario en sesión.</param>
        public static bool EsReconocimientoValido(int label, double distance, int labelEsperado) =>
            distance <= UmbralReconocimiento && label == labelEsperado;

        // ── Validaciones de entrada ───────────────────────────────────────────────

        [GeneratedRegex(@"^[\p{L}]+( [\p{L}]+)*$")]
        private static partial Regex RegexNombre();

        /// <summary>Valida el nombre ingresado para registrar una nueva persona.</summary>
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

        /// <summary>Valida que se haya capturado una fotografía antes de registrar.</summary>
        public static (bool Ok, string Mensaje) ValidarFotoCapturada(Drawing.Bitmap foto) =>
            foto == null
                ? (false, "⚠ Primero captura una foto del rostro.")
                : (true, string.Empty);

        /// <summary>Valida que haya al menos una cámara disponible en el sistema.</summary>
        public static (bool Ok, string Mensaje) ValidarCamaraDisponible(int totalCamaras) =>
            totalCamaras == 0
                ? (false, "No se encontró ninguna cámara disponible.")
                : (true, string.Empty);

        /// <summary>Valida que haya un rostro detectado en el frame actual.</summary>
        public static (bool Ok, string Mensaje) ValidarRostroDetectado(bool rostroDetectado) =>
            !rostroDetectado
                ? (false, "No hay ningún rostro detectado frente a la cámara.")
                : (true, string.Empty);

        /// <summary>Valida que el sistema esté en modo Registro antes de capturar foto.</summary>
        public static (bool Ok, string Mensaje) ValidarModoRegistroActivo(bool esModoRegistro) =>
            !esModoRegistro
                ? (false, "Activa el Modo Registro antes de capturar una foto.")
                : (true, string.Empty);
    }
}