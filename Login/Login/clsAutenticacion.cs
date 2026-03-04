using System;
using System.Windows;
using System.Data;
using System.Data.SqlClient;

namespace Login
{
    internal class clsAutenticacion
    {
        private clsConexion conexion_2FA = new clsConexion; //Instancia con la clase conexión

        public string GenerarCodigo(string correo)
        {
            string codigo = new Random().Next(100000, 999999).ToString();
            DateTime expiracion = DateTime.UtcNow.AddMinutes(5);

            try
            {
                conexion_2FA.Abrir();

                // Invalidar códigos anteriores del mismo correo
                string invalidar = @"UPDATE CodigosOTP 
                                     SET Usado = 1 
                                     WHERE Correo = @Correo AND Usado = 0";
                SqlCommand cmdInvalidar = new SqlCommand(invalidar, _conexion.SqlC);
                cmdInvalidar.Parameters.AddWithValue("@Correo", correo);
                cmdInvalidar.ExecuteNonQuery();

                // Insertar el nuevo código
                string query = @"INSERT INTO CodigosOTP 
                                 (Correo, Codigo, FechaExpiracion, Usado, Intentos)
                                 VALUES (@Correo, @Codigo, @Expiracion, 0, 0)";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Codigo", codigo);
                cmd.Parameters.AddWithValue("@Expiracion", expiracion);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ Error al generar el código: " + ex.Message);

            }
            finally
            {
                conexion_2FA.Cerrar();
            }
            return codigo;
        }

        public void ValidarCodigo(string correo, string codigoIngresado)
        {
            try
            {
                conexion_2FA.Abrir();

                string query = @"SELECT Id, Intentos FROM CodigosOTP 
                                 WHERE Correo = @Correo 
                                 AND Codigo = @Codigo 
                                 AND Usado = 0 
                                 AND FechaExpiracion > GETUTCDATE()
                                 AND Intentos < 3";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Codigo", codigoIngresado);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    reader.Close();

                    // Marcar código como usado
                    string update = "UPDATE CodigosOTP SET Usado = 1 WHERE Id = @Id";
                    SqlCommand cmdUpdate = new SqlCommand(update, _conexion.SqlC);
                    cmdUpdate.Parameters.AddWithValue("@Id", id);
                    cmdUpdate.ExecuteNonQuery();

                    return true; // Código correcto
                }
                else
                {
                    reader.Close();

                    // Sumar intento fallido
                    string updateIntentos = @"UPDATE CodigosOTP 
                                             SET Intentos = Intentos + 1 
                                             WHERE Correo = @Correo 
                                             AND Usado = 0";
                    SqlCommand cmdIntentos = new SqlCommand(updateIntentos, _conexion.SqlC);
                    cmdIntentos.Parameters.AddWithValue("@Correo", correo);
                    cmdIntentos.ExecuteNonQuery();

                    return false; // Código incorrecto
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("⚠ Error al validar el código: " + ex.Message);
                return false;
            }
            finally
            {
                conexion_2FA.Cerrar();
            }
    }
}
