using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Login.Clases
{
    public class clsValidacionesClientes
    {
        // ─────────────────────────────────────────────────────────────
        // FORMULARIO VACÍO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarFormularioVacio(params string[] campos)
        {
            return clsValidaciones.ValidarFormularioVacio(campos);
        }

        // ─────────────────────────────────────────────────────────────
        // CAMPOS DUPLICADOS
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarDNINoDuplicado(string dni, string dniActual, clsConsultasBD db)
        {
            if (db.ExisteDNIEnOtroCliente(dni, dniActual))
            {
                MessageBox.Show("⚠ Este DNI ya está registrado en otro cliente.",
                    "DNI duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public static bool ValidarTelefonoNoDuplicado(string telefono, string dniActual, clsConsultasBD db)
        {
            if (db.ExisteTelefonoEnOtroCliente(telefono, dniActual))
            {
                MessageBox.Show("⚠ Este número de teléfono ya está registrado en otro cliente.",
                    "Teléfono duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // DICCIONARIOS — departamentos y municipios de Honduras
        // ─────────────────────────────────────────────────────────────

        private static readonly Dictionary<int, (string nombre, int totalMunicipios)> _departamentosHN =
            new Dictionary<int, (string, int)>
            {
                {  1, ("Atlántida",          8) },
                {  2, ("Colón",             10) },
                {  3, ("Comayagua",         21) },
                {  4, ("Copán",             23) },
                {  5, ("Cortés",            12) },
                {  6, ("Choluteca",         16) },
                {  7, ("El Paraíso",        19) },
                {  8, ("Francisco Morazán", 28) },
                {  9, ("Gracias a Dios",     6) },
                { 10, ("Intibucá",          17) },
                { 11, ("Islas de la Bahía",  4) },
                { 12, ("La Paz",            19) },
                { 13, ("Lempira",           28) },
                { 14, ("Ocotepeque",        16) },
                { 15, ("Olancho",           23) },
                { 16, ("Santa Bárbara",     28) },
                { 17, ("Valle",              9) },
                { 18, ("Yoro",             11) }
            };

        private static readonly Dictionary<int, Dictionary<int, string>> _municipiosHN =
            new Dictionary<int, Dictionary<int, string>>
            {
                { 1, new Dictionary<int, string> {
                    {1,"La Ceiba"},{2,"El Porvenir"},{3,"Esparta"},{4,"Jutiapa"},
                    {5,"La Masica"},{6,"San Francisco"},{7,"Tela"},{8,"Arizona"} }},
                { 2, new Dictionary<int, string> {
                    {1,"Trujillo"},{2,"Balfate"},{3,"Iriona"},{4,"La Ceiba"},
                    {5,"Limón"},{6,"Sabá"},{7,"Santa Fe"},{8,"Santa Rosa de Aguán"},
                    {9,"Sonaguera"},{10,"Tocoa"} }},
                { 3, new Dictionary<int, string> {
                    {1,"Comayagua"},{2,"Ajuterique"},{3,"El Rosario"},{4,"Esquías"},
                    {5,"Humuya"},{6,"La Libertad"},{7,"Lamaní"},{8,"La Trinidad"},
                    {9,"Lejamani"},{10,"Meámbar"},{11,"Minas de Oro"},{12,"Ojos de Agua"},
                    {13,"San Jerónimo"},{14,"San José de Comayagua"},{15,"San José del Potrero"},
                    {16,"San Luis"},{17,"San Sebastián"},{18,"Siguatepeque"},
                    {19,"Villa de San Antonio"},{20,"Las Lajas"},{21,"Taulabé"} }},
                { 4, new Dictionary<int, string> {
                    {1,"Santa Rosa de Copán"},{2,"Cabañas"},{3,"Concepción"},{4,"Copán Ruinas"},
                    {5,"Corquín"},{6,"Cucuyagua"},{7,"Dolores"},{8,"Dulce Nombre"},
                    {9,"El Paraíso"},{10,"Florida"},{11,"La Jigua"},{12,"La Unión"},
                    {13,"Nueva Arcadia"},{14,"San Agustín"},{15,"San Antonio"},
                    {16,"San Jerónimo"},{17,"San José"},{18,"San Juan de Opoa"},
                    {19,"San Nicolás"},{20,"San Pedro"},{21,"Santa Rita"},
                    {22,"Trinidad de Copán"},{23,"Veracruz"} }},
                { 5, new Dictionary<int, string> {
                    {1,"San Pedro Sula"},{2,"Choloma"},{3,"Omoa"},{4,"Pimienta"},
                    {5,"Potrerillos"},{6,"Puerto Cortés"},{7,"San Antonio de Cortés"},
                    {8,"San Francisco de Yojoa"},{9,"San Manuel"},{10,"Santa Cruz de Yojoa"},
                    {11,"Villanueva"},{12,"La Lima"} }},
                { 6, new Dictionary<int, string> {
                    {1,"Choluteca"},{2,"Apacilagua"},{3,"Concepción de María"},{4,"Duyure"},
                    {5,"El Corpus"},{6,"El Triunfo"},{7,"Marcovia"},{8,"Morolica"},
                    {9,"Namasigüe"},{10,"Orocuina"},{11,"Pespire"},{12,"San Antonio de Flores"},
                    {13,"San Isidro"},{14,"San José"},{15,"San Marcos de Colón"},{16,"Santa Ana"} }},
                { 7, new Dictionary<int, string> {
                    {1,"Yuscarán"},{2,"Alauca"},{3,"Danlí"},{4,"El Paraíso"},
                    {5,"Guinope"},{6,"Jacaleapa"},{7,"Liure"},{8,"Morocelí"},
                    {9,"Oropolí"},{10,"Potrerillos"},{11,"San Antonio de Flores"},
                    {12,"San Lucas"},{13,"San Matías"},{14,"Soledad"},{15,"Teupasenti"},
                    {16,"Texiguat"},{17,"Vado Ancho"},{18,"Yauyupe"},{19,"Trojes"} }},
                { 8, new Dictionary<int, string> {
                    {1,"Tegucigalpa D.C."},{2,"Alubarén"},{3,"Cedros"},{4,"Curarén"},
                    {5,"El Porvenir"},{6,"Guaimaca"},{7,"La Libertad"},{8,"La Venta"},
                    {9,"Lepaterique"},{10,"Maraita"},{11,"Marale"},{12,"Nueva Armenia"},
                    {13,"Ojojona"},{14,"Orica"},{15,"Reitoca"},{16,"Sabanagrande"},
                    {17,"San Antonio de Oriente"},{18,"San Buenaventura"},{19,"San Ignacio"},
                    {20,"San Juan de Flores"},{21,"San Miguelito"},{22,"Santa Ana"},
                    {23,"Santa Lucía"},{24,"Talanga"},{25,"Tatumbla"},{26,"Valle de Ángeles"},
                    {27,"Villa de San Francisco"},{28,"Vallecillo"} }},
                { 9, new Dictionary<int, string> {
                    {1,"Puerto Lempira"},{2,"Brus Laguna"},{3,"Ahuas"},
                    {4,"Juan Francisco Bulnes"},{5,"Villeda Morales"},{6,"Wampusirpi"} }},
                { 10, new Dictionary<int, string> {
                    {1,"La Esperanza"},{2,"Camasca"},{3,"Colomoncagua"},{4,"Concepción"},
                    {5,"Dolores"},{6,"Intibucá"},{7,"Jesús de Otoro"},{8,"Magdalena"},
                    {9,"Masaguara"},{10,"San Antonio"},{11,"San Isidro"},{12,"San Juan"},
                    {13,"San Marcos de la Sierra"},{14,"San Miguel Guancapla"},
                    {15,"Santa Lucía"},{16,"Yamaranguila"},{17,"San Francisco de Opalaca"} }},
                { 11, new Dictionary<int, string> {
                    {1,"Roatán"},{2,"Guanaja"},{3,"José Santos Guardiola"},{4,"Utila"} }},
                { 12, new Dictionary<int, string> {
                    {1,"La Paz"},{2,"Aguanqueterique"},{3,"Cabañas"},{4,"Cane"},
                    {5,"Chinacla"},{6,"Guajiquiro"},{7,"Lauterique"},{8,"Marcala"},
                    {9,"Mercedes de Oriente"},{10,"Opatoro"},{11,"San Antonio del Norte"},
                    {12,"San José"},{13,"San Juan"},{14,"San Pedro de Tutule"},
                    {15,"Santa Ana"},{16,"Santa Elena"},{17,"Santa María"},
                    {18,"Santiago de Puringla"},{19,"Yarula"} }},
                { 13, new Dictionary<int, string> {
                    {1,"Gracias"},{2,"Belén"},{3,"Candelaria"},{4,"Cololaca"},
                    {5,"Erandique"},{6,"Gualcince"},{7,"Guarita"},{8,"La Campa"},
                    {9,"La Iguala"},{10,"Las Flores"},{11,"La Unión"},{12,"La Virtud"},
                    {13,"Lepaera"},{14,"Mapulaca"},{15,"Piraera"},{16,"San Andrés"},
                    {17,"San Francisco"},{18,"San Juan Guarita"},{19,"San Manuel Colohete"},
                    {20,"San Rafael"},{21,"San Sebastián"},{22,"Santa Cruz"},
                    {23,"Talgua"},{24,"Tambla"},{25,"Tomalá"},{26,"Valladolid"},
                    {27,"Virginia"},{28,"San Marcos de Caiquín"} }},
                { 14, new Dictionary<int, string> {
                    {1,"Ocotepeque"},{2,"Belén Gualcho"},{3,"Concepción"},{4,"Dolores Merendón"},
                    {5,"Fraternidad"},{6,"La Encarnación"},{7,"La Labor"},{8,"Lucerna"},
                    {9,"Mercedes"},{10,"San Fernando"},{11,"San Francisco del Valle"},
                    {12,"San Jorge"},{13,"San Marcos"},{14,"Santa Fe"},
                    {15,"Sensenti"},{16,"Sinuapa"} }},
                { 15, new Dictionary<int, string> {
                    {1,"Juticalpa"},{2,"Campamento"},{3,"Catacamas"},{4,"Concordia"},
                    {5,"Dulce Nombre de Culmí"},{6,"El Rosario"},{7,"Esquipulas del Norte"},
                    {8,"Gualaco"},{9,"Guarizama"},{10,"Guata"},{11,"Guayape"},
                    {12,"Jano"},{13,"La Unión"},{14,"Lepaguare"},{15,"Manto"},
                    {16,"Salamá"},{17,"San Esteban"},{18,"San Francisco de Becerra"},
                    {19,"San Francisco de la Paz"},{20,"Santa María del Real"},
                    {21,"Silca"},{22,"Yocón"},{23,"Patuca"} }},
                { 16, new Dictionary<int, string> {
                    {1,"Santa Bárbara"},{2,"Arada"},{3,"Atima"},{4,"Azacualpa"},
                    {5,"Ceguaca"},{6,"Concepción del Norte"},{7,"Concepción del Sur"},
                    {8,"Chinda"},{9,"El Níspero"},{10,"Gualala"},{11,"Ilama"},
                    {12,"Macuelizo"},{13,"Naranjito"},{14,"Nuevo Celilac"},{15,"Petoa"},
                    {16,"Protección"},{17,"Quimistán"},{18,"San Francisco de Ojuera"},
                    {19,"San José de Colinas"},{20,"San Luis"},{21,"San Marcos"},
                    {22,"San Nicolás"},{23,"San Pedro Zacapa"},{24,"Santa Rita"},
                    {25,"San Vicente Centenario"},{26,"Trinidad"},{27,"Las Vegas"},
                    {28,"Nueva Frontera"} }},
                { 17, new Dictionary<int, string> {
                    {1,"Nacaome"},{2,"Alianza"},{3,"Amapala"},{4,"Aramecina"},
                    {5,"Caridad"},{6,"Goascorán"},{7,"Langue"},{8,"San Francisco de Coray"},
                    {9,"San Lorenzo"} }},
                { 18, new Dictionary<int, string> {
                    {1,"Yoro"},{2,"Arenal"},{3,"El Negrito"},{4,"El Progreso"},
                    {5,"Jocon"},{6,"Morazán"},{7,"Olanchito"},{8,"Santa Rita"},
                    {9,"Sulaco"},{10,"Victoria"},{11,"Yorito"} }}
            };

        // ─────────────────────────────────────────────────────────────
        // DNI
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarDNIHondureño(string dni)
        {
            if (!clsValidaciones.ValidarFormatoDNI(dni)) return false;
            return DNI(dni);
        }

        public static bool DNI(string valor, Control campo = null)
        {
            string dni = valor.Trim();

            if (!clsValidaciones.ValidarFormatoDNI(dni)) return false;

            // ── Departamento ──────────────────────────────────────────
            int depto = int.Parse(dni.Substring(0, 2));
            if (!_departamentosHN.ContainsKey(depto))
            {
                MessageBox.Show(
                    $"⚠ El código de departamento '{depto:D2}' no es válido.\n\n" +
                    "Los departamentos de Honduras van del 01 al 18.",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // ── Municipio ─────────────────────────────────────────────
            int muni = int.Parse(dni.Substring(2, 2));
            var (nombreDepto, totalMunicipios) = _departamentosHN[depto];

            if (muni < 1 || muni > totalMunicipios)
            {
                MessageBox.Show(
                    $"⚠ El municipio '{muni:D2}' no es válido para el departamento de {nombreDepto}.\n\n" +
                    $"{nombreDepto} tiene {totalMunicipios} municipio(s) (01 al {totalMunicipios:D2}).",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // ── Año de nacimiento + edad mínima ───────────────────────
            int anioNacimiento = int.Parse(dni.Substring(4, 4));
            int anioActual = DateTime.Now.Year;
            int anioMinimo = anioActual - 75;
            int anioMaximo = anioActual - 18;

            if (anioNacimiento < anioMinimo || anioNacimiento > anioMaximo)
            {
                MessageBox.Show(
                    "⚠ El cliente debe tener 18 años o más para ser registrado.",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // ── Secuencial ────────────────────────────────────────────
            if (dni.Substring(8, 5) == "00000")
            {
                MessageBox.Show("⚠ Los últimos 5 dígitos del DNI no pueden ser todos ceros.",
                    "DNI inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // NOMBRE / APELLIDO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarLongitudNombre(string texto, string nombreCampo)
        {
            return clsValidaciones.ValidarTextoRequerido(texto, nombreCampo)
                && clsValidaciones.ValidarSinRepeticionExcesiva(texto.Trim(), nombreCampo)
                && clsValidaciones.ValidarLongitudMaxima(texto.Trim(), 50, nombreCampo);
        }

        // ─────────────────────────────────────────────────────────────
        // CORREO
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarLongitudCorreo(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return true;

            string error = clsValidaciones.ValidarCorreoLogin(correo.Trim());
            if (error != null)
            {
                MessageBox.Show(error, "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.ValidarLongitudMaxima(correo, 100, "correo")) return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // DIRECCIÓN
        // ─────────────────────────────────────────────────────────────

        public static bool ValidarDireccion(string direccion)
        {
            return clsValidaciones.ValidarTextoRequerido(direccion, "dirección del cliente")
                && clsValidaciones.ValidarNoEsSoloNumeros(direccion.Trim(), "dirección")
                && clsValidaciones.ValidarIniciaConLetra(direccion.Trim(), "dirección")
                && clsValidaciones.ValidarSinRepeticionExcesiva(direccion.Trim(), "dirección")
                && clsValidaciones.ValidarLongitudMaxima(direccion.Trim(), 150, "dirección");
        }
    }
}