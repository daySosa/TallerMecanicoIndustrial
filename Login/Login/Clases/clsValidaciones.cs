using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Login.Clases
{
    public class clsValidaciones
    {
        public clsValidaciones() { }

        // ── Validaciones de Actualizar Gasto ────────────────────────────────

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

        // ── Validaciones Actualizar Pago ─────────────────────────────────────

        // Sobrecarga de ValidarTextoRequerido con Action<string>
        public static bool ValidarTextoRequerido(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        public static bool ValidarSoloDigitos(string texto, string mensaje, Action<string> mostrarMensaje)
        {
            if (!texto.All(char.IsDigit))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }


        public static bool ValidarEntero(string texto, out int resultado, string mensaje, Action<string> mostrarMensaje)
        {
            if (!int.TryParse(texto, out resultado))
            {
                mostrarMensaje(mensaje);
                return false;
            }
            return true;
        }

        // ✅ Sobrecarga de ValidarPrecio con Action<string>
        public static bool ValidarPrecio(string texto, out decimal precio, Action<string> mostrarMensaje)
        {
            string limpio = texto.Replace("L", "").Replace(" ", "").Trim();
            if (!decimal.TryParse(limpio, out precio) || precio <= 0)
            {
                mostrarMensaje("⚠ El monto debe ser un número mayor a 0.");
                return false;
            }
            return true;
        }

        // ✅ Valida que la cantidad sea un número entero positivo
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

        // ✅ Valida que la cantidad solicitada no exceda el stock disponible
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

        // ✅ Valida que el DNI no esté vacío y tenga exactamente 13 dígitos
        public static bool ValidarDNIHondureño(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni) || !Regex.IsMatch(dni, @"^\d{13}$"))
            {
                MessageBox.Show("⚠ Ingrese un DNI válido de 13 dígitos.", "DNI inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un texto solo contenga letras y espacios
        public static bool ValidarSoloLetras(string texto, string nombreCampo)
        {
            if (!texto.Trim().All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show($"⚠ El {nombreCampo} solo puede contener letras y espacios.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida formato de correo electrónico (opcional, solo si no está vacío)
        public static bool ValidarCorreo(string correo)
        {
            if (!string.IsNullOrWhiteSpace(correo) &&
                !Regex.IsMatch(correo.Trim(), @"^[^@]+@[^@]+\.[^@]+$"))
            {
                MessageBox.Show("⚠ El correo no tiene un formato válido.\nEjemplo: nombre@dominio.com",
                    "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ✅ Valida que un teléfono tenga la longitud mínima requerida y restricciones
        public static bool ValidarTelefono(string telefono, int longitudMinima)
        {
            if (string.IsNullOrWhiteSpace(telefono) || telefono.Length < longitudMinima)
            {
                MessageBox.Show("⚠ Ingrese un teléfono válido (ej: 9999-9999).", "Campo requerido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool Telefono(string valor, Control campo = null)
        {
            string tel = valor.Trim();

            if (!Regex.IsMatch(tel, @"^\d{8}$"))
            {
                MessageBox.Show("El teléfono debe contener solo números y tener exactamente 8 dígitos.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(tel, @"^[2389]"))
            {
                MessageBox.Show("El teléfono debe iniciar con 2, 3, 8 o 9.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ✅ Valida DNI
        public static bool DNI(string valor, Control campo = null)
        {
            string dni = valor.Trim();

            // Solo números y exactamente 13 dígitos
            if (!Regex.IsMatch(dni, @"^\d{13}$"))
            {
                MessageBox.Show("El DNI debe contener solo números y tener exactamente 13 dígitos.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            // Primeros 4 dígitos: código municipio Honduras (0101 al 1818)
            int codigoMunicipio = Convert.ToInt32(dni.Substring(0, 4));
            if (codigoMunicipio < 101 || codigoMunicipio > 1899)
            {
                MessageBox.Show("Los primeros 4 dígitos del DNI deben corresponder a un código de municipio válido (ej: 0101 - 1818).",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            // Dígitos 5-8: año de registro (1900 al año actual)
            int anioRegistro = Convert.ToInt32(dni.Substring(4, 4));
            int anioActual = DateTime.Now.Year;
            if (anioRegistro < 1900 || anioRegistro > anioActual)
            {
                MessageBox.Show($"Los dígitos 5 al 8 del DNI deben ser un año válido entre 1900 y {anioActual}.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }

            // Últimos 6 dígitos: secuencial, no puede ser 000000
            string secuencial = dni.Substring(8, 5);
            if (secuencial == "000000")
            {
                MessageBox.Show("Los últimos 5 dígitos del DNI no pueden ser todos ceros.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);

                return false;
            }
            return true;
        }

        // ✅ Validaciones en Login (correo y contraseña)
        public static string ValidarCorreoLogin(string correo)
        {
            correo = correo.Trim();

            // Campo vacío
            if (string.IsNullOrWhiteSpace(correo))
                return "⚠ El correo es obligatorio.";

            // Sin espacios
            if (correo.Contains(" "))
                return "⚠ El correo no puede contener espacios.";

            // Sin mayúsculas en usuario ni dominio
            if (correo.Contains("@"))
            {
                string[] partes = correo.Split('@');
                string usuario = partes[0];
                string dominio = partes[1];

                if (usuario.Any(char.IsUpper))
                    return "⚠ El correo no puede contener letras mayúsculas.";

                if (dominio.Any(char.IsUpper))
                    return "⚠ El dominio no puede contener letras mayúsculas.";
            }

            // Máximo 100 caracteres
            if (correo.Length > 100)
                return "⚠ El correo es demasiado largo (máximo 100 caracteres).";

            // Formato general con Regex estricto
            if (!Regex.IsMatch(correo, @"^[a-z0-9][a-z0-9._%+\-]*@[a-z0-9.\-]+\.[a-z]{2,}$"))
                return "⚠ Ingresa un correo electrónico válido.";

            // No puede empezar con punto
            if (correo.StartsWith("."))
                return "⚠ El correo no puede empezar con un punto.";

            // No puede tener puntos consecutivos
            if (correo.Contains(".."))
                return "⚠ El correo no puede tener puntos consecutivos.";

            // Dominios permitidos
            string[] dominiosPermitidos = { "@gmail.com", "@hotmail.com", "@outlook.com", "@yahoo.com" };
            if (!dominiosPermitidos.Any(d => correo.EndsWith(d)))
                return "⚠ Dominio no permitido.";

            // Evitar caracteres repetidos en el usuario (antes del @)
            if (correo.Contains("@"))
            {
                string usuario = correo.Split('@')[0];

                // Si todos los caracteres son iguales (ej: aaaaaaa o 111111)
                if (usuario.Distinct().Count() == 1)
                    return "⚠ El correo no puede tener caracteres repetidos.";
            }

            return null;
        }

        public static string ValidarContrasenaLogin(string contrasena)
        {
            // Campo vacío
            if (string.IsNullOrWhiteSpace(contrasena))
                return "⚠ La contraseña es obligatoria.";

            // Sin espacios
            if (contrasena.Contains(" "))
                return "⚠ La contraseña no puede contener espacios.";

            // Mínimo 8 caracteres
            if (contrasena.Length < 8)
                return "⚠ La contraseña debe tener al menos 8 caracteres.";

            // Máximo 50 caracteres
            if (contrasena.Length > 50)
                return "⚠ La contraseña es demasiado larga (máximo 50 caracteres).";

            return null;
        }

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

        // Valida que se haya seleccionado un cliente y vehículo en la orden
        public static bool ValidarClienteVehiculoOrden(string clienteDNI, string vehiculoPlaca)
        {
            if (string.IsNullOrEmpty(clienteDNI) || string.IsNullOrEmpty(vehiculoPlaca))
            {
                MessageBox.Show("⚠ Busca un cliente o vehículo antes de guardar.",
                    "Datos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // Valida que la fecha de inicio no sea mayor a un año en el futuro
        public static bool ValidarFechaOrden(DateTime? fecha)
        {
            if (fecha.HasValue && fecha.Value > DateTime.Today.AddYears(1))
            {
                MessageBox.Show("⚠ La fecha de inicio no puede ser mayor a un año en el futuro.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // Valida que la fecha de entrega no sea anterior a la fecha de inicio
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

        // Valida que el precio de servicio sea un número válido (acepta vacío)
        public static bool ValidarPrecioServicio(string texto, out decimal precio)
        {
            precio = 0;
            string limpio = texto.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();
            if (!string.IsNullOrWhiteSpace(limpio) &&
                !decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out precio))
            {
                MessageBox.Show("⚠ El precio del servicio debe ser un número válido.",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            decimal.TryParse(limpio, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out precio);
            return true;
        }

        // Valida que la orden no sea de un mes anterior
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

        // Validar placa de vehículo
        public static bool ValidarPlaca(string placa)
        {
            string p = placa.Trim().ToUpper();

            if (string.IsNullOrWhiteSpace(p))
            {
                MessageBox.Show("⚠ La placa es obligatoria.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Solo letras y números
            if (!Regex.IsMatch(p, @"^[A-Z0-9]+$"))
            {
                MessageBox.Show("⚠ La placa solo puede contener letras y números.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Longitud razonable
            if (p.Length < 6)
            {
                MessageBox.Show("⚠ La placa debe tener 6 caracteres.",
                    "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

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

        // ✅ Valida que el año tenga exactamente 4 dígitos antes del parse
        public static bool ValidarAnioFormato(string texto)
        {
            if (!Regex.IsMatch(texto.Trim(), @"^\d{4}$"))
            {
                MessageBox.Show("⚠ El año debe tener exactamente 4 dígitos.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

    }
}