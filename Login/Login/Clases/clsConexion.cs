using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.SqlClient;




namespace Login.Clases
{
    /// <summary>
    /// Clase encargada de gestionar la conexión a la base de datos SQL Server.
    /// Permite abrir y cerrar la conexión de forma segura.
    /// </summary>
    internal class clsConexion
    {
        //// <summary>
        /// Cadena de conexión utilizada para acceder a la base de datos en Azure SQL.
        /// </summary>
        string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        /// <summary>
        /// Objeto de conexión SQL que se utiliza para ejecutar comandos en la base de datos.
        /// </summary>
        public SqlConnection SqlC = new SqlConnection();

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="clsConexion"/>
        /// y asigna la cadena de conexión al objeto SqlConnection.
        /// </summary>
        public clsConexion()
        {
            SqlC.ConnectionString = conexion;
        }

        /// <summary>
        /// Abre la conexión a la base de datos si se encuentra cerrada.
        /// Maneja excepciones en caso de error durante la apertura.
        /// </summary>
        public void Abrir() 
        { 
            try 
            { 
                if (SqlC.State == ConnectionState.Closed) SqlC.Open(); 
            } 
            catch (Exception ex) 
            { 
                MessageBox.Show("Error al abrir la conexión: " + ex.Message); 
            } 
        }

        /// <summary>
        /// Cierra la conexión a la base de datos si se encuentra abierta.
        /// Maneja excepciones en caso de error durante el cierre.
        /// </summary>
        public void Cerrar() 
        { 
            try 
            { 
                if (SqlC.State == ConnectionState.Open) SqlC.Close(); 
            } 
            catch (Exception ex) { MessageBox.Show("Error al cerrar la conexión: " + ex.Message); 
            } 
        }
    }
}
