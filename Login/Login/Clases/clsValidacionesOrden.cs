using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Validaciones específicas para el formulario de Órdenes de Trabajo.
    /// Complementa a clsValidaciones con reglas propias de este módulo.
    /// </summary>
    public class clsValidacionesOrden
    {
        // ─────────────────────────────────────────────────────────────
        // BÚSQUEDA — DNI / PLACA
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el campo de búsqueda no esté vacío según el criterio elegido.
        /// </summary>
        public static bool ValidarCampoBusqueda(string valor, bool esDNI)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                string campo = esDNI ? "DNI del cliente" : "placa del vehículo";
                MessageBox.Show($"⚠ Ingresa el {campo} para realizar la búsqueda.",
                    "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que el DNI de búsqueda tenga exactamente 13 dígitos numéricos.
        /// </summary>
        public static bool ValidarFormatoDNIBusqueda(string dni)
        {
            if (!Regex.IsMatch(dni.Trim(), @"^\d{13}$"))
            {
                MessageBox.Show("⚠ El DNI debe contener exactamente 13 dígitos numéricos.\n" +
                                "Ejemplo: 0801199900123",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que la placa de búsqueda tenga un formato hondureño reconocido.
        /// </summary>
        public static bool ValidarFormatoPlacaBusqueda(string placa)
        {
            string p = placa.Trim().ToUpper();

            if (p.Length < 5 || p.Length > 8)
            {
                MessageBox.Show("⚠ La placa debe tener entre 5 y 8 caracteres.\n" +
                                "Ejemplos válidos: ABC1234 · AB1234 · A1234",
                    "Placa inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            bool formatoTurismo = Regex.IsMatch(p, @"^[A-Z]{3}\d{4}$");
            bool formatoMoto = Regex.IsMatch(p, @"^[A-Z]{1,2}\d{4}$");
            bool formatoCamion = Regex.IsMatch(p, @"^[A-Z]{1,3}\d{3,4}[A-Z]?$");

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

        // ─────────────────────────────────────────────────────────────
        // CLIENTE Y VEHÍCULO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que se haya encontrado y asignado un cliente antes de guardar la orden.
        /// </summary>
        public static bool ValidarClienteAsignado(string clienteDNI)
        {
            if (string.IsNullOrWhiteSpace(clienteDNI))
            {
                MessageBox.Show("⚠ Debes buscar y seleccionar un cliente antes de guardar la orden.\n\n" +
                                "Ingresa el DNI o la placa del vehículo y presiona 'Buscar'.",
                    "Cliente no asignado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que se haya encontrado y asignado un vehículo antes de guardar la orden.
        /// </summary>
        public static bool ValidarVehiculoAsignado(string vehiculoPlaca)
        {
            if (string.IsNullOrWhiteSpace(vehiculoPlaca))
            {
                MessageBox.Show("⚠ Debes buscar y seleccionar un vehículo antes de guardar la orden.\n\n" +
                                "El vehículo se asigna automáticamente al buscar por DNI o por placa.",
                    "Vehículo no asignado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // ESTADO DE LA ORDEN
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que se haya seleccionado un estado para la orden.
        /// </summary>
        public static bool ValidarEstadoOrden(object itemSeleccionado)
        {
            if (itemSeleccionado == null)
            {
                MessageBox.Show("⚠ Debes seleccionar el estado de la orden antes de guardar.\n\n" +
                                "Opciones: Sin Empezar · En Espera · En Proceso · Finalizado",
                    "Estado requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que una orden finalizada no vuelva a un estado anterior sin confirmación.
        /// </summary>
        public static bool ValidarCambioEstadoFinalizado(string estadoAnterior, string estadoNuevo)
        {
            if (estadoAnterior == "Finalizado" && estadoNuevo != "Finalizado")
            {
                var res = MessageBox.Show(
                    $"⚠ La orden estaba marcada como 'Finalizado'.\n" +
                    $"¿Deseas cambiarla a '{estadoNuevo}'?\n\n" +
                    "Esta acción reabrirá la orden.",
                    "Confirmar cambio de estado",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que la fecha de inicio no sea mayor a un año en el futuro.
        /// </summary>
        public static bool ValidarFechaInicio(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                MessageBox.Show("⚠ Debes seleccionar la fecha de inicio de la orden.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fecha.Value > DateTime.Today.AddYears(1))
            {
                MessageBox.Show("⚠ La fecha de inicio no puede ser mayor a un año en el futuro.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida que la fecha de entrega no sea anterior a la fecha de inicio.
        /// </summary>
        public static bool ValidarFechaEntrega(DateTime? fechaInicio, DateTime? fechaEntrega)
        {
            if (!fechaEntrega.HasValue) return true; // La entrega es opcional

            if (fechaInicio.HasValue && fechaEntrega.Value < fechaInicio.Value)
            {
                MessageBox.Show("⚠ La fecha de entrega no puede ser anterior a la fecha de inicio.\n\n" +
                                $"Fecha inicio:   {fechaInicio.Value:dd/MM/yyyy}\n" +
                                $"Fecha entrega:  {fechaEntrega.Value:dd/MM/yyyy}",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (fechaEntrega.Value > DateTime.Today.AddYears(2))
            {
                MessageBox.Show("⚠ La fecha de entrega no puede ser mayor a 2 años en el futuro.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Valida que la orden no sea de un mes anterior (para actualizaciones).
        /// </summary>
        public static bool ValidarMesActualizacion(DateTime? fecha)
        {
            if (fecha.HasValue)
            {
                var hoy = DateTime.Today;
                if (fecha.Value.Year < hoy.Year ||
                   (fecha.Value.Year == hoy.Year && fecha.Value.Month < hoy.Month))
                {
                    MessageBox.Show("⚠ No se pueden actualizar órdenes de meses anteriores.\n\n" +
                                    $"Fecha de la orden: {fecha.Value:MMMM yyyy}\n" +
                                    $"Mes actual:        {hoy:MMMM yyyy}",
                        "Operación no permitida", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // PRECIO DEL SERVICIO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida y extrae el precio del servicio desde el TextBox con formato "L 0.00".
        /// Acepta vacío o cero (el servicio puede ser gratuito).
        /// </summary>
        public static bool ValidarPrecioServicio(string texto, out decimal precio)
        {
            precio = 0;

            if (string.IsNullOrWhiteSpace(texto)) return true;

            // 1. Quitar la "L", espacios y comas por completo
            string limpio = texto.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();

            // 2. Manejar múltiples puntos (ej: 1.500.00 -> 1500.00)
            // Si hay más de un punto, quitamos todos menos el último
            int conteoPuntos = limpio.Count(f => f == '.');
            if (conteoPuntos > 1)
            {
                int ultimoPunto = limpio.LastIndexOf('.');
                // Quitamos todos los puntos y luego volvemos a poner el decimal al final
                string parteEntera = limpio.Substring(0, ultimoPunto).Replace(".", "");
                string parteDecimal = limpio.Substring(ultimoPunto);
                limpio = parteEntera + parteDecimal;
            }

            // 3. Intentar convertir (usando InvariantCulture para que el punto sea decimal)
            bool esValido = decimal.TryParse(limpio,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out precio);

            if (!esValido)
            {
                MessageBox.Show("⚠ El precio debe ser un número válido.\nEjemplo: 1500.50",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 4. Validaciones de rango
            if (precio < 0) return false;
            if (precio > 999999.99m) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // REPUESTOS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Advierte si no se agregó ningún repuesto a la orden (no bloquea, solo avisa).
        /// </summary>
        public static bool ConfirmarOrdenSinRepuestos(int cantidadRepuestos)
        {
            if (cantidadRepuestos == 0)
            {
                var res = MessageBox.Show(
                    "⚠ No has agregado ningún repuesto a esta orden.\n\n" +
                    "¿Deseas guardar la orden sin repuestos?",
                    "Sin repuestos",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                return res == MessageBoxResult.Yes;
            }
            return true;
        }

        /// <summary>
        /// Valida que al menos un repuesto esté marcado como incluido si existen repuestos.
        /// </summary>
        public static bool ValidarAlMenosUnRepuestoIncluido(
            System.Collections.Generic.IEnumerable<dynamic> repuestos)
        {
            bool hayAlgunIncluido = false;
            int total = 0;

            foreach (var r in repuestos)
            {
                total++;
                if (r.Incluido) { hayAlgunIncluido = true; break; }
            }

            if (total > 0 && !hayAlgunIncluido)
            {
                var res = MessageBox.Show(
                    "⚠ Tienes repuestos en la lista pero ninguno está marcado como incluido.\n\n" +
                    "¿Deseas continuar de todas formas?",
                    "Repuestos sin incluir",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                return res == MessageBoxResult.Yes;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // OBSERVACIONES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que las observaciones no superen el límite de caracteres.
        /// </summary>
        public static bool ValidarObservaciones(string texto)
        {
            if (!string.IsNullOrEmpty(texto) && texto.Length > 500)
            {
                MessageBox.Show("⚠ Las observaciones no pueden superar los 500 caracteres.\n\n" +
                                $"Caracteres actuales: {texto.Length} / 500",
                    "Texto demasiado largo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // FOTO
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el archivo de foto seleccionado exista y tenga extensión permitida.
        /// </summary>
        public static bool ValidarFoto(string rutaFoto)
        {
            if (string.IsNullOrEmpty(rutaFoto)) return true; // La foto es opcional

            if (!System.IO.File.Exists(rutaFoto))
            {
                MessageBox.Show("⚠ El archivo de foto seleccionado ya no existe en la ruta indicada.\n\n" +
                                $"Ruta: {rutaFoto}",
                    "Foto no encontrada", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            string ext = System.IO.Path.GetExtension(rutaFoto).ToLower();
            string[] extensionesPermitidas = { ".jpg", ".jpeg", ".png", ".bmp" };

            bool extensionValida = false;
            foreach (string e in extensionesPermitidas)
                if (ext == e) { extensionValida = true; break; }

            if (!extensionValida)
            {
                MessageBox.Show("⚠ El archivo de foto debe ser una imagen válida.\n\n" +
                                "Formatos permitidos: JPG · JPEG · PNG · BMP",
                    "Formato no permitido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            long tamañoBytes = new System.IO.FileInfo(rutaFoto).Length;
            if (tamañoBytes > 5 * 1024 * 1024) // 5 MB
            {
                MessageBox.Show("⚠ La imagen no puede superar los 5 MB.\n\n" +
                                $"Tamaño actual: {tamañoBytes / 1024 / 1024:N1} MB",
                    "Imagen demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA — AÑADIR ORDEN
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Ejecuta todas las validaciones necesarias para GUARDAR una nueva orden.
        /// Retorna false en el primer error encontrado.
        /// </summary>
        public static bool ValidarFormularioAñadir(
            string clienteDNI,
            string vehiculoPlaca,
            object estadoSeleccionado,
            DateTime? fechaInicio,
            DateTime? fechaEntrega,
            string precioServicioTexto,
            string observaciones,
            string rutaFoto,
            int cantidadRepuestos,
            out decimal precioServicio)
        {
            precioServicio = 0;

            // — Cliente y vehículo —
            if (!ValidarClienteAsignado(clienteDNI)) return false;
            if (!ValidarVehiculoAsignado(vehiculoPlaca)) return false;

            // — Estado —
            if (!ValidarEstadoOrden(estadoSeleccionado)) return false;

            // — Fechas —
            if (!ValidarFechaInicio(fechaInicio)) return false;
            if (!ValidarFechaEntrega(fechaInicio, fechaEntrega)) return false;

            // — Precio servicio —
            if (!ValidarPrecioServicio(precioServicioTexto, out precioServicio)) return false;

            // — Observaciones —
            if (!ValidarObservaciones(observaciones)) return false;

            // — Foto —
            if (!ValidarFoto(rutaFoto)) return false;

            // — Repuestos (advertencia, no bloquea si el usuario confirma) —
            if (!ConfirmarOrdenSinRepuestos(cantidadRepuestos)) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA — ACTUALIZAR ORDEN
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Ejecuta todas las validaciones necesarias para ACTUALIZAR una orden existente.
        /// Retorna false en el primer error encontrado.
        /// </summary>
        public static bool ValidarFormularioActualizar(
            DateTime? fechaInicio,
            DateTime? fechaEntrega,
            string precioServicioTexto,
            string observaciones,
            string rutaFoto,
            out decimal precioServicio)
        {
            precioServicio = 0;

            // — Mes anterior (no se puede editar) —
            if (!ValidarMesActualizacion(fechaInicio)) return false;

            // — Fechas —
            if (!ValidarFechaEntrega(fechaInicio, fechaEntrega)) return false;

            // — Precio servicio —
            if (!ValidarPrecioServicio(precioServicioTexto, out precioServicio)) return false;

            // — Observaciones —
            if (!ValidarObservaciones(observaciones)) return false;

            // — Foto —
            if (!ValidarFoto(rutaFoto)) return false;

            return true;
        }
    }
}