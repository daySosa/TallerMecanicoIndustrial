using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    public class clsValidacionesVehiculo
    {
        // ─────────────────────────────────────────────────────────────
        // PLACA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPlacaNoNula(string placa)
        {
            return clsValidaciones.ValidarTextoRequerido(placa, "placa del vehículo");
        }

        public static bool ValidarPlacaSoloAlfanumerico(string placa)
        {
            if (!Regex.IsMatch(placa.Trim(), @"^[A-Za-z0-9]+$"))
            {
                MessageBox.Show("⚠ La placa solo puede contener letras y números, sin espacios ni caracteres especiales.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarLongitudPlaca(string placa)
        {
            string p = placa.Trim().ToUpper();
            if (p.Length < 6 || p.Length > 7)
            {
                MessageBox.Show("⚠ La placa debe tener entre 6 y 7 caracteres (ej: ABC1234 o AB1234).",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarFormatoPlacaHondureña(string placa)
        {
            string p = placa.Trim().ToUpper();

            bool formatoTurismo = Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$");
            bool formatoMoto = Regex.IsMatch(p, @"^[A-Z]{1,2}\d{4}$");
            bool formatoCamion = Regex.IsMatch(p, @"^[A-Z]{1,3}\d{3,4}[A-Z]$");

            if (formatoTurismo || formatoMoto || formatoCamion)
                return true;

            int letrasIniciales = 0, digitos = 0, letrasFinales = 0;
            bool enDigitos = false, enLetrasFinales = false;

            foreach (char c in p)
            {
                if (char.IsLetter(c) && !enDigitos) letrasIniciales++;
                else if (char.IsDigit(c)) { enDigitos = true; digitos++; }
                else if (char.IsLetter(c)) { enLetrasFinales = true; letrasFinales++; }
            }

            if (letrasIniciales == 3 && !enLetrasFinales)
            {
                MessageBox.Show(
                    "⚠ Formato de placa incorrecto para Turismo / Pickup / Camioneta.\n\n" +
                    "El formato correcto es:  ABC1234\n" +
                    "  • 3 letras seguidas de 4 dígitos, sin espacios.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (letrasIniciales <= 2 && !enLetrasFinales)
            {
                MessageBox.Show(
                    "⚠ Formato de placa incorrecto para Motocicleta / MotoTaxi.\n\n" +
                    "Los formatos correctos son:\n" +
                    "  • A1234   (1 letra + 4 dígitos)\n" +
                    "  • AB1234  (2 letras + 4 dígitos)",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (enLetrasFinales)
            {
                MessageBox.Show(
                    "⚠ Formato de placa incorrecto para Camión / vehículo especial.\n\n" +
                    "El formato correcto es:  ABC1234A\n" +
                    "  • 1 a 3 letras + 3 o 4 dígitos + 1 letra al final.",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show(
                "⚠ Formato de placa no reconocido.\n\n" +
                "Formatos válidos en Honduras:\n" +
                "  • Turismo / Pickup / Camioneta:  ABC1234\n" +
                "  • Motocicleta / MotoTaxi:        A1234  o  AB1234\n" +
                "  • Camión / especiales:           ABC1234A",
                "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        public static bool ValidarPlacaNoReservada(string placa)
        {
            string[] reservadas = { "POLICIA", "EJERCITO", "FUERZAS", "TEST", "XXXXX" };
            string p = placa.Trim().ToUpper();

            foreach (string r in reservadas)
            {
                if (p.Contains(r))
                {
                    MessageBox.Show($"⚠ La placa '{p}' contiene una combinación reservada o no permitida.",
                        "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        public static bool ValidarPlacaNoDuplicada(string placa, Func<string, bool> existeEnBD)
        {
            if (existeEnBD(placa.Trim().ToUpper()))
            {
                MessageBox.Show($"⚠ La placa '{placa.Trim().ToUpper()}' ya está registrada en el sistema.",
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
            if (!clsValidaciones.ValidarTextoRequerido(marca, "marca del vehículo")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(marca, "marca")) return false;
            if (!clsValidaciones.ValidarIniciaConLetra(marca.Trim(), "marca")) return false;
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(marca.Trim(), "marca")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(marca, 50, "marca")) return false;
            return true;
        }

        // Redirige a clsValidaciones por compatibilidad con llamadas existentes
        public static bool ValidarMarcaSinRepeticion(string marca)
            => clsValidaciones.ValidarSinRepeticionExcesiva(marca, "marca");

        // ─────────────────────────────────────────────────────────────
        // MODELO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarModelo(string modelo)
        {
            if (!clsValidaciones.ValidarTextoRequerido(modelo, "modelo del vehículo")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(modelo, "modelo")) return false;
            if (!clsValidaciones.ValidarIniciaConLetra(modelo.Trim(), "modelo")) return false;
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(modelo.Trim(), "modelo")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(modelo, 80, "modelo")) return false;

            if (!Regex.IsMatch(modelo.Trim(), @"^[a-zA-Z0-9\s\-\.\(\)\/]+$"))
            {
                MessageBox.Show("⚠ El modelo contiene caracteres no permitidos.",
                    "Modelo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarModeloSinRepeticion(string modelo)
            => clsValidaciones.ValidarSinRepeticionExcesiva(modelo, "modelo");

        // ─────────────────────────────────────────────────────────────
        // AÑO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarAnioVehiculo(string texto, out int año)
        {
            año = 0;
            if (!clsValidaciones.ValidarTextoRequerido(texto, "año del vehículo")) return false;
            if (!clsValidaciones.ValidarAnioFormato(texto)) return false;
            if (!clsValidaciones.ValidarAnio(texto, out año)) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // TIPO DE VEHÍCULO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarTipoVehiculo(object itemSeleccionado)
        {
            return clsValidaciones.ValidarComboSeleccionado(itemSeleccionado, "tipo de vehículo");
        }

        public static bool ValidarCoherenciaPlacaTipo(string placa, string tipo)
        {
            string p = placa.Trim().ToUpper();
            bool esMoto = tipo == "Motocicleta" || tipo == "MotoTaxi";
            bool esTurismo = tipo == "Turismo" || tipo == "Pickup" || tipo == "Camioneta";
            bool esCamion = tipo == "Camiones";

            if (esTurismo && Regex.IsMatch(p, @"^[A-Z]{1,2}\d{4}$"))
            {
                MessageBox.Show(
                    $"⚠ La placa '{p}' tiene formato de motocicleta (1-2 letras + 4 dígitos)\n" +
                    $"pero el tipo seleccionado es '{tipo}'.\n\n" +
                    "Corrige la placa o el tipo de vehículo antes de continuar.",
                    "Error de coherencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (esMoto && Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$"))
            {
                MessageBox.Show(
                    $"⚠ La placa '{p}' tiene formato de turismo/pickup/camioneta (3 letras + 4 dígitos)\n" +
                    $"pero el tipo seleccionado es '{tipo}'.\n\n" +
                    "Corrige la placa o el tipo de vehículo antes de continuar.",
                    "Error de coherencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (esCamion && !Regex.IsMatch(p, @"^[A-Z]{1,3}\d{3,4}[A-Z]$"))
            {
                MessageBox.Show(
                    $"⚠ La placa '{p}' no tiene formato de camión.\n\n" +
                    "El formato correcto es: ABC1234A\n" +
                    "  • 1 a 3 letras + 3 o 4 dígitos + 1 letra al final.",
                    "Error de coherencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // OBSERVACIONES
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarObservaciones(string texto)
        {
            return clsValidaciones.ValidarLongitudMaxima(texto, 500, "observaciones");
        }

        // ─────────────────────────────────────────────────────────────
        // CLIENTE / DNI
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteVerificado(string clienteDniVerificado)
        {
            if (string.IsNullOrWhiteSpace(clienteDniVerificado))
            {
                MessageBox.Show("⚠ Debes buscar y verificar el DNI del cliente antes de guardar.",
                    "Cliente no verificado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarClienteDNI(string dni)
        {
            if (!clsValidaciones.ValidarDNIHondureño(dni)) return false;
            if (!clsValidaciones.DNI(dni)) return false;
            return true;
        }
    }
}