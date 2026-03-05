using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data;
using System.Data.SqlClient;





namespace Login
{
    internal class clsConexion
    {
        string conexion = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Serv2026;";

        public SqlConnection SqlC = new SqlConnection();

        public clsConexion()
        {
            SqlC.ConnectionString = conexion;
        }

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
