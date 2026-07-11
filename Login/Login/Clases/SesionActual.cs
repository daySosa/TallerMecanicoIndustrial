namespace Login.Clases
{
    /// <summary>
    /// Guarda los datos del usuario logueado durante toda la sesión de la app.
    /// Es estática porque debe ser accesible desde cualquier ventana sin pasarla por parámetro.
    /// </summary>
    public static class SesionActual
    {
        public static string Email { get; private set; }
        public static string Nombre { get; private set; }
        public static string Apellido { get; private set; }
        public static string Rol { get; private set; }

        public static bool EsAdministrador => Rol == "Administrador";
        public static bool HaySesionActiva => !string.IsNullOrEmpty(Email);


        private static readonly RepositorioSql _db = new();

        public static void IniciarSesion(string email, string nombre, string apellido, string rol)
        {
            Email = email;
            Nombre = nombre;
            Apellido = apellido;
            Rol = rol;
        }

        public static void CerrarSesion()
        {
            if (!string.IsNullOrEmpty(Email))
            {
                try
                {
                    _db.RegistrarBitacora(Email, "Sesión", "Cerrar sesión",
                        $"{Nombre} {Apellido} cerró sesión");
                }
                catch
                {

                }
            }

            Email = null;
            Nombre = null;
            Apellido = null;
            Rol = null;
        }
    }
}