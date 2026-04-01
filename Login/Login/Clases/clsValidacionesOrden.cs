using System.Text.RegularExpressions;
using System.Windows;

namespace Login.Clases
{
    public class clsValidacionesOrden
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(string clienteDNI, string vehiculoPlaca, string precio)
        {
            string precioLimpio = precio.Replace("L", "").Replace("0.00", "")
                                        .Replace(",", "").Replace(" ", "").Trim();

            return clsValidaciones.ValidarFormularioVacio(clienteDNI, vehiculoPlaca, precioLimpio);
        }

        // ─────────────────────────────────────────────────────────────
        // BÚSQUEDA — DNI / PLACA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarCampoBusqueda(string valor, bool esDNI)
        {
            string campo = esDNI ? "DNI del cliente" : "placa del vehículo";
            return clsValidaciones.ValidarTextoRequerido(valor,
                $"⚠ Ingresa el {campo} para realizar la búsqueda.",
                msg => MessageBox.Show(msg, "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        public static bool ValidarFormatoDNIBusqueda(string dni)
        {
            return clsValidaciones.ValidarFormatoDNI(dni);
        }

        public static bool ValidarFormatoPlacaBusqueda(string placa)
        {
            string p = placa.Trim().ToUpper();

            return clsValidacionesVehiculo.ValidarFormatoPlacaSegunTipo(p, string.Empty);
        }

        public static bool EsCaracterValidoDNI(string texto)
            => texto.All(char.IsDigit);

        public static bool EsCaracterValidoPlaca(string texto)
            => texto.All(char.IsLetterOrDigit);

        // ─────────────────────────────────────────────────────────────
        // CLIENTE Y VEHÍCULO
        // ─────────────────────────────────────────────────────────────

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

        public static bool ValidarEstadoOrden(object itemSeleccionado)
        {
            return clsValidaciones.ValidarComboSeleccionado(itemSeleccionado, "estado de la orden");
        }

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
        // PRIORIDAD
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPrioridad(object itemSeleccionado)
        {
            return clsValidaciones.ValidarComboSeleccionado(itemSeleccionado, "prioridad de la orden");
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFechaInicio(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                MessageBox.Show("⚠ Debes seleccionar la fecha de inicio de la orden.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var hoy = DateTime.Today;

            if (fecha.Value.Date != hoy)
            {
                MessageBox.Show(
                    $"⚠ La fecha de inicio debe ser el día de hoy ({hoy:dd/MM/yyyy}).",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static bool ValidarFechaEntrega(DateTime? fechaInicio, DateTime? fechaEntrega)
        {
            if (!fechaEntrega.HasValue)
            {
                MessageBox.Show("⚠ Debes seleccionar una fecha de entrega estimada.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.ValidarFechaEntrega(fechaInicio, fechaEntrega))
                return false;

            if (fechaEntrega.Value.Date > DateTime.Today.AddYears(2))
            {
                MessageBox.Show("⚠ La fecha de entrega no puede ser mayor a 2 años en el futuro.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static bool ValidarMesActualizacion(DateTime? fecha)
        {
            return clsValidaciones.ValidarMesOrden(fecha);
        }

        // ─────────────────────────────────────────────────────────────
        // PRECIO DEL SERVICIO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarPrecioServicio(string texto, out decimal precio)
        {
            precio = 0;
            string limpio = texto.Replace("L", "").Replace(",", "").Replace(" ", "").Trim();

            if (string.IsNullOrWhiteSpace(limpio) || limpio == "0" || limpio == "0.00")
            {
                MessageBox.Show("⚠ El precio del servicio es obligatorio y debe ser mayor a 0.",
                    "Precio requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            int conteoPuntos = limpio.Count(f => f == '.');
            if (conteoPuntos > 1)
            {
                int ultimoPunto = limpio.LastIndexOf('.');
                string parteEntera = limpio.Substring(0, ultimoPunto).Replace(".", "");
                string parteDecimal = limpio.Substring(ultimoPunto);
                limpio = parteEntera + parteDecimal;
            }

            if (!decimal.TryParse(limpio,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out precio) || precio <= 0)
            {
                MessageBox.Show("⚠ El precio del servicio debe ser un número mayor a 0.\nEjemplo: 1500.50",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (precio > 999_999.99m)
            {
                MessageBox.Show("⚠ El precio del servicio supera el límite permitido (L 999,999.99).",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // REPUESTOS
        // ─────────────────────────────────────────────────────────────

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

        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return true;

            if (!clsValidaciones.ValidarIniciaConLetra(texto.Trim(), "observaciones")) return false;    
            if (!clsValidaciones.ValidarNoEsSoloNumeros(texto.Trim(), "observaciones")) return false;   
            if (!clsValidaciones.ValidarTextoConCaracteresPermitidos(texto.Trim(), "observaciones")) return false; 
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(texto, 500, "observaciones")) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // FOTO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFoto(string rutaFoto)
        {
            if (string.IsNullOrEmpty(rutaFoto)) return true;

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
            if (tamañoBytes > 5 * 1024 * 1024)
            {
                MessageBox.Show("⚠ La imagen no puede superar los 5 MB.\n\n" +
                                $"Tamaño actual: {tamañoBytes / 1024 / 1024:N1} MB",
                    "Imagen demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA 
        // ─────────────────────────────────────────────────────────────

        private static bool ValidarFormulario(
            string clienteDNI,
            string vehiculoPlaca,
            object estadoSeleccionado,
            object prioridadSeleccionada,
            DateTime? fechaInicio,
            DateTime? fechaEntrega,
            string precioServicioTexto,
            string observaciones,
            string rutaFoto,
            int cantidadRepuestos,
            out decimal precioServicio)
        {
            precioServicio = 0;

            if (!ValidarClienteAsignado(clienteDNI)) return false;
            if (!ValidarVehiculoAsignado(vehiculoPlaca)) return false;
            if (!ValidarEstadoOrden(estadoSeleccionado)) return false;
            if (!ValidarPrioridad(prioridadSeleccionada)) return false;
            if (!ValidarFechaInicio(fechaInicio)) return false;
            if (!ValidarFechaEntrega(fechaInicio, fechaEntrega)) return false;
            if (!ValidarPrecioServicio(precioServicioTexto, out precioServicio)) return false;
            if (!ValidarObservaciones(observaciones)) return false;
            if (!ValidarFoto(rutaFoto)) return false;
            if (!ConfirmarOrdenSinRepuestos(cantidadRepuestos)) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA — AÑADIR ORDEN
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioAñadir(
            string clienteDNI,
            string vehiculoPlaca,
            object estadoSeleccionado,
            object prioridadSeleccionada,
            DateTime? fechaInicio,
            DateTime? fechaEntrega,
            string precioServicioTexto,
            string observaciones,
            string rutaFoto,
            int cantidadRepuestos,
            out decimal precioServicio)
        {
            return ValidarFormulario(
                clienteDNI, vehiculoPlaca,
                estadoSeleccionado, prioridadSeleccionada,
                fechaInicio, fechaEntrega,
                precioServicioTexto, observaciones,
                rutaFoto, cantidadRepuestos,
                out precioServicio);
        }

        // ─────────────────────────────────────────────────────────────
        // VALIDACIÓN COMPLETA — ACTUALIZAR ORDEN
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioActualizar(
            string clienteDNI,
            string vehiculoPlaca,
            object estadoSeleccionado,
            object prioridadSeleccionada,
            DateTime? fechaInicio,
            DateTime? fechaEntrega,
            string precioServicioTexto,
            string observaciones,
            string rutaFoto,
            int cantidadRepuestos,
            out decimal precioServicio)
        {
            if (!ValidarMesActualizacion(fechaInicio))
            {
                precioServicio = 0;
                return false;
            }

            return ValidarFormulario(
                clienteDNI, vehiculoPlaca,
                estadoSeleccionado, prioridadSeleccionada,
                fechaInicio, fechaEntrega,
                precioServicioTexto, observaciones,
                rutaFoto, cantidadRepuestos,
                out precioServicio);
        }
    }
}