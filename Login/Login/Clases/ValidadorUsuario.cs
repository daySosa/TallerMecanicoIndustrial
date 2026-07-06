using System.Windows;

namespace Login.Clases
{
    public static class ValidadorUsuario
    {
        // ─────────────────────────────────────────────────────────────
        // NOMBRE
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarNombre(string nombre)
        {
            if (!ValidacionesGenerales.ValidarTextoRequerido(nombre, "nombre")) return false;
            if (!ValidacionesGenerales.ValidarLongitudMaxima(nombre, 200, "nombre")) return false;
            if (!ValidacionesGenerales.ValidarIniciaConLetra(nombre, "nombre")) return false;
            if (!ValidacionesGenerales.ValidarNoEsSoloNumeros(nombre, "nombre")) return false;
            if (!ValidacionesGenerales.ValidarTextoAlfanumerico(nombre, "nombre")) return false;
            if (!ValidacionesGenerales.ValidarSinRepeticionExcesiva(nombre, "nombre")) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // APELLIDO
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarApellido(string apellido)
        {
            if (!ValidacionesGenerales.ValidarTextoRequerido(apellido, "apellido")) return false;
            if (!ValidacionesGenerales.ValidarLongitudMaxima(apellido, 200, "apellido")) return false;
            if (!ValidacionesGenerales.ValidarIniciaConLetra(apellido, "apellido")) return false;
            if (!ValidacionesGenerales.ValidarNoEsSoloNumeros(apellido, "apellido")) return false;
            if (!ValidacionesGenerales.ValidarTextoAlfanumerico(apellido, "apellido")) return false;
            if (!ValidacionesGenerales.ValidarSinRepeticionExcesiva(apellido, "apellido")) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CORREO
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarCorreo(string correo)
        {
            string error = ValidacionesGenerales.ValidarCorreoLogin(correo);
            if (error != null)
            {
                MessageBox.Show(error, "Correo inválido",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // TELÉFONO (obligatorio: NOT NULL en la BD)
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarTelefono(string telefono)
        {
            if (!ValidacionesGenerales.ValidarTextoRequerido(telefono, "teléfono")) return false;

            if (!ValidacionesGenerales.Telefono(telefono))
            {
                MessageBox.Show(
                    $"⚠ El teléfono \"{telefono.Trim()}\" no tiene un formato válido.\n\n" +
                    "Debe tener 8 dígitos, por ejemplo: 9988-7766 o 99887766.",
                    "Teléfono inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // ROL
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarRolSeleccionado(object rolSeleccionado)
            => ValidacionesGenerales.ValidarComboSeleccionado(rolSeleccionado, "rol");

        // ─────────────────────────────────────────────────────────────
        // CONTRASEÑA — nuevo usuario (obligatoria)
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarContrasenaNuevoUsuario(string contrasena)
        {
            string error = ValidacionesGenerales.ValidarContrasenaLogin(contrasena);
            if (error != null)
            {
                MessageBox.Show(error, "Contraseña inválida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!ValidacionesGenerales.TieneLongitudMinima(contrasena, 6))
            {
                MessageBox.Show(
                    $"⚠ La contraseña ingresada tiene {contrasena.Length} caracteres; " +
                    "se requieren al menos 6.\n\n" +
                    "Agrega algunos caracteres más para continuar.",
                    "Contraseña inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CONTRASEÑA — edición (opcional: solo valida si se escribió algo)
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarContrasenaEdicion(string contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena)) return true; // no se cambia
            return ValidarContrasenaNuevoUsuario(contrasena);
        }
    }
}