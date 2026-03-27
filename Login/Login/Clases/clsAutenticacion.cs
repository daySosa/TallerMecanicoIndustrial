using MailKit.Net.Smtp;
using MimeKit;
using System.Data.SqlClient;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// 
    /// </summary>
    internal class clsAutenticacion
    {
        /// <summary>
        /// The conexion 2 fa
        /// </summary>
        private clsConexion conexion_2FA = new clsConexion();

        /// <summary>
        /// Generars the codigo.
        /// </summary>
        /// <param name="correo">The correo.</param>
        /// <returns></returns>
        public string GenerarCodigo(string correo)
        {
            string codigo = new Random().Next(100000, 999999).ToString();
            DateTime expiracion = DateTime.UtcNow.AddMinutes(5);

            try
            {
                conexion_2FA.Abrir();

                string invalidar = @"UPDATE CodigosOTP 
                                     SET Usado = 1 
                                     WHERE Correo = @Correo AND Usado = 0";
                SqlCommand cmdInvalidar = new SqlCommand(invalidar, conexion_2FA.SqlC);
                cmdInvalidar.Parameters.AddWithValue("@Correo", correo);
                cmdInvalidar.ExecuteNonQuery();

                string query = @"INSERT INTO CodigosOTP 
                                 (Correo, Codigo, FechaExpiracion, Usado, Intentos)
                                 VALUES (@Correo, @Codigo, @Expiracion, 0, 0)";
                SqlCommand cmd = new SqlCommand(query, conexion_2FA.SqlC);
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

        /// <summary>
        /// Validars the codigo.
        /// </summary>
        /// <param name="correo">The correo.</param>
        /// <param name="codigoIngresado">The codigo ingresado.</param>
        /// <returns></returns>
        public bool ValidarCodigo(string correo, string codigoIngresado)
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

                SqlCommand cmd = new SqlCommand(query, conexion_2FA.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Codigo", codigoIngresado);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    reader.Close();

                    string update = "UPDATE CodigosOTP SET Usado = 1 WHERE Id = @Id";
                    SqlCommand cmdUpdate = new SqlCommand(update, conexion_2FA.SqlC);
                    cmdUpdate.Parameters.AddWithValue("@Id", id);
                    cmdUpdate.ExecuteNonQuery();

                    return true;
                }
                else
                {
                    reader.Close();

                    string updateIntentos = @"UPDATE CodigosOTP 
                                             SET Intentos = Intentos + 1 
                                             WHERE Correo = @Correo 
                                             AND Usado = 0";
                    SqlCommand cmdIntentos = new SqlCommand(updateIntentos, conexion_2FA.SqlC);
                    cmdIntentos.Parameters.AddWithValue("@Correo", correo);
                    cmdIntentos.ExecuteNonQuery();

                    return false;
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

        /// <summary>
        /// Enviars the correo.
        /// </summary>
        /// <param name="correoDestino">The correo destino.</param>
        /// <param name="codigo">The codigo.</param>
        /// <returns></returns>
        public bool EnviarCorreo(string correoDestino, string codigo)
        {
            try
            {
                var mensaje = new MimeMessage();
                mensaje.From.Add(new MailboxAddress("Taller Mecánico", "tallermecanicoind26@gmail.com"));
                mensaje.To.Add(new MailboxAddress("", correoDestino));
                mensaje.Subject = "Código de verificación - Taller Mecánico";

                mensaje.Body = new TextPart("html")
                {
                    Text = $@"
                    <div style='font-family: Arial; padding: 20px;'>
                        <h2 style='color: #2563EB;'>Verificación de identidad</h2>
                        <p>Tu código de verificación es:</p>
                        <h1 style='letter-spacing: 8px; color: #1E40AF;'>{codigo}</h1>
                        <p>Este código expira en <b>5 minutos</b>.</p>
                        <p style='color: gray; font-size: 12px;'>
                            Si no fuiste tú, ignora este mensaje.
                        </p>
                    </div>"
                };

                using var smtp = new SmtpClient();
                smtp.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                smtp.Authenticate("tallermecanicoind26@gmail.com", "igzy ooxe fmjr ippx");
                smtp.Send(mensaje);
                smtp.Disconnect(true);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al enviar el correo: " + ex.Message);
                return false;
            }
        }
    }
}