using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Login.Clases
{
    public class clsValidaciones
    {
        public clsValidaciones() { }

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
            if (!Regex.IsMatch(texto.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ]"))
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
            if (Regex.IsMatch(texto.Trim(), @"(.)\1{4,}"))
            {
                MessageBox.Show($"⚠ El {nombreCampo} contiene caracteres repetidos excesivamente.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarTextoConCaracteresPermitidos(string texto, string nombreCampo)
        {
            if (!Regex.IsMatch(texto.Trim(), @"^[a-zA-ZáéíóúÁÉÍÓÚñÑüÜ0-9\s\-\.\(\)\/]+$"))
            {
                MessageBox.Show($"⚠ El {nombreCampo} contiene caracteres no permitidos.",
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

            if (!Regex.IsMatch(correo, @"^[a-z0-9][a-z0-9._%+\-]*@[a-z0-9.\-]+\.[a-z]{2,}$"))
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
            if (!Regex.IsMatch(p, @"^[A-Z0-9]+$"))
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
            if (!Regex.IsMatch(texto.Trim(), @"^\d{4}$"))
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
            if (!Regex.IsMatch(dni.Trim(), @"^\d{13}$"))
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
        // LOGIN
        // ─────────────────────────────────────────────────────────────

        public static string ValidarContrasenaLogin(string contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena)) return "⚠ La contraseña es obligatoria.";
            if (contrasena.Contains(" ")) return "⚠ La contraseña no puede contener espacios.";
            if (contrasena.Length > 50) return "⚠ La contraseña es demasiado larga (máximo 50 caracteres).";
            return null;
        }
    }
}