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
    /// 
    /// </summary>
    internal class clsConexion
    {
        /// <summary>
        /// The conexion
        /// </summary>
        string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        /// <summary>
        /// The SQL c
        /// </summary>
        public SqlConnection SqlC = new SqlConnection();

        /// <summary>
        /// Initializes a new instance of the <see cref="clsConexion"/> class.
        /// </summary>
        public clsConexion()
        {
            SqlC.ConnectionString = conexion;
        }

        /// <summary>
        /// Abrirs this instance.
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
        /// Cerrars this instance.
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
