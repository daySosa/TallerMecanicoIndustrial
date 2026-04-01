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

            if (!formatoTurismo && !formatoMoto && !formatoCamion)
            {
                MessageBox.Show(
                    "⚠ Formato de placa no reconocido.\n\n" +
                    "Formatos válidos en Honduras:\n" +
                    "  • Turismo / Pickup / Camioneta:  ABC1234\n" +
                    "  • Motocicleta / MotoTaxi:        A1234  o  AB1234\n" +
                    "  • Camiones / especiales:         ABC1234A",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
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
            if (!clsValidaciones.ValidarTextoRequerido(marca, "marca del vehículo"))
                return false;

            if (!clsValidaciones.ValidarIniciaConLetra(marca.Trim(), "marca"))
                return false;

            if (!clsValidaciones.ValidarLongitudMaxima(marca.Trim(), 50, "marca"))
                return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // MODELO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarModelo(string modelo)
        {
            if (!clsValidaciones.ValidarTextoRequerido(modelo, "modelo del vehículo"))
                return false;

            if (!clsValidaciones.ValidarIniciaConLetra(modelo.Trim(), "modelo"))
                return false;

            if (!clsValidaciones.ValidarLongitudMaxima(modelo.Trim(), 80, "modelo"))
                return false;

            if (!Regex.IsMatch(modelo.Trim(), @"^[a-zA-Z0-9\s\-\.\(\)\/]+$"))
            {
                MessageBox.Show("⚠ El modelo contiene caracteres no permitidos.",
                    "Modelo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // AÑO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarAnioVehiculo(string texto, out int año)
        {
            año = 0;

            if (!clsValidaciones.ValidarTextoRequerido(texto, "año del vehículo"))
                return false;

            if (!clsValidaciones.ValidarAnioFormato(texto))
                return false;

            if (!int.TryParse(texto.Trim(), out año))
            {
                MessageBox.Show("⚠ El año ingresado no es válido.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int añoActual = DateTime.Now.Year;

            if (año < 1900)
            {
                MessageBox.Show("⚠ El año no puede ser anterior a 1900.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (año > añoActual + 1)
            {
                MessageBox.Show($"⚠ El año no puede ser mayor a {añoActual + 1}.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
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

            if (!esMoto && Regex.IsMatch(p, @"^[A-Z]{1,2}\d{4}$"))
            {
                var res = MessageBox.Show(
                    $"⚠ La placa '{p}' tiene formato de motocicleta (1-2 letras + 4 dígitos)\n" +
                    $"pero el tipo seleccionado es '{tipo}'.\n\n¿Deseas continuar de todas formas?",
                    "Advertencia de coherencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }

            if (esMoto && Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$"))
            {
                var res = MessageBox.Show(
                    $"⚠ La placa '{p}' tiene formato de turismo/pickup (3 letras + 4 dígitos)\n" +
                    $"pero el tipo seleccionado es '{tipo}'.\n\n¿Deseas continuar de todas formas?",
                    "Advertencia de coherencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // OBSERVACIONES
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return true;

            if (!clsValidaciones.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(texto, 500, "observaciones")) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CLIENTE / DNI
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteDNI(string dni)
        {
            return clsValidaciones.ValidarClienteEncontrado(dni);
        }

        public static bool ValidarPlacaRequerida(string placa)
        {
            return clsValidaciones.ValidarTextoRequerido(placa, "placa del vehículo");
        }
    }
}