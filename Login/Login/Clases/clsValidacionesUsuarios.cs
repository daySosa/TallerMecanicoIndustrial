using System.Windows;

namespace Login.Clases
{
    public static class clsValidacionesUsuarios
    {
        // ─────────────────────────────────────────────────────────────
        // NOMBRE
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarNombre(string nombre)
        {
            if (!clsValidaciones.ValidarTextoRequerido(nombre, "nombre")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(nombre, 200, "nombre")) return false;
            if (!clsValidaciones.ValidarIniciaConLetra(nombre, "nombre")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(nombre, "nombre")) return false;
            if (!clsValidaciones.ValidarTextoAlfanumerico(nombre, "nombre")) return false;
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(nombre, "nombre")) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // APELLIDO
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarApellido(string apellido)
        {
            if (!clsValidaciones.ValidarTextoRequerido(apellido, "apellido")) return false;
            if (!clsValidaciones.ValidarLongitudMaxima(apellido, 200, "apellido")) return false;
            if (!clsValidaciones.ValidarIniciaConLetra(apellido, "apellido")) return false;
            if (!clsValidaciones.ValidarNoEsSoloNumeros(apellido, "apellido")) return false;
            if (!clsValidaciones.ValidarTextoAlfanumerico(apellido, "apellido")) return false;
            if (!clsValidaciones.ValidarSinRepeticionExcesiva(apellido, "apellido")) return false;
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        // CORREO
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarCorreo(string correo)
        {
            string error = clsValidaciones.ValidarCorreoLogin(correo);
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
            if (!clsValidaciones.ValidarTextoRequerido(telefono, "teléfono")) return false;
            return clsValidaciones.Telefono(telefono);
        }

        // ─────────────────────────────────────────────────────────────
        // ROL
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarRolSeleccionado(object rolSeleccionado)
            => clsValidaciones.ValidarComboSeleccionado(rolSeleccionado, "rol");

        // ─────────────────────────────────────────────────────────────
        // CONTRASEÑA — nuevo usuario (obligatoria)
        // ─────────────────────────────────────────────────────────────
        public static bool ValidarContrasenaNuevoUsuario(string contrasena)
        {
            string error = clsValidaciones.ValidarContrasenaLogin(contrasena);
            if (error != null)
            {
                MessageBox.Show(error, "Contraseña inválida",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!clsValidaciones.TieneLongitudMinima(contrasena, 6))
            {
                MessageBox.Show("⚠ La contraseña debe tener al menos 6 caracteres.",
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