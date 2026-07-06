using System.Windows;

namespace Login.Clases
{
    public class ValidadorÓrden
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(string clienteDNI, string vehiculoPlaca, string precio)
        {
            string precioLimpio = precio.Replace("L", "").Replace("0.00", "")
                                        .Replace(",", "").Replace(" ", "").Trim();

            return ValidacionesGenerales.ValidarFormularioVacio(clienteDNI, vehiculoPlaca, precioLimpio);
        }

        // ─────────────────────────────────────────────────────────────
        // BÚSQUEDA — DNI / PLACA
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarCampoBusqueda(string valor, bool esDNI)
        {
            string campo = esDNI ? "DNI del cliente" : "placa del vehículo";
            return ValidacionesGenerales.ValidarTextoRequerido(valor,
                $"⚠ Ingresa el {campo} para realizar la búsqueda.",
                msg => MessageBox.Show(msg, "Campo vacío", MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        public static bool ValidarFormatoDNIBusqueda(string dni)
        {
            return ValidacionesGenerales.ValidarFormatoDNI(dni);
        }

        public static bool ValidarFormatoPlacaBusqueda(string placa)
        {
            string p = placa.Trim().ToUpper();

            return ValidadorVehículo.ValidarFormatoPlacaSegunTipo(p, string.Empty);
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
                MessageBox.Show(
                    "⚠ Esta orden no tiene un cliente asignado.\n\n" +
                    "Ingresa el DNI del cliente o la placa de su vehículo y presiona \"Buscar\" " +
                    "para asignarlo antes de guardar.",
                    "Cliente no asignado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarVehiculoAsignado(string vehiculoPlaca)
        {
            if (string.IsNullOrWhiteSpace(vehiculoPlaca))
            {
                MessageBox.Show(
                    "⚠ Esta orden no tiene un vehículo asignado.\n\n" +
                    "El vehículo se asigna automáticamente al buscar por DNI o placa; " +
                    "si el cliente tiene varios vehículos, selecciona uno de la lista.",
                    "Vehículo no asignado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // ESTADO DEL CLIENTE Y VEHÍCULO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarClienteActivo(bool clienteActivo, string nombreCliente)
        {
            if (!clienteActivo)
            {
                MessageBox.Show(
                    $"⚠ El cliente \"{nombreCliente}\" se encuentra inactivo.\n\n" +
                    "No es posible crear órdenes para clientes dados de baja.\n" +
                    "Actívalo desde el módulo de Clientes si deseas continuar.",
                    "Cliente inactivo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        public static bool ValidarVehiculoActivo(bool vehiculoActivo, string nombreVehiculo)
        {
            if (!vehiculoActivo)
            {
                MessageBox.Show(
                    $"⚠ El vehículo \"{nombreVehiculo}\" está fuera de servicio (inactivo).\n\n" +
                    "No es posible crear órdenes para vehículos dados de baja.\n" +
                    "Actívalo desde el módulo de Vehículos si deseas continuar.",
                    "Vehículo inactivo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // ESTADO DE LA ORDEN
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarEstadoOrden(object itemSeleccionado)
        {
            return ValidacionesGenerales.ValidarComboSeleccionado(itemSeleccionado, "estado de la orden");
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
            return ValidacionesGenerales.ValidarComboSeleccionado(itemSeleccionado, "prioridad de la orden");
        }

        // ─────────────────────────────────────────────────────────────
        // FECHAS
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFechaInicio(DateTime? fecha)
        {
            if (!fecha.HasValue)
            {
                MessageBox.Show(
                    "⚠ Selecciona la fecha de inicio; este campo no puede quedar vacío.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            var hoy = DateTime.Today;

            if (fecha.Value.Date != hoy)
            {
                MessageBox.Show(
                    $"⚠ Seleccionaste {fecha.Value:dd/MM/yyyy}, pero las órdenes nuevas " +
                    $"deben iniciar hoy ({hoy:dd/MM/yyyy}).",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static bool ValidarFechaEntrega(DateTime? fechaInicio, DateTime? fechaEntrega)
        {
            if (!fechaEntrega.HasValue)
            {
                MessageBox.Show(
                    "⚠ Selecciona una fecha estimada de entrega para el cliente.",
                    "Fecha requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!ValidacionesGenerales.ValidarFechaEntrega(fechaInicio, fechaEntrega))
                return false;

            if (fechaEntrega.Value.Date > DateTime.Today.AddYears(2))
            {
                MessageBox.Show(
                    $"⚠ La fecha de entrega ({fechaEntrega.Value:dd/MM/yyyy}) está a más de 2 años. " +
                    "Verifica que no hayas seleccionado el año equivocado.",
                    "Fecha inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public static bool ValidarMesActualizacion(DateTime? fecha)
        {
            return ValidacionesGenerales.ValidarMesOrden(fecha);
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
                MessageBox.Show(
                    "⚠ El precio del servicio no puede estar vacío ni ser 0.\n\n" +
                    "Ingresa el costo total a cobrar por la mano de obra, ej: 1500.00",
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
                MessageBox.Show(
                    $"⚠ \"{texto.Trim()}\" no es un precio válido.\n\n" +
                    "Ingresa solo números y, si aplica, un punto decimal. Ejemplo: 1500.50",
                    "Precio inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (precio > 999_999.99m)
            {
                MessageBox.Show(
                    $"⚠ El precio ingresado (L {precio:N2}) supera el límite permitido de L 999,999.99.\n\n" +
                    "Si es un trabajo mayor, divídelo en varias órdenes o consulta con administración.",
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

            if (!ValidacionesGenerales.ValidarIniciaConLetra(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarNoEsSoloNumeros(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarSinRepeticionExcesiva(texto.Trim(), "observaciones")) return false;
            if (!ValidacionesGenerales.ValidarLongitudMaxima(texto, 500, "observaciones")) return false;

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
                MessageBox.Show(
                    "⚠ La foto seleccionada ya no se encuentra en su ubicación original.\n\n" +
                    $"Ruta buscada: {rutaFoto}\n\n" +
                    "Puede que el archivo haya sido movido o eliminado; selecciona otra foto.",
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
                MessageBox.Show(
                    $"⚠ El archivo tiene extensión \"{ext}\", que no está permitida.\n\n" +
                    "Formatos aceptados: JPG · JPEG · PNG · BMP",
                    "Formato no permitido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            long tamañoBytes = new System.IO.FileInfo(rutaFoto).Length;
            if (tamañoBytes > 5 * 1024 * 1024)
            {
                MessageBox.Show(
                    $"⚠ La imagen pesa {tamañoBytes / 1024.0 / 1024.0:N1} MB; el límite es 5 MB.\n\n" +
                    "Comprime la imagen o toma la foto con menor resolución.",
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
            out decimal precioServicio,
            bool esActualizar = false)
        {
            precioServicio = 0;

            if (!ValidarClienteAsignado(clienteDNI)) return false;
            if (!ValidarVehiculoAsignado(vehiculoPlaca)) return false;
            if (!ValidarEstadoOrden(estadoSeleccionado)) return false;
            if (!ValidarPrioridad(prioridadSeleccionada)) return false;
            if (!esActualizar && !ValidarFechaInicio(fechaInicio)) return false;
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
                out precioServicio,
                esActualizar: true);
        }
    }
}