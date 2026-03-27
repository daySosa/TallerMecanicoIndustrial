using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Login.Clases
{

    /// <summary>
    /// Validaciones específicas del módulo de Clientes.
    /// Reglas de negocio que solo aplican a esta interfaz.
    /// </summary>

    class clsValidacionesClientes
    {
        /// <summary>
        /// Valida que el nombre no supere los 50 caracteres.
        /// </summary>
        public static bool ValidarLongitudNombre(string texto, string nombreCampo)
        {
            if (texto.Trim().Length > 50)
            {
                MessageBox.Show($"⚠ El {nombreCampo} no puede superar los 50 caracteres.",
                    $"{nombreCampo} inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que el correo no supere los 100 caracteres.
        /// </summary>
        public static bool ValidarLongitudCorreo(string correo)
        {
            if (correo.Trim().Length > 100)
            {
                MessageBox.Show("⚠ El correo no puede superar los 100 caracteres.",
                    "Correo inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que la dirección no esté vacía y no supere los 150 caracteres.
        /// </summary>
        public static bool ValidarDireccion(string direccion)
        {
            if (string.IsNullOrWhiteSpace(direccion))
            {
                MessageBox.Show("⚠ Escribe la dirección del cliente.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (direccion.Trim().Length > 150)
            {
                MessageBox.Show("⚠ La dirección no puede superar los 150 caracteres.",
                    "Dirección inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

    }
}
