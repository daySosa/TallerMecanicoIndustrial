using Microsoft.Data.SqlClient;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Clase encargada de gestionar la conexión a la base de datos SQL Server.
    /// Permite abrir y cerrar la conexión de forma segura.
    /// La cadena de conexión se obtiene desde App.config.
    /// </summary>
    internal sealed class ClsConexion : IDisposable
    {
        private const string NombreConexion = "TallerMecanico";

        /// <summary>
        /// Objeto de conexión SQL que se utiliza para ejecutar comandos en la base de datos.
        /// </summary>
        public SqlConnection SqlC { get; }

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="ClsConexion"/> leyendo
        /// la cadena de conexión desde App.config.
        /// </summary>
        public ClsConexion()
        {
            string cadena = ObtenerCadenaConexion();
            SqlC = new SqlConnection(cadena);
        }

        /// <summary>
        /// Obtiene la cadena de conexión definida en App.config.
        /// Lanza una excepción clara si no se encuentra configurada.
        /// </summary>
        private static string ObtenerCadenaConexion()
        {
            var config = ConfigurationManager.ConnectionStrings[NombreConexion];

            if (config == null || string.IsNullOrWhiteSpace(config.ConnectionString))
            {
                throw new InvalidOperationException(
                    $"No se encontró la cadena de conexión '{NombreConexion}' en App.config.");
            }

            return config.ConnectionString;
        }

        /// <summary>
        /// Abre la conexión a la base de datos si se encuentra cerrada.
        /// </summary>
        public bool Abrir()
        {
            try
            {
                if (SqlC.State == ConnectionState.Closed)
                    SqlC.Open();

                return true;
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error al abrir la conexión: {ex.Message}",
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Cierra la conexión a la base de datos si se encuentra abierta.
        /// </summary>
        public void Cerrar()
        {
            try
            {
                if (SqlC.State == ConnectionState.Open)
                    SqlC.Close();
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Error al cerrar la conexión: {ex.Message}",
                    "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Libera los recursos de la conexión SQL.
        /// </summary>
        public void Dispose()
        {
            SqlC?.Dispose();
        }
    }
}