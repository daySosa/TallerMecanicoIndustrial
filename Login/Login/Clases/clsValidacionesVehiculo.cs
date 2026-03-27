using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Validaciones específicas del formulario de vehículos.
    /// </summary>
    public class clsValidacionesVehiculo
    {
        // ─────────────────────────────────────────────────────────────
        // PLACA
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que la placa solo contenga letras y números (sin espacios ni especiales).
        /// </summary>
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

        /// <summary>
        /// Valida que la placa tenga entre 6 y 7 caracteres (estándar Honduras).
        /// </summary>
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

        /// <summary>
        /// Valida el formato de placa hondureña: 3 letras + 4 dígitos (ABC1234)
        /// o 2 letras + 4 dígitos (AB1234) para motocicletas/especiales.
        /// </summary>
        public static bool ValidarFormatoPlacaHondureña(string placa)
        {
            string p = placa.Trim().ToUpper();

            bool formatoAuto = Regex.IsMatch(p, @"^[A-Z]{2,3}\d{4}$");
            bool formatoMoto = Regex.IsMatch(p, @"^[A-Z]{1,2}\d{4}$");
            bool formatoGov = Regex.IsMatch(p, @"^[A-Z]{1,3}\d{3,4}[A-Z]?$");

            if (!formatoAuto && !formatoMoto && !formatoGov)
            {
                MessageBox.Show(
                    "⚠ Formato de placa no reconocido.\n" +
                    "Formatos aceptados:\n" +
                    "  • Turismo/Pickup: ABC1234\n" +
                    "  • Motocicleta:   AB1234\n" +
                    "  • Gubernamental: AB1234G",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que la placa no contenga palabras ofensivas o reservadas.
        /// </summary>
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

        // ─────────────────────────────────────────────────────────────
        // MARCA
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que la marca no esté vacía, no supere 50 caracteres y sea alfanumérica.
        /// </summary>
        public static bool ValidarMarca(string marca)
        {
            if (string.IsNullOrWhiteSpace(marca))
            {
                MessageBox.Show("⚠ La marca del vehículo es obligatoria.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (marca.Trim().Length > 50)
            {
                MessageBox.Show("⚠ La marca no puede superar los 50 caracteres.",
                    "Marca inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(marca.Trim(), @"^[a-zA-Z0-9\s\-\.]+$"))
            {
                MessageBox.Show("⚠ La marca solo puede contener letras, números, espacios, guiones y puntos.",
                    "Marca inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // MODELO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el modelo no esté vacío, no supere 80 caracteres y sea alfanumérico.
        /// </summary>
        public static bool ValidarModelo(string modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo))
            {
                MessageBox.Show("⚠ El modelo del vehículo es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (modelo.Trim().Length > 80)
            {
                MessageBox.Show("⚠ El modelo no puede superar los 80 caracteres.",
                    "Modelo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

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

        /// <summary>
        /// Valida el año con reglas extendidas: formato, rango y coherencia con tipo de vehículo.
        /// </summary>
        public static bool ValidarAnioVehiculo(string texto, out int año)
        {
            año = 0;

            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("⚠ El año del vehículo es obligatorio.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!Regex.IsMatch(texto.Trim(), @"^\d{4}$"))
            {
                MessageBox.Show("⚠ El año debe ser un número de exactamente 4 dígitos.",
                    "Año inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

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

        /// <summary>
        /// Valida que se haya seleccionado un tipo de vehículo del ComboBox.
        /// </summary>
        public static bool ValidarTipoVehiculo(object itemSeleccionado)
        {
            if (itemSeleccionado == null)
            {
                MessageBox.Show("⚠ Debes seleccionar el tipo de vehículo.",
                    "Tipo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que el tipo de vehículo sea coherente con el formato de la placa ingresada.
        /// Ejemplo: motocicleta suele tener placa de 2 letras + 4 dígitos.
        /// </summary>
        public static bool ValidarCoherenciaPlacaTipo(string placa, string tipo)
        {
            string p = placa.Trim().ToUpper();
            bool esMoto = tipo == "Motocicleta" || tipo == "MotoTaxi";

            if (esMoto && Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$"))
            {
                var res = MessageBox.Show(
                    $"⚠ La placa '{p}' (3 letras + 4 dígitos) corresponde normalmente a un vehículo de turismo.\n" +
                    "¿Deseas continuar de todas formas?",
                    "Advertencia de coherencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }

            if (!esMoto && Regex.IsMatch(p, @"^[A-Z]{1}\d{4}$"))
            {
                var res = MessageBox.Show(
                    $"⚠ La placa '{p}' (1 letra + 4 dígitos) corresponde normalmente a una motocicleta.\n" +
                    "¿Deseas continuar de todas formas?",
                    "Advertencia de coherencia", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // OBSERVACIONES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que las observaciones no superen el límite de caracteres (opcional).
        /// </summary>
        public static bool ValidarObservaciones(string texto)
        {
            if (!string.IsNullOrEmpty(texto) && texto.Length > 500)
            {
                MessageBox.Show("⚠ Las observaciones no pueden superar los 500 caracteres.",
                    "Texto demasiado largo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CLIENTE / DNI
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el DNI haya sido verificado en BD antes de guardar.
        /// </summary>
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

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA DEL FORMULARIO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Ejecuta todas las validaciones del formulario en orden.
        /// Retorna false en el primer error encontrado.
        /// </summary>
        public static bool ValidarFormularioCompleto(
            string placa,
            string marca,
            string modelo,
            string anioTexto,
            object tipoSeleccionado,
            string observaciones,
            string clienteDniVerificado,
            out int año)
        {
            año = 0;

            // — Placa —
            if (!ValidarPlacaSoloAlfanumerico(placa)) return false;
            if (!ValidarLongitudPlaca(placa)) return false;
            if (!ValidarFormatoPlacaHondureña(placa)) return false;
            if (!ValidarPlacaNoReservada(placa)) return false;

            // — Marca —
            if (!ValidarMarca(marca)) return false;

            // — Modelo —
            if (!ValidarModelo(modelo)) return false;

            // — Año —
            if (!ValidarAnioVehiculo(anioTexto, out año)) return false;

            // — Tipo —
            if (!ValidarTipoVehiculo(tipoSeleccionado)) return false;

            // — Coherencia placa-tipo (advertencia, no bloquea si el usuario acepta) —
            string tipoStr = (tipoSeleccionado as System.Windows.Controls.ComboBoxItem)
                             ?.Content?.ToString() ?? string.Empty;
            if (!ValidarCoherenciaPlacaTipo(placa, tipoStr)) return false;

            // — Observaciones —
            if (!ValidarObservaciones(observaciones)) return false;

            // — Cliente verificado —
            if (!ValidarClienteVerificado(clienteDniVerificado)) return false;

            return true;
        }
    }
}