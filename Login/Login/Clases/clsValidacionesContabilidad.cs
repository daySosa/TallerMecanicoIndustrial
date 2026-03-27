using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Validaciones específicas del módulo de Contabilidad.
    /// Reglas de negocio que solo aplican a egresos e ingresos.
    /// </summary>

    class clsValidacionesContabilidad
    {

        /// <summary>
        /// Valida que el nombre del gasto no supere los 100 caracteres.
        /// </summary>
        public static bool ValidarLongitudNombreGasto(string texto)
        {
            if (texto.Trim().Length > 100)
            {
                MessageBox.Show("⚠ El nombre del gasto no puede superar los 100 caracteres.",
                    "Nombre inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Valida que las observaciones sean obligatorias y no superen los 300 caracteres.
        /// </summary>
        public static bool ValidarObservaciones(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                MessageBox.Show("⚠ Escribe las observaciones del gasto.",
                    "Campo requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (texto.Trim().Length > 300)
            {
                MessageBox.Show("⚠ Las observaciones no pueden superar los 300 caracteres.",
                    "Observaciones inválidas", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }


        /// <summary>
        /// Valida que el ID de la orden sea un número entero positivo mayor a cero.
        /// </summary>
        public static bool ValidarOrdenId(string texto, out int ordenId)
        {
            ordenId = 0;
            if (!int.TryParse(texto.Trim(), out ordenId) || ordenId <= 0)
            {
                MessageBox.Show("⚠ El ID de la orden debe ser un número entero mayor a 0.",
                    "ID de orden inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

    }
}
