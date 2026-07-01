using System;
using System.Collections.Generic;
using System.Text;

namespace Login.Clases
{
    /// <summary>
    /// Guarda los datos del usuario logueado durante toda la sesión de la app.
    /// Es estática porque debe ser accesible desde cualquier ventana sin pasarla por parámetro.
    /// </summary>
    public static class clsSesion
    {
        public static string Email { get; private set; }
        public static string Nombre { get; private set; }
        public static string Apellido { get; private set; }
        public static string Rol { get; private set; }

        public static bool EsAdministrador => Rol == "Administrador";
        public static bool HaySesionActiva => !string.IsNullOrEmpty(Email);

        public static void IniciarSesion(string email, string nombre, string apellido, string rol)
        {
            Email = email;
            Nombre = nombre;
            Apellido = apellido;
            Rol = rol;
        }

        public static void CerrarSesion()
        {
            Email = null;
            Nombre = null;
            Apellido = null;
            Rol = null;
        }
    }
}