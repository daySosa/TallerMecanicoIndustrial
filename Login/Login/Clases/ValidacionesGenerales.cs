using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Login.Clases
{
    public class ValidacionesGenerales
    {
        public ValidacionesGenerales() { }

        // ─────────────────────────────────────────────────────────────
        // REGEX PRECOMPILADAS (evita recompilar el patrón en cada llamada)
        // ─────────────────────────────────────────────────────────────
        private static readonly Regex RegexCorreo = new(
            @"^[a-z0-9][a-z0-9._%+\-]*@[a-z0-9.\-]+\.[a-z]{2,}$", RegexOptions.Compiled);

        private static readonly Regex RegexTelefonoLongitud = new(@"^\d{8}$", RegexOptions.Compiled);
        private static readonly Regex RegexTelefonoPrefijo = new(@"^[2389]", RegexOptions.Compiled);
        private static readonly Regex RegexPlaca = new(@"^[A-Z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex RegexAnio4Digitos = new(@"^\d{4}$", RegexOptions.Compiled);
        private static readonly Regex RegexDni = new(@"^\d{13}$", RegexOptions.Compiled);
        private static readonly Regex RegexIniciaConLetra = new(
            @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ]", RegexOptions.Compiled);
        private static readonly Regex RegexRepeticionExcesiva = new(@"(.)\1{4,}", RegexOptions.Compiled);

        private static readonly Regex RegexMayuscula = new(@"[A-Z]", RegexOptions.Compiled);
        private static readonly Regex RegexMinuscula = new(@"[a-z]", RegexOptions.Compiled);
        private static readonly Regex RegexDigito = new(@"\d", RegexOptions.Compiled);
        private static readonly Regex RegexEspecial = new(@"[^a-zA-Z0-9]", RegexOptions.Compiled);
        private static readonly Regex RegexEspacioBlanco = new(@"\s", RegexOptions.Compiled);

        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarFormularioVacio(params string[] campos)
        {
            if (campos.All(string.IsNullOrWhiteSpace))
            {
                MessageBox.Show("⚠ Complete todos los campos requeridos antes de guardar.",
                    "Formulario vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // COMBO / TEXTO REQUERIDO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarComboSeleccionado(object selectedItem, string nombreCampo)
        {
            if (selectedItem == null)
            {
                MessageBox.Show($"⚠ Selecciona un {nombreCampo}.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarTextoRequerido(string texto, string nombreCampo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show($"⚠ Escribe el {nombreCampo}.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarTextoRequerido(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // PRECIO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPrecio(string texto, out decimal precio)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (!decimal.TryParse(limpio, out precio) || precio <= 0)
            {
                MessageBox.Show("⚠ Ingresa un precio válido mayor a 0.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static string FormatearPrecio(string texto)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (decimal.TryParse(limpio, out decimal valor) && valor > 0)
                return "L " + valor.ToString("N2");
            return "";
        }

        public static string LimpiarPrefijoPrecio(string texto)
        {
            return texto.Replace("L", "").Replace(" ", "").Trim();
        }

        // ─────────────────────────────────────────────────────────────
        // FECHA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFecha(string texto, out DateTime fecha)
        {
            if (!DateTime.TryParseExact(texto.Trim(), "dd/MM/yyyy HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out fecha))
            {
                MessageBox.Show("⚠ Formato de fecha inválido. Usa dd/MM/yyyy HH:mm\nEjemplo: 13/03/2026 14:30",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarFechaEntrega(DateTime? fechaInicio, DateTime? fechaEntrega)
        {
            if (fechaInicio.HasValue && fechaEntrega.HasValue &&
                fechaEntrega.Value < fechaInicio.Value)
            {
                MessageBox.Show("⚠ La fecha de entrega no puede ser anterior a la fecha de inicio.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarMesOrden(DateTime? fecha)
        {
            if (fecha.HasValue)
            {
                var hoy = DateTime.Today;
                if (fecha.Value.Year < hoy.Year ||
                   (fecha.Value.Year == hoy.Year && fecha.Value.Month < hoy.Month))
                {
                    MessageBox.Show("No se pueden actualizar órdenes de meses anteriores.",
                        "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CANTIDADES / ENTEROS
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarEnteroPositivo(string texto, out int resultado, string titulo)
        {
            if (!int.TryParse(texto.Trim(), out resultado) || resultado <= 0)
            {
                MessageBox.Show("⚠ La cantidad debe ser un número entero mayor a 0.",
                    titulo, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarStock(int cantidad, int stockDisponible)
        {
            if (cantidad > stockDisponible)
            {
                MessageBox.Show(
                    $"⚠ Stock insuficiente. Solo hay {stockDisponible} unidades disponibles.",
                    "Stock insuficiente", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // TEXTO — formato y longitud
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarSoloDigitos(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (!texto.All(char.IsDigit))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        public static bool ValidarTextoAlfanumerico(string texto, string nombreCampo)
        {
            if (!texto.Trim().All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show($"⚠ El {nombreCampo} no debe contener caracteres especiales.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarLongitudMaxima(string texto, int max, string nombreCampo)
        {
            if (!string.IsNullOrEmpty(texto) && texto.Length > max)
            {
                MessageBox.Show($"⚠ El {nombreCampo} no puede superar {max} caracteres.",
                    "Longitud inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarIniciaConLetra(string texto, string nombreCampo)
        {
            if (!RegexIniciaConLetra.IsMatch(texto.Trim()))
            {
                MessageBox.Show($"⚠ El {nombreCampo} debe iniciar con una letra.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarNoEsSoloNumeros(string texto, string nombreCampo)
        {
            if (texto.Trim().All(char.IsDigit))
            {
                MessageBox.Show($"⚠ El {nombreCampo} no puede ser solo números.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarSinRepeticionExcesiva(string texto, string nombreCampo)
        {
            if (RegexRepeticionExcesiva.IsMatch(texto.Trim()))
            {
                MessageBox.Show($"⚠ El {nombreCampo} contiene caracteres repetidos excesivamente.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CORREO
        // ─────────────────────────────────────────────────────────────
        public static string ValidarCorreoLogin(string correo)
        {
            correo = correo.Trim();
            if (string.IsNullOrWhiteSpace(correo)) return "⚠ El correo es obligatorio.";
            if (correo.Contains(" ")) return "⚠ El correo no puede contener espacios.";
            if (!correo.Contains("@")) return "⚠ El correo debe contener '@'.";

            string[] partes = correo.Split('@');
            string usuario = partes[0];
            string dominio = partes[1];
            if (usuario.Any(char.IsUpper)) return "⚠ El correo no puede contener letras mayúsculas.";
            if (dominio.Any(char.IsUpper)) return "⚠ El dominio no puede contener letras mayúsculas.";
            if (usuario.All(char.IsDigit)) return "⚠ El correo no puede ser solo números.";
            if (correo.Length > 100) return "⚠ El correo es demasiado largo (máximo 100 caracteres).";

            if (!RegexCorreo.IsMatch(correo))
                return "⚠ Ingresa un correo electrónico válido.";

            if (correo.StartsWith(".")) return "⚠ El correo no puede empezar con un punto.";
            if (correo.Contains("..")) return "⚠ El correo no puede tener puntos consecutivos.";

            string[] dominiosPermitidos = { "@gmail.com", "@hotmail.com", "@outlook.com", "@yahoo.com" };
            if (!dominiosPermitidos.Any(d => correo.EndsWith(d)))
                return "⚠ Dominio no permitido.";

            return null;
        }

        // ─────────────────────────────────────────────────────────────
        // TELÉFONO
        // ─────────────────────────────────────────────────────────────

        public static bool Telefono(string valor, Control campo = null)
        {
            string tel = valor.Trim();

            if (!RegexTelefonoLongitud.IsMatch(tel))
            {
                MessageBox.Show("El teléfono debe contener solo números y tener exactamente 8 dígitos.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!RegexTelefonoPrefijo.IsMatch(tel))
            {
                MessageBox.Show("El teléfono debe iniciar con 2, 3, 8 o 9.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (tel.Distinct().Count() == 1)
            {
                MessageBox.Show("⚠ El teléfono no puede tener todos los dígitos iguales.",
                    "Teléfono inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // PLACA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPlaca(string placa)
        {
            string p = placa.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(p))
            {
                MessageBox.Show("⚠ La placa es obligatoria.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!RegexPlaca.IsMatch(p))
            {
                MessageBox.Show("⚠ La placa solo puede contener letras y números.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (p.All(char.IsDigit))
            {
                MessageBox.Show("⚠ La placa no puede ser solo números.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (p.Length < 6)
            {
                MessageBox.Show("⚠ La placa debe tener al menos 6 caracteres.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return ValidarSinRepeticionExcesiva(p, "placa");
        }

        // ─────────────────────────────────────────────────────────────
        // AÑO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarAnio(string texto, out int año)
        {
            año = 0;
            if (!int.TryParse(texto, out año) || año < 1900 || año > DateTime.Now.Year + 1)
            {
                MessageBox.Show("⚠ El año ingresado no es válido.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarAnioFormato(string texto)
        {
            if (!RegexAnio4Digitos.IsMatch(texto.Trim()))
            {
                MessageBox.Show("⚠ El año debe tener exactamente 4 dígitos.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // DNI
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteDNI(string textoDNI, string clienteVerificado)
        {
            if (string.IsNullOrWhiteSpace(textoDNI))
            {
                MessageBox.Show("⚠ Debes ingresar el DNI del cliente y presionar 'Buscar'.",
                    "DNI requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(clienteVerificado))
            {
                MessageBox.Show("⚠ Debes verificar el DNI del cliente antes de guardar.\n\n" +
                                "Ingresa el DNI y presiona el botón 'Buscar'.",
                    "Cliente no buscado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static bool ValidarFormatoDNI(string dni)
        {
            if (!RegexDni.IsMatch(dni.Trim()))
            {
                MessageBox.Show("⚠ El DNI debe contener exactamente 13 dígitos numéricos.",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // RANGO DE PRECIOS
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarRangoPrecios(string textoMin, string textoMax, out decimal pMin, out decimal pMax)
        {
            pMin = 0;
            pMax = decimal.MaxValue;

            if (!string.IsNullOrWhiteSpace(textoMin) && !decimal.TryParse(textoMin, out pMin))
            {
                MessageBox.Show("⚠ El precio mínimo debe ser un número válido.",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!string.IsNullOrWhiteSpace(textoMax) && !decimal.TryParse(textoMax, out pMax))
            {
                MessageBox.Show("⚠ El precio máximo debe ser un número válido.",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            pMin = decimal.TryParse(textoMin, out decimal pm) ? pm : 0;
            pMax = decimal.TryParse(textoMax, out decimal px) ? px : decimal.MaxValue;

            if (pMin > pMax)
            {
                MessageBox.Show("⚠ El precio mínimo no puede ser mayor que el precio máximo.",
                    "Rango inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // LOGIN — helpers básicos
        // ─────────────────────────────────────────────────────────────

        public static string ValidarContrasenaLogin(string contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena)) return "⚠ La contraseña es obligatoria.";
            if (contrasena.Contains(" ")) return "⚠ La contraseña no puede contener espacios.";
            if (contrasena.Length > 50) return "⚠ La contraseña es demasiado larga (máximo 50 caracteres).";
            return null;
        }

        public static bool EsRequerido(string valor)
            => !string.IsNullOrWhiteSpace(valor);

        public static bool EsCorreoValido(string correo)
            => !string.IsNullOrWhiteSpace(correo) && RegexCorreo.IsMatch(correo.Trim());

        public static bool EsSoloNumeros(string valor)
            => !string.IsNullOrEmpty(valor) && valor.All(char.IsDigit);

        public static bool TieneLongitudExacta(string valor, int longitud)
            => (valor?.Length ?? 0) == longitud;

        public static bool TieneLongitudMinima(string valor, int minimo)
            => (valor?.Length ?? 0) >= minimo;

        // ─────────────────────────────────────────────────────────────
        // CONTRASEÑA — VALIDACIÓN COMPLETA DE FORTALEZA
        // Usado en el flujo de "Recuperar contraseña". Devuelve el primer
        // requisito incumplido, o null si la contraseña es válida.
        // ─────────────────────────────────────────────────────────────

        public const int LongitudMinimaContrasena = 8;
        public const int LongitudMaximaContrasena = 64;

        private static readonly HashSet<string> ContrasenasComunes = new(StringComparer.OrdinalIgnoreCase)
        {
            "12345678", "123456789", "1234567890", "password", "contraseña",
            "qwerty123", "abc12345", "11111111", "00000000", "password1",
            "admin123", "iloveyou", "letmein1", "welcome1", "12345678a",
            "qwertyui", "asdfghjk", "1q2w3e4r", "taller123", "mecanico1"
        };

        public static string ValidarFortalezaContrasena(string contrasena, string correo = null)
        {
            if (!EsRequerido(contrasena))
                return "⚠ Ingresa tu nueva contraseña.";

            if (contrasena.Length < LongitudMinimaContrasena)
                return $"⚠ La contraseña debe tener al menos {LongitudMinimaContrasena} caracteres.";

            if (contrasena.Length > LongitudMaximaContrasena)
                return $"⚠ La contraseña no puede superar {LongitudMaximaContrasena} caracteres.";

            if (RegexEspacioBlanco.IsMatch(contrasena))
                return "⚠ La contraseña no puede contener espacios ni tabulaciones.";

            if (!RegexMayuscula.IsMatch(contrasena))
                return "⚠ La contraseña debe incluir al menos una letra mayúscula.";

            if (!RegexMinuscula.IsMatch(contrasena))
                return "⚠ La contraseña debe incluir al menos una letra minúscula.";

            if (!RegexDigito.IsMatch(contrasena))
                return "⚠ La contraseña debe incluir al menos un número.";

            if (!RegexEspecial.IsMatch(contrasena))
                return "⚠ La contraseña debe incluir al menos un carácter especial (ej: @, #, $, %).";

            if (RegexRepeticionExcesiva.IsMatch(contrasena))
                return "⚠ La contraseña no puede repetir el mismo carácter muchas veces seguidas.";

            if (TieneSecuenciaObvia(contrasena))
                return "⚠ La contraseña no puede contener secuencias obvias (ej: 1234, abcd).";

            if (ContrasenasComunes.Contains(contrasena))
                return "⚠ Esta contraseña es demasiado común. Elige una más segura.";

            if (!string.IsNullOrWhiteSpace(correo))
            {
                string usuarioCorreo = correo.Split('@')[0];
                if (contrasena.Contains(usuarioCorreo, StringComparison.OrdinalIgnoreCase))
                    return "⚠ La contraseña no puede contener tu correo o nombre de usuario.";
            }

            return null;
        }

        /// <summary>Detecta secuencias ascendentes/descendentes de 4+ caracteres (1234, dcba, etc.).</summary>
        private static bool TieneSecuenciaObvia(string texto)
        {
            const int longitudSecuencia = 4;
            int consecutivosAsc = 1;
            int consecutivosDesc = 1;

            for (int i = 1; i < texto.Length; i++)
            {
                int diferencia = texto[i] - texto[i - 1];

                consecutivosAsc = diferencia == 1 ? consecutivosAsc + 1 : 1;
                consecutivosDesc = diferencia == -1 ? consecutivosDesc + 1 : 1;

                if (consecutivosAsc >= longitudSecuencia || consecutivosDesc >= longitudSecuencia)
                    return true;
            }
            return false;
        }
    }
}