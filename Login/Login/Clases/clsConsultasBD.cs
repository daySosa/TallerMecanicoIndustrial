using System.Data.SqlClient;
using Órdenes_de_Trabajo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Login.Clases;

namespace Login.Clases
{
    public class clsConsultasBD
    {
        private clsConexion _conexion = new clsConexion(); 

        public bool ActualizarGasto(int gastoId, string tipoGasto, string nombreGasto,
                                     string observaciones, decimal precio, DateTime fecha)
        {
            try
            {
                string query = @"
                    UPDATE Contabilidad_Gastos SET
                        Tipo_Gasto          = @TipoGasto,
                        Nombre_Gasto        = @NombreGasto,
                        Observaciones_Gasto = @Observaciones,
                        Precio_Gasto        = @Precio,
                        Fecha_Gasto         = @Fecha
                    WHERE Gasto_ID = @GastoID";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@TipoGasto", tipoGasto);
                cmd.Parameters.AddWithValue("@NombreGasto", nombreGasto);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                                            ? (object)DBNull.Value
                                            : observaciones);
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Fecha", fecha);
                cmd.Parameters.AddWithValue("@GastoID", gastoId);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar el gasto: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool AgregarGasto(string tipoGasto, string nombreGasto, string observaciones, decimal precio)
        {
            try
            {
                string query = @"
                    INSERT INTO Contabilidad_Gastos 
                        (Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto)
                    VALUES 
                        (@TipoGasto, @NombreGasto, @Observaciones, @Precio, GETDATE())";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@TipoGasto", tipoGasto);
                cmd.Parameters.AddWithValue("@NombreGasto", nombreGasto);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                    ? (object)DBNull.Value
                    : observaciones.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al guardar el gasto: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }



        public (string nombres, string apellidos) BuscarNombreCliente(string dni)
        {
            try
            {
                string query = "SELECT Cliente_Nombres, Cliente_Apellidos FROM Cliente WHERE Cliente_DNI = @DNI";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@DNI", dni);

                _conexion.Abrir();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                    return (reader["Cliente_Nombres"].ToString(), reader["Cliente_Apellidos"].ToString());
                else
                    return (null, null);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al buscar cliente: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public decimal? ObtenerTotalOrden(int ordenId)
        {
            try
            {
                string query = "SELECT OrdenPrecio_Total FROM Orden_Trabajo WHERE Orden_ID = @OrdenID";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);

                _conexion.Abrir();
                object result = cmd.ExecuteScalar();

                return (result != null && result != DBNull.Value)
                    ? Convert.ToDecimal(result)
                    : (decimal?)null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener total de orden: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool ActualizarPago(int pagoId, string dni, int ordenId, decimal monto)
        {
            try
            {
                string query = @"
                    UPDATE Contabilidad_Pago
                    SET Cliente_DNI = @DNI,
                        Orden_ID    = @OrdenID,
                        Precio_Pago = @Monto
                    WHERE Pago_ID = @PagoID";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                cmd.Parameters.AddWithValue("@Monto", monto);
                cmd.Parameters.AddWithValue("@PagoID", pagoId);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar el pago: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool RegistrarPago(string clienteDni, int ordenId, decimal monto)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("sp_RegistrarPago", _conexion.SqlC);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ClienteDNI", clienteDni);
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                cmd.Parameters.AddWithValue("@Monto", monto);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException sqlEx)
            {
                throw new Exception(sqlEx.Message);
            }
            catch (Exception ex)
            {
                throw new Exception("Error al registrar el pago: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }


        public List<clsProductoInventario> ObtenerProductosInventario()
        {
            var lista = new List<clsProductoInventario>();
            try
            {
                SqlCommand cmd = new SqlCommand("sp_ObtenerProductosInventario", _conexion.SqlC);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Busqueda", DBNull.Value);

                _conexion.Abrir();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        lista.Add(new clsProductoInventario
                        {
                            Producto_ID = rd.GetInt32(rd.GetOrdinal("Producto_ID")),
                            Producto_Nombre = rd["Producto_Nombre"].ToString(),
                            Producto_Categoria = rd["Producto_Categoria"].ToString(),
                            Producto_Cantidad_Actual = rd.GetInt32(rd.GetOrdinal("Producto_Cantidad_Actual")),
                            Producto_Precio = rd.GetDecimal(rd.GetOrdinal("Producto_Precio"))
                        });
                    }
                }
                return lista;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar productos: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }


        public bool AgregarCliente(string dni, string nombres, string apellidos,
                                    string telefono, string email, string direccion)
        {
            try
            {
                string sql = @"
            IF NOT EXISTS (SELECT 1 FROM Cliente WHERE Cliente_DNI = @DNI)
            BEGIN
                INSERT INTO Cliente
                    (Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                     Cliente_TelefonoPrincipal, Cliente_Email,
                     Cliente_Direccion, Cliente_Activo)
                VALUES
                    (@DNI, @Nombres, @Apellidos, @Telefono, @Email,
                     @Direccion, 1)
                SELECT 1
            END
            ELSE
                SELECT 0";

                SqlCommand cmd = new SqlCommand(sql, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@Nombres", nombres);
                cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email)
                    ? (object)DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(direccion)
                    ? (object)DBNull.Value : direccion.Trim());

                _conexion.Abrir();
                int resultado = Convert.ToInt32(cmd.ExecuteScalar());
                return resultado == 1; 
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar cliente: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool ActualizarCliente(string dni, string nombres, string apellidos,
                                       string telefono, string email, string direccion, bool activo)
        {
            try
            {
                string sql = @"
            UPDATE Cliente SET
                Cliente_Nombres           = @Nombres,
                Cliente_Apellidos         = @Apellidos,
                Cliente_TelefonoPrincipal = @Telefono,
                Cliente_Email             = @Email,
                Cliente_Direccion         = @Direccion,
                Cliente_Activo            = @Activo
            WHERE Cliente_DNI = @DNI";

                SqlCommand cmd = new SqlCommand(sql, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Nombres", nombres);
                cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email)
                    ? (object)DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(direccion)
                    ? (object)DBNull.Value : direccion.Trim());
                cmd.Parameters.AddWithValue("@Activo", activo ? 1 : 0);
                cmd.Parameters.AddWithValue("@DNI", dni);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar cliente: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public DataRow ObtenerComprobantePago(int pagoId)
        {
            try
            {
                string query = @"
            SELECT 
                Pago_ID, Precio_Pago, Fecha_Pago,
                Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                Orden_ID
            FROM Vista_Pagos_Completos
            WHERE Pago_ID = @PagoID";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@PagoID", pagoId);

                _conexion.Abrir();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al obtener comprobante: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }
    }
}