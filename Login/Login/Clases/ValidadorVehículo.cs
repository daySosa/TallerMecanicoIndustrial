using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    public class ValidadorVehículo
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(params string[] campos)
        {
            return ValidacionesGenerales.ValidarFormularioVacio(campos);
        }

        // ─────────────────────────────────────────────────────────────
        // PLACA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPlacaNoNula(string placa)
        {
            return ValidacionesGenerales.ValidarTextoRequerido(placa, "placa del vehículo");
        }

        public static bool ValidarPlacaSoloAlfanumerico(string placa)
        {
            return ValidacionesGenerales.ValidarPlaca(placa);
        }

        public static bool ValidarLongitudPlaca(string placa)
        {
            if (!ValidacionesGenerales.ValidarLongitudMaxima(placa.Trim(), 7, "placa"))
                return false;

            if (placa.Trim().Length < 6)
            {
                MessageBox.Show(
                    $"⚠ La placa \"{placa.Trim()}\" tiene {placa.Trim().Length} caracter(es); " +
                    "debe tener entre 6 y 7.\n\n" +
                    "  • Moto:   6 caracteres (ej: GBA123)\n" +
                    "  • Carro:  7 caracteres (ej: GHA1234)\n" +
                    "  • Camión: 7 caracteres (ej: GCA123A)",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarFormatoPlacaSegunTipo(string placa, string tipo)
        {
            string p = placa.Trim().ToUpper();

            bool formatoCarro = Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$");
            bool formatoMoto = Regex.IsMatch(p, @"^[A-Z]{2}\d{4}$");
            bool formatoCamion = Regex.IsMatch(p, @"^[A-Z]{3}\d{3}[A-Z]$");

            if (string.IsNullOrWhiteSpace(tipo))
            {
                if (formatoCarro || formatoMoto || formatoCamion) return true;

                string msg = p.Length == 6
                    ? $"⚠ \"{p}\" no coincide con el formato de placa de motocicleta.\n\nFormato válido:\n  • 2 letras + 4 dígitos: GBA1234"
                    : p.Length == 7 && char.IsDigit(p[6])
                        ? $"⚠ \"{p}\" no coincide con el formato de placa de carro.\n\nFormato válido:\n  • 3 letras + 4 dígitos: GHA1234"
                        : p.Length == 7 && char.IsLetter(p[6])
                            ? $"⚠ \"{p}\" no coincide con el formato de placa de camión.\n\nFormato válido:\n  • 3 letras + 3 dígitos + 1 letra: GCA123A"
                            : $"⚠ \"{p}\" no coincide con ningún formato de placa reconocido.\n\nFormatos válidos en Honduras:\n" +
                              "  • Carro:  3 letras + 4 dígitos:           GHA1234\n" +
                              "  • Moto:   2 letras + 4 dígitos:           GBA1234\n" +
                              "  • Camión: 3 letras + 3 dígitos + 1 letra: GCA123A";

                MessageBox.Show(msg, "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            switch (tipo)
            {
                case "Turismo":
                case "Pickup":
                case "Camioneta":
                    if (formatoCarro) return true;
                    MessageBox.Show(
                        $"⚠ \"{p}\" no es una placa válida para tipo {tipo}.\n\nFormato válido:\n  • 3 letras + 4 dígitos: GHA1234",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;

                case "Motocicleta":
                case "MotoTaxi":
                    if (formatoMoto) return true;
                    MessageBox.Show(
                        $"⚠ \"{p}\" no es una placa válida para tipo {tipo}.\n\nFormato válido:\n  • 2 letras + 4 dígitos: GBA1234",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;

                case "Camión":
                    if (formatoCamion) return true;
                    MessageBox.Show(
                        $"⚠ \"{p}\" no es una placa válida para tipo {tipo}.\n\nFormato válido:\n  • 3 letras + 3 dígitos + 1 letra: GCA123A",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;

                default:
                    if (formatoCarro || formatoMoto || formatoCamion) return true;
                    MessageBox.Show(
                        $"⚠ \"{p}\" no coincide con ningún formato de placa reconocido.\n\nFormatos válidos en Honduras:\n" +
                        "  • Carro:  3 letras + 4 dígitos:           GHA1234\n" +
                        "  • Moto:   2 letras + 4 dígitos:           GBA1234\n" +
                        "  • Camión: 3 letras + 3 dígitos + 1 letra: GCA123A",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
            }
        }

        public static bool ValidarPlacaNoReservada(string placa)
        {
            string[] reservadas = { "POLICIA", "EJERCITO", "FUERZAS", "TEST", "XXXXX" };
            string p = placa.Trim().ToUpper();

            foreach (string r in reservadas)
            {
                if (p.Contains(r))
                {
                    MessageBox.Show(
                        $"⚠ La placa \"{p}\" no puede registrarse porque contiene \"{r}\", " +
                        "una palabra reservada para uso oficial.\n\n" +
                        "Verifica que hayas digitado la placa correctamente.",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        public static bool ValidarPlacaNoDuplicada(string placa, Func<string, bool> existeEnBD)
        {
            string p = placa.Trim().ToUpper();
            if (existeEnBD(p))
            {
                MessageBox.Show(
                    $"⚠ Ya existe un vehículo registrado con la placa \"{p}\".\n\n" +
                    "Usa el buscador para localizarlo en vez de crear un registro nuevo, " +
                    "o revisa que no hayas transpuesto algún carácter.",
                    "Placa duplicada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }


        // ─────────────────────────────────────────────────────────────
        // MARCA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarMarca(string marca)
        {
            return ValidacionesGenerales.ValidarTextoRequerido(marca, "marca del vehículo")
                && ValidacionesGenerales.ValidarNoEsSoloNumeros(marca.Trim(), "marca")
                && ValidacionesGenerales.ValidarIniciaConLetra(marca.Trim(), "marca")
                && ValidacionesGenerales.ValidarSinRepeticionExcesiva(marca.Trim(), "marca")
                && ValidacionesGenerales.ValidarLongitudMaxima(marca.Trim(), 50, "marca");
        }

        // ─────────────────────────────────────────────────────────────
        // MODELO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarModelo(string modelo)
        {
            return ValidacionesGenerales.ValidarTextoRequerido(modelo, "modelo del vehículo")
                && ValidacionesGenerales.ValidarNoEsSoloNumeros(modelo.Trim(), "modelo")
                && ValidacionesGenerales.ValidarIniciaConLetra(modelo.Trim(), "modelo")
                && ValidacionesGenerales.ValidarSinRepeticionExcesiva(modelo.Trim(), "modelo")
                && ValidacionesGenerales.ValidarLongitudMaxima(modelo.Trim(), 80, "modelo");
        }

        // ─────────────────────────────────────────────────────────────
        // AÑO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarAnioVehiculo(string texto, out int año)
        {
            año = 0;
            return ValidacionesGenerales.ValidarTextoRequerido(texto, "año del vehículo")
                && ValidacionesGenerales.ValidarAnioFormato(texto)
                && ValidacionesGenerales.ValidarAnio(texto, out año);
        }

        // ─────────────────────────────────────────────────────────────
        // TIPO DE VEHÍCULO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarTipoVehiculo(object itemSeleccionado)
        {
            return ValidacionesGenerales.ValidarComboSeleccionado(itemSeleccionado, "tipo de vehículo");
        }

        // ─────────────────────────────────────────────────────────────
        // OBSERVACIONES
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;
            return ValidacionesGenerales.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")
                && ValidacionesGenerales.ValidarNoEsSoloNumeros(texto.Trim(), "observaciones")
                && ValidacionesGenerales.ValidarLongitudMaxima(texto.Trim(), 500, "observaciones");
        }

        // ─────────────────────────────────────────────────────────────
        // CLIENTE / DNI
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteDNI(string textoDNI, string clienteVerificado)
        {
            return ValidacionesGenerales.ValidarClienteDNI(textoDNI, clienteVerificado);
        }

        public static bool ValidarFormatoDNICliente(string dni)
        {
            return ValidacionesGenerales.ValidarFormatoDNI(dni);
        }
    }
}