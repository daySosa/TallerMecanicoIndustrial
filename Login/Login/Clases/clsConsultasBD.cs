using Dasboard_Prueba;
using Login.Clases;
using Órdenes_de_Trabajo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

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

        public DataTable ObtenerGastos(string busqueda = null)
        {
            try
            {
                string query = @"
            SELECT Gasto_ID, Tipo_Gasto, Nombre_Gasto, Precio_Gasto, Fecha_Gasto, Observaciones_Gasto
            FROM Contabilidad_Gastos
            WHERE (@Busqueda IS NULL
                   OR Nombre_Gasto LIKE '%' + @Busqueda + '%'
                   OR Tipo_Gasto   LIKE '%' + @Busqueda + '%')
            ORDER BY Fecha_Gasto DESC";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Busqueda", string.IsNullOrEmpty(busqueda)
                    ? (object)DBNull.Value : busqueda.Trim());

                _conexion.Abrir();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar gastos: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public int ContarNotificacionesPendientes()
        {
            try
            {
                string query = "SELECT COUNT(*) FROM Notificaciones WHERE Leida = 0";
                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                _conexion.Abrir();
                return (int)cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al contar notificaciones: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public DataTable ObtenerNotificacionesPendientes()
        {
            try
            {
                string query = @"
            SELECT Notificacion_ID, Tipo_Notificacion, Mensaje
            FROM Vista_Notificaciones_Pendientes
            ORDER BY Notificacion_ID DESC";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                _conexion.Abrir();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar notificaciones: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public void MarcarNotificacionLeida(int? id)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("sp_MarcarNotificacionLeida", _conexion.SqlC);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@NotificacionID",
                    id.HasValue ? (object)id.Value : DBNull.Value);
                _conexion.Abrir();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception("Error al marcar notificación: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool AgregarProducto(string nombre, string categoria, string marca,
                             string modelo, decimal precio, int cantidad)
        {
            try
            {
                string query = @"
            INSERT INTO Producto
                (Producto_Nombre, Producto_Categoria, Producto_Marca,
                 Producto_Modelo, Producto_Precio,
                 Producto_Cantidad_Actual, Producto_Stock_Minimo)
            VALUES
                (@Nombre, @Categoria, @Marca, @Modelo,
                 @Precio, @Cantidad, 10)";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", categoria);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", string.IsNullOrWhiteSpace(modelo)
                    ? (object)DBNull.Value : modelo.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al agregar producto: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool ActualizarProducto(int productoId, string nombre, string categoria,
                                        string marca, string modelo, decimal precio, int cantidad)
        {
            try
            {
                string query = @"
            UPDATE Producto SET
                Producto_Nombre          = @Nombre,
                Producto_Categoria       = @Categoria,
                Producto_Marca           = @Marca,
                Producto_Modelo          = @Modelo,
                Producto_Precio          = @Precio,
                Producto_Cantidad_Actual = Producto_Cantidad_Actual + @Cantidad
            WHERE Producto_ID = @ID";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", categoria);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", string.IsNullOrWhiteSpace(modelo)
                    ? (object)DBNull.Value : modelo.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);
                cmd.Parameters.AddWithValue("@ID", productoId);

                _conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al actualizar producto: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public bool ValidarLogin(string correo, string contrasena)
        {
            try
            {
                string query = @"SELECT * FROM LOGIN
                         WHERE Usuario_Email      = @correo
                         AND   Usuario_Contraseña = @contrasena";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@correo", correo);
                cmd.Parameters.AddWithValue("@contrasena", contrasena);

                _conexion.Abrir();
                SqlDataReader lector = cmd.ExecuteReader();
                bool encontrado = lector.Read();
                lector.Close();
                return encontrado;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al validar login: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public DataTable ObtenerPagos(string busqueda = null)
        {
            try
            {
                string query = @"
            SELECT 
                Pago_ID,
                Cliente_DNI,
                Cliente_Nombres,
                Orden_ID,
                Precio_Pago,
                Fecha_Pago
            FROM Vista_Pagos_Completos
            WHERE (@Busqueda IS NULL
                   OR CAST(Pago_ID AS VARCHAR) LIKE '%' + @Busqueda + '%'
                   OR Cliente_Nombres        LIKE '%' + @Busqueda + '%'
                   OR Cliente_Apellidos      LIKE '%' + @Busqueda + '%')
            ORDER BY Fecha_Pago DESC";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Busqueda", (object)busqueda ?? DBNull.Value);

                _conexion.Abrir();
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al cargar pagos: " + ex.Message);
            }
            finally { _conexion.Cerrar(); }
        }

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaOrdenes(DateTime fechaDesde)
        {
            var vals = new List<double>();
            var labels = new List<string>();
            try
            {
                string query = @"
            SELECT YEAR(Fecha) AS Anio, MONTH(Fecha) AS Mes,
                   SUM(OrdenPrecio_Total) AS Total
            FROM   Orden_Trabajo WHERE Fecha >= @Desde
            GROUP  BY YEAR(Fecha), MONTH(Fecha)
            ORDER  BY Anio, Mes";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Desde", fechaDesde);
                _conexion.Abrir();
                using SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    vals.Add(Convert.ToDouble(rd["Total"]));
                    labels.Add($"{Convert.ToInt32(rd["Mes"]):D2}/{Convert.ToInt32(rd["Anio"])}");
                }
                return (vals, labels);
            }
            catch (Exception ex) { throw new Exception("Error al cargar gráfica órdenes: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaCantidadOrdenes(DateTime fechaDesde)
        {
            var vals = new List<double>();
            var labels = new List<string>();
            try
            {
                string query = @"
            SELECT YEAR(Fecha) AS Anio, MONTH(Fecha) AS Mes,
                   COUNT(*) AS Cantidad
            FROM   Orden_Trabajo WHERE Fecha >= @Desde
            GROUP  BY YEAR(Fecha), MONTH(Fecha)
            ORDER  BY Anio, Mes";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Desde", fechaDesde);
                _conexion.Abrir();
                using SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    vals.Add(Convert.ToDouble(rd["Cantidad"]));
                    labels.Add($"{Convert.ToInt32(rd["Mes"]):D2}/{Convert.ToInt32(rd["Anio"])}");
                }
                return (vals, labels);
            }
            catch (Exception ex) { throw new Exception("Error al cargar gráfica cantidad órdenes: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaGastos(DateTime fechaDesde)
        {
            var vals = new List<double>();
            var labels = new List<string>();
            try
            {
                string query = @"
            SELECT YEAR(Fecha_Gasto) AS Anio, MONTH(Fecha_Gasto) AS Mes,
                   SUM(Precio_Gasto) AS Total
            FROM   Contabilidad_Gastos WHERE Fecha_Gasto >= @Desde
            GROUP  BY YEAR(Fecha_Gasto), MONTH(Fecha_Gasto)
            ORDER  BY Anio, Mes";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                cmd.Parameters.AddWithValue("@Desde", fechaDesde);
                _conexion.Abrir();
                using SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    vals.Add(Convert.ToDouble(rd["Total"]));
                    labels.Add($"{Convert.ToInt32(rd["Mes"]):D2}/{Convert.ToInt32(rd["Anio"])}");
                }
                return (vals, labels);
            }
            catch (Exception ex) { throw new Exception("Error al cargar gráfica gastos: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        public (List<OrdenReciente> ordenes, decimal balanceTotal, decimal gastosTotal) ObtenerDatosDashboard()
        {
            var ordenes = new List<OrdenReciente>();
            decimal balanceTotal = 0, gastosTotal = 0;
            try
            {
                string query = @"
            SELECT TOP 10
                o.Orden_ID,
                c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS Cliente_NombreCompleto,
                o.Vehiculo_Placa, o.Fecha, o.Estado, o.OrdenPrecio_Total
            FROM Orden_Trabajo o
            INNER JOIN Cliente c ON o.Cliente_DNI = c.Cliente_DNI
            ORDER BY o.Fecha DESC";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                _conexion.Abrir();
                using (SqlDataReader rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        ordenes.Add(new OrdenReciente
                        {
                            Orden_ID = Convert.ToInt32(rd["Orden_ID"]),
                            Cliente_NombreCompleto = rd["Cliente_NombreCompleto"].ToString(),
                            Vehiculo_Placa = rd["Vehiculo_Placa"].ToString(),
                            Fecha = Convert.ToDateTime(rd["Fecha"]),
                            Estado = rd["Estado"].ToString(),
                            OrdenPrecio_Total = Convert.ToDecimal(rd["OrdenPrecio_Total"])
                        });
                    }
                }

                using (SqlCommand cmd2 = new SqlCommand(
                    "SELECT ISNULL(SUM(OrdenPrecio_Total), 0) FROM Orden_Trabajo", _conexion.SqlC))
                    balanceTotal = Convert.ToDecimal(cmd2.ExecuteScalar());

                using (SqlCommand cmd3 = new SqlCommand(
                    "SELECT ISNULL(SUM(Precio_Gasto), 0) FROM Contabilidad_Gastos", _conexion.SqlC))
                    gastosTotal = Convert.ToDecimal(cmd3.ExecuteScalar());

                return (ordenes, balanceTotal, gastosTotal);
            }
            catch (Exception ex) { throw new Exception("Error al cargar datos dashboard: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }

        public List<NotificacionItem> ObtenerTodasNotificaciones()
        {
            var lista = new List<NotificacionItem>();
            try
            {
                string query = @"
            SELECT Notificacion_ID, Tipo_Notificacion, Mensaje, Leida 
            FROM Notificaciones 
            ORDER BY Notificacion_ID DESC";

                SqlCommand cmd = new SqlCommand(query, _conexion.SqlC);
                _conexion.Abrir();
                using SqlDataReader rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new NotificacionItem
                    {
                        Notificacion_ID = Convert.ToInt32(rd["Notificacion_ID"]),
                        Tipo_Notificacion = rd["Tipo_Notificacion"].ToString(),
                        Mensaje = rd["Mensaje"].ToString(),
                        Leida = Convert.ToBoolean(rd["Leida"])
                    });
                }
                return lista;
            }
            catch (Exception ex) { throw new Exception("Error al cargar notificaciones: " + ex.Message); }
            finally { _conexion.Cerrar(); }
        }
    }
}