using Dasboard_Prueba;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using Órdenes_de_Trabajo;
using System.Data;
using Vehículos;

namespace Login.Clases
{
    /// <summary>
    /// Repositorio de acceso a datos SQL para el sistema del taller mecánico.
    /// Cada método abre y cierra su propia conexión de forma independiente,
    /// permitiendo que las consultas puedan ejecutarse en paralelo (Task.WhenAll)
    /// sin compartir estado ni pelear por la misma conexión física.
    /// </summary>
    public class RepositorioSql
    {
        #region CONTABILIDAD - GASTOS

        public bool ActualizarGasto(int gastoId, string tipoGasto, string nombreGasto,
                                     string observaciones, decimal precio, DateTime fecha)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Gasto_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@GastoID", gastoId);
                cmd.Parameters.AddWithValue("@TipoGasto", tipoGasto);
                cmd.Parameters.AddWithValue("@NombreGasto", nombreGasto);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Fecha", fecha);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar el gasto: " + ex.Message, ex);
            }
        }

        public bool AgregarGasto(string tipoGasto, string nombreGasto, string observaciones, decimal precio)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Gasto_Agregar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@TipoGasto", tipoGasto);
                cmd.Parameters.AddWithValue("@NombreGasto", nombreGasto);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al guardar el gasto: " + ex.Message, ex);
            }
        }

        public DataTable ObtenerGastos(string busqueda = null)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Gasto_ObtenerTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Busqueda", string.IsNullOrWhiteSpace(busqueda)
                    ? DBNull.Value : busqueda.Trim());

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar gastos: " + ex.Message, ex);
            }
        }

        #endregion

        #region CONTABILIDAD - PAGOS

        public decimal? ObtenerTotalOrden(int ordenId)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Orden_ObtenerTotal", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);

                conexion.Abrir();
                object result = cmd.ExecuteScalar();
                return result is not null && result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : null;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener total de orden: " + ex.Message, ex);
            }
        }

        public bool ActualizarPago(int pagoId, string dni, int ordenId, decimal monto)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Pago_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PagoID", pagoId);
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                cmd.Parameters.AddWithValue("@Monto", monto);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar el pago: " + ex.Message, ex);
            }
        }

        public bool RegistrarPago(string clienteDni, int ordenId, decimal monto)
        {
            using var conexion = new ClsConexion();
            try
            {

                using var cmd = new SqlCommand("sp_RegistrarPago", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClienteDNI", clienteDni);
                cmd.Parameters.AddWithValue("@OrdenID", ordenId);
                cmd.Parameters.AddWithValue("@Monto", monto);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al registrar el pago: " + ex.Message, ex);
            }
        }

        public DataRow ObtenerComprobantePago(int pagoId)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Pago_ObtenerComprobante", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PagoID", pagoId);

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener comprobante: " + ex.Message, ex);
            }
        }

        public DataTable ObtenerPagos(string busqueda = null)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Pago_ObtenerTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Busqueda", string.IsNullOrWhiteSpace(busqueda)
                    ? DBNull.Value : busqueda.Trim());

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar pagos: " + ex.Message, ex);
            }
        }

        public DataTable ObtenerOrdenesFinalizadasSinPago(string dni)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Orden_ObtenerFinalizadasSinPago", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar órdenes del cliente: " + ex.Message, ex);
            }
        }

        #endregion

        #region INVENTARIO / PRODUCTOS

        public List<ValidadorInventario> ObtenerProductosInventario()
        {
            using var conexion = new ClsConexion();
            var lista = new List<ValidadorInventario>();
            try
            {
                using var cmd = new SqlCommand("sp_ObtenerProductosInventario", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Busqueda", DBNull.Value);

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new ValidadorInventario
                    {
                        Producto_ID = rd.GetInt32(rd.GetOrdinal("Producto_ID")),
                        Producto_Nombre = rd["Producto_Nombre"].ToString(),
                        Producto_Categoria = rd["Producto_Categoria"].ToString(),
                        Producto_Cantidad_Actual = rd.GetInt32(rd.GetOrdinal("Producto_Cantidad_Actual")),
                        Producto_Precio = rd.GetDecimal(rd.GetOrdinal("Producto_Precio"))
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar productos: " + ex.Message, ex);
            }
        }

        public List<ValidadorInventario> ObtenerProductos()
        {
            using var conexion = new ClsConexion();
            var lista = new List<ValidadorInventario>();
            try
            {
                using var cmd = new SqlCommand("sp_Producto_ObtenerTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new ValidadorInventario
                    {
                        Producto_ID = rd.GetInt32(rd.GetOrdinal("Producto_ID")),
                        Producto_Nombre = rd["Producto_Nombre"].ToString(),
                        Producto_Categoria = rd["Producto_Categoria"].ToString(),
                        Producto_Marca = rd["Producto_Marca"].ToString(),
                        Producto_Modelo = rd["Producto_Modelo"].ToString(),
                        Producto_Cantidad_Actual = rd.GetInt32(rd.GetOrdinal("Producto_Cantidad_Actual")),
                        Producto_Cantidad_Minima = rd.GetInt32(rd.GetOrdinal("Producto_Stock_Minimo")),
                        Producto_Precio = rd.GetDecimal(rd.GetOrdinal("Producto_Precio"))
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar productos: " + ex.Message, ex);
            }
        }

        public bool AgregarProducto(string nombre, string categoria, string marca,
                                     string modelo, decimal precio, int cantidad)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Producto_Agregar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", categoria);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", string.IsNullOrWhiteSpace(modelo)
                    ? DBNull.Value : modelo.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al agregar producto: " + ex.Message, ex);
            }
        }

        public bool ActualizarProducto(int productoId, string nombre, string categoria,
                                        string marca, string modelo, decimal precio, int cantidad)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Producto_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ID", productoId);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Categoria", categoria);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", string.IsNullOrWhiteSpace(modelo)
                    ? DBNull.Value : modelo.Trim());
                cmd.Parameters.AddWithValue("@Precio", precio);
                cmd.Parameters.AddWithValue("@Cantidad", cantidad);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar producto: " + ex.Message, ex);
            }
        }

        #endregion

        #region CLIENTES

        public (string nombres, string apellidos) BuscarNombreCliente(string dni)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_BuscarNombre", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);

                conexion.Abrir();
                using var reader = cmd.ExecuteReader();
                return reader.Read()
                    ? (reader["Cliente_Nombres"].ToString(), reader["Cliente_Apellidos"].ToString())
                    : (null, null);
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al buscar cliente: " + ex.Message, ex);
            }
        }

        public bool AgregarCliente(string dni, string nombres, string apellidos,
                                    string telefono, string email, string direccion)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_Agregar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@Nombres", nombres);
                cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email)
                    ? DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(direccion)
                    ? DBNull.Value : direccion.Trim());

                conexion.Abrir();
                int resultado = Convert.ToInt32(cmd.ExecuteScalar());
                return resultado == 1;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al agregar cliente: " + ex.Message, ex);
            }
        }

        public bool ActualizarCliente(string dniOriginal, string nombres, string apellidos,
                string telefono, string email, string direccion,
                bool activo, string nuevoDni = null)
        {
            using var conexion = new ClsConexion();
            string dniAGuardar = string.IsNullOrEmpty(nuevoDni) ? dniOriginal : nuevoDni;

            try
            {
                using var cmd = new SqlCommand("sp_Cliente_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNIOriginal", dniOriginal);
                cmd.Parameters.AddWithValue("@NuevoDNI", dniAGuardar);
                cmd.Parameters.AddWithValue("@Nombres", nombres);
                cmd.Parameters.AddWithValue("@Apellidos", apellidos);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Email", string.IsNullOrWhiteSpace(email)
                    ? DBNull.Value : email.Trim());
                cmd.Parameters.AddWithValue("@Direccion", string.IsNullOrWhiteSpace(direccion)
                    ? DBNull.Value : direccion.Trim());
                cmd.Parameters.AddWithValue("@Activo", activo ? 1 : 0);

                conexion.Abrir();
                cmd.ExecuteNonQuery();

                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar cliente: " + ex.Message, ex);
            }
        }

        public bool ExisteTelefonoEnOtroCliente(string telefono, string dniActual)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_ExisteTelefonoEnOtro", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@DNIActual", dniActual ?? "");

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al verificar teléfono: " + ex.Message, ex);
            }
        }

        public bool ExisteDNIEnOtroCliente(string dni, string dniActual)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_ExisteDNIEnOtro", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@DNIActual", dniActual ?? "");

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al verificar DNI: " + ex.Message, ex);
            }
        }

        public List<clsCliente> ObtenerClientes()
        {
            using var conexion = new ClsConexion();
            var lista = new List<clsCliente>();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_ObtenerTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new clsCliente
                    {
                        Cliente_DPI = rd["Cliente_DNI"].ToString(),
                        Cliente_Nombre = rd["Cliente_Nombres"].ToString(),
                        Cliente_Apellido = rd["Cliente_Apellidos"].ToString(),
                        Cliente_Telefono = rd["Cliente_TelefonoPrincipal"].ToString(),
                        Cliente_Correo = rd["Cliente_Email"].ToString(),
                        Cliente_Direccion = rd["Cliente_Direccion"].ToString(),
                        Cliente_Activo = rd["Cliente_Activo"] != DBNull.Value && (bool)rd["Cliente_Activo"]
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar clientes: " + ex.Message, ex);
            }
        }

        public (string nombre, bool existe) VerificarClienteDNI(string dni)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_VerificarPorDNI", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);

                conexion.Abrir();
                using var reader = cmd.ExecuteReader();
                return reader.Read() ? (reader["Nombre"].ToString(), true) : (string.Empty, false);
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al consultar el cliente: " + ex.Message, ex);
            }
        }

        public (string nombreCompleto, string telefono, string email,
                string vehiculoNombre, string vehiculoTipo, string vehiculoPlaca,
                bool activo, bool vehiculoActivo) BuscarClientePorDNI(string dni)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_BuscarPorDNI", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@DNI", dni);
                conexion.Abrir();

                using var rd = cmd.ExecuteReader();

                if (!rd.Read()) return default;

                string nombre = rd["NombreCompleto"].ToString()!;
                string telefono = rd["Cliente_TelefonoPrincipal"].ToString()!;
                string email = rd["Cliente_Email"].ToString()!;
                bool activo = rd["Cliente_Activo"] != DBNull.Value && Convert.ToBoolean(rd["Cliente_Activo"]);

                rd.NextResult();
                if (rd.Read())
                {
                    bool vActivo = rd["Vehiculo_Activo"] != DBNull.Value && Convert.ToBoolean(rd["Vehiculo_Activo"]);
                    return (nombre, telefono, email,
                            rd["NombreVehiculo"].ToString()!,
                            rd["TipoAño"].ToString()!,
                            rd["Vehiculo_Placa"].ToString()!,
                            activo, vActivo);
                }

                return (nombre, telefono, email, "", "", "", activo, true);
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al buscar cliente por DNI: " + ex.Message, ex);
            }
        }

        public class ClienteSugerencia
        {
            public string DNI { get; set; } = string.Empty;
            public string NombreCompleto { get; set; } = string.Empty;
        }

        public List<ClienteSugerencia> BuscarClientesPorDNI(string texto)
        {
            using var conexion = new ClsConexion();
            var lista = new List<ClienteSugerencia>();
            try
            {
                using var cmd = new SqlCommand("sp_Cliente_BuscarSugerencias", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Texto", texto);

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new ClienteSugerencia
                    {
                        DNI = rd["Cliente_DNI"].ToString()!,
                        NombreCompleto = rd["NombreCompleto"].ToString()!
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al buscar clientes: " + ex.Message, ex);
            }
        }

        #endregion

        #region VEHÍCULOS

        public List<Vehiculo> ObtenerVehiculos()
        {
            using var conexion = new ClsConexion();
            var lista = new List<Vehiculo>();
            try
            {
                using var cmd = new SqlCommand("sp_Vehiculo_ObtenerTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                conexion.Abrir();
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(new Vehiculo
                    {
                        Vehiculo_Placa = reader["Vehiculo_Placa"].ToString(),
                        Vehiculo_Marca = reader["Vehiculo_Marca"].ToString(),
                        Vehiculo_Modelo = reader["Vehiculo_Modelo"].ToString(),
                        Vehiculo_Año = reader.GetInt32(reader.GetOrdinal("Vehiculo_Año")),
                        Vehiculo_Tipo = reader["Vehiculo_Tipo"].ToString(),
                        Vehiculo_Observaciones = reader["Vehiculo_Observaciones"].ToString(),
                        Cliente_DNI = reader["Cliente_DNI"].ToString(),
                        Cliente_NombreCompleto = reader["Cliente_NombreCompleto"].ToString(),
                        EstaActivo = reader["Vehiculo_Activo"] != DBNull.Value && (bool)reader["Vehiculo_Activo"]
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar vehículos: " + ex.Message, ex);
            }
        }

        public bool ExistePlaca(string placa)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Vehiculo_ExistePlaca", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Placa", placa);

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al verificar placa: " + ex.Message, ex);
            }
        }

        public (string vehiculoNombre, string vehiculoTipo, string clienteDNI,
                string nombreCompleto, string telefono, string email,
                bool activo, bool vehiculoActivo) BuscarVehiculoPorPlaca(string placa)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Vehiculo_BuscarPorPlaca", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Placa", placa);
                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    bool cActivo = rd["Cliente_Activo"] != DBNull.Value && Convert.ToBoolean(rd["Cliente_Activo"]);
                    bool vActivo = rd["Vehiculo_Activo"] != DBNull.Value && Convert.ToBoolean(rd["Vehiculo_Activo"]);
                    return (rd["NombreVehiculo"].ToString()!,
                            rd["TipoAño"].ToString()!,
                            rd["Cliente_DNI"].ToString()!,
                            rd["NombreCompleto"].ToString()!,
                            rd["Cliente_TelefonoPrincipal"].ToString()!,
                            rd["Cliente_Email"].ToString()!,
                            cActivo, vActivo);
                }
                return default;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al buscar vehículo por placa: " + ex.Message, ex);
            }
        }


        public bool AgregarVehiculo(string placa, string dni, string marca, string modelo,
                                     int anio, string tipo, string observaciones)
        {
            using var conexion = new ClsConexion();
            try
            {

                using var cmd = new SqlCommand("sp_RegistrarVehiculo", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Placa", placa);
                cmd.Parameters.AddWithValue("@DNI", dni);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", modelo);
                cmd.Parameters.AddWithValue("@Anio", anio);
                cmd.Parameters.AddWithValue("@Tipo", tipo);
                cmd.Parameters.AddWithValue("@Obs", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al registrar el vehículo: " + ex.Message, ex);
            }
        }

        public bool ActualizarVehiculo(string placaOriginal, string placaNueva, string dni,
                                        string marca, string modelo, int anio, string tipo,
                                        string observaciones, bool activo)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Vehiculo_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@PlacaOriginal", placaOriginal);
                cmd.Parameters.AddWithValue("@PlacaNueva", placaNueva);
                cmd.Parameters.AddWithValue("@Marca", marca);
                cmd.Parameters.AddWithValue("@Modelo", modelo);
                cmd.Parameters.AddWithValue("@Anio", anio);
                cmd.Parameters.AddWithValue("@Tipo", tipo);
                cmd.Parameters.AddWithValue("@Obs", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());
                cmd.Parameters.AddWithValue("@Activo", activo ? 1 : 0);
                cmd.Parameters.AddWithValue("@DNI", dni);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar el vehículo: " + ex.Message, ex);
            }
        }

        #endregion

        #region ÓRDENES DE TRABAJO

        public List<OrdenTrabajo> ObtenerOrdenes()
        {
            using var conexion = new ClsConexion();
            var lista = new List<OrdenTrabajo>();
            try
            {
                using var cmd = new SqlCommand("sp_Orden_ObtenerTodas", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new OrdenTrabajo
                    {
                        Orden_ID = rd.GetInt32(rd.GetOrdinal("Orden_ID")),
                        Cliente_DNI = rd["Cliente_DNI"].ToString(),
                        Cliente_NombreCompleto = rd["Cliente_NombreCompleto"].ToString(),
                        Vehiculo_Placa = rd["Vehiculo_Placa"].ToString(),
                        Producto_Nombre = rd["Producto_Nombre"].ToString(),
                        Producto_Categoria = rd["Producto_Categoria"].ToString(),
                        Estado = rd["Estado"].ToString(),
                        Fecha = rd.GetDateTime(rd.GetOrdinal("Fecha")),
                        Fecha_Entrega = rd["Fecha_Entrega"] != DBNull.Value
                            ? rd.GetDateTime(rd.GetOrdinal("Fecha_Entrega"))
                            : null,
                        Observaciones = rd["Observaciones"].ToString(),
                        Servicio_Precio = rd.GetDecimal(rd.GetOrdinal("Servicio_Precio")),
                        OrdenPrecio_Total = rd.GetDecimal(rd.GetOrdinal("OrdenPrecio_Total"))
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar órdenes: " + ex.Message, ex);
            }
        }

        public (string nombreCompleto, string telefono, string email,
                string vehiculoNombre, string vehiculoTipo, string vehiculoPlaca,
                string clienteDNI, string estado, DateTime fecha, DateTime? fechaEntrega,
                string observaciones, decimal servicioPrecio, decimal ordenTotal,
                string foto) ObtenerOrdenParaEditar(int ordenID)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Orden_ObtenerParaEditar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@OrdenID", ordenID);
                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    return (
                        rd["NombreCompleto"].ToString(),
                        rd["Cliente_TelefonoPrincipal"].ToString(),
                        rd["Cliente_Email"].ToString(),
                        rd["NombreVehiculo"].ToString(),
                        rd["TipoAño"].ToString(),
                        rd["Vehiculo_Placa"].ToString(),
                        rd["Cliente_DNI"].ToString(),
                        rd["Estado"].ToString(),
                        Convert.ToDateTime(rd["Fecha"]),
                        rd["Fecha_Entrega"] as DateTime?,
                        rd["Observaciones"].ToString(),
                        Convert.ToDecimal(rd["Servicio_Precio"]),
                        Convert.ToDecimal(rd["OrdenPrecio_Total"]),
                        rd["Adjuntos_Fotos"]?.ToString() ?? string.Empty
                    );
                }
                return default;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar la orden: " + ex.Message, ex);
            }
        }


        private static DataTable ConstruirTablaRepuestos(List<RepuestoOrden> repuestos)
        {
            var tabla = new DataTable();
            tabla.Columns.Add("ProductoID", typeof(int));
            tabla.Columns.Add("Cantidad", typeof(int));
            tabla.Columns.Add("Incluido", typeof(bool));

            foreach (var r in repuestos)
                tabla.Rows.Add(r.ProductoID, r.Cantidad, r.Incluido);

            return tabla;
        }

        public int AgregarOrden(string clienteDNI, string placa, int? productoID, string estado,
                          DateTime fecha, DateTime? fechaEntrega, string observaciones,
                          decimal precioServicio, decimal total, string foto,
                          List<RepuestoOrden> repuestos)
        {
            using var conexion = new ClsConexion();
            try
            {

                using var cmd = new SqlCommand("sp_Orden_Agregar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ClienteDNI", clienteDNI);
                cmd.Parameters.AddWithValue("@Placa", placa);
                cmd.Parameters.AddWithValue("@ProductoID", productoID.HasValue ? productoID.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Estado", estado);
                cmd.Parameters.AddWithValue("@Fecha", fecha);
                cmd.Parameters.AddWithValue("@FechaEntrega", fechaEntrega.HasValue ? fechaEntrega.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());
                cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                cmd.Parameters.AddWithValue("@Total", total);
                cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(foto) ? DBNull.Value : foto);

                var tvp = cmd.Parameters.AddWithValue("@Repuestos", ConstruirTablaRepuestos(repuestos));
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "TipoRepuestoOrden";

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al agregar la orden: " + ex.Message, ex);
            }
        }

        public bool ActualizarOrden(int ordenID, string estado, DateTime fecha,
              DateTime? fechaEntrega, string observaciones,
              decimal precioServicio, decimal total, string foto,
              List<RepuestoOrden> repuestos)
        {
            using var conexion = new ClsConexion();
            try
            {

                using var cmd = new SqlCommand("sp_Orden_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@OrdenID", ordenID);
                cmd.Parameters.AddWithValue("@Estado", estado);
                cmd.Parameters.AddWithValue("@Fecha", fecha);
                cmd.Parameters.AddWithValue("@FechaEntrega", fechaEntrega.HasValue ? fechaEntrega.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(observaciones)
                    ? DBNull.Value : observaciones.Trim());
                cmd.Parameters.AddWithValue("@ServicioPrecio", precioServicio);
                cmd.Parameters.AddWithValue("@Total", total);
                cmd.Parameters.AddWithValue("@Foto", string.IsNullOrEmpty(foto) ? DBNull.Value : foto);

                var tvp = cmd.Parameters.AddWithValue("@Repuestos", ConstruirTablaRepuestos(repuestos));
                tvp.SqlDbType = SqlDbType.Structured;
                tvp.TypeName = "TipoRepuestoOrden";

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar la orden: " + ex.Message, ex);
            }
        }

        public List<RepuestoOrden> ObtenerRepuestosOrden(int ordenID)
        {
            using var conexion = new ClsConexion();
            var lista = new List<RepuestoOrden>();
            try
            {
                using var cmd = new SqlCommand("sp_Orden_ObtenerRepuestos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@OrdenID", ordenID);

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                int numero = 1;
                while (rd.Read())
                {
                    lista.Add(new RepuestoOrden
                    {
                        Numero = numero++,
                        ProductoID = Convert.ToInt32(rd["Producto_ID"]),
                        Nombre = rd["Repuesto_Nombre"].ToString(),
                        Cantidad = Convert.ToInt32(rd["Repuesto_Cantidad"]),
                        Precio = Convert.ToDecimal(rd["Repuesto_Precio"]),
                        Incluido = true
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar repuestos de la orden: " + ex.Message, ex);
            }
        }

        #endregion

        #region DASHBOARD Y GRÁFICAS

        public (List<OrdenReciente> ordenes, decimal balanceTotal, decimal gastosTotal) ObtenerDatosDashboard()
        {
            using var conexion = new ClsConexion();
            var ordenes = new List<OrdenReciente>();
            try
            {
                const string query = @"
                    SELECT TOP 10
                        o.Orden_ID,
                        c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS Cliente_NombreCompleto,
                        o.Vehiculo_Placa, o.Fecha, o.Estado, o.OrdenPrecio_Total
                    FROM Orden_Trabajo o
                    INNER JOIN Cliente c ON o.Cliente_DNI = c.Cliente_DNI
                    ORDER BY o.Fecha DESC";

                conexion.Abrir();

                using (var cmd = new SqlCommand(query, conexion.SqlC))
                using (var rd = cmd.ExecuteReader())
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

                decimal balanceTotal, gastosTotal;
                using (var cmd2 = new SqlCommand(
                    "SELECT ISNULL(SUM(OrdenPrecio_Total), 0) FROM Orden_Trabajo", conexion.SqlC))
                    balanceTotal = Convert.ToDecimal(cmd2.ExecuteScalar());

                using (var cmd3 = new SqlCommand(
                    "SELECT ISNULL(SUM(Precio_Gasto), 0) FROM Contabilidad_Gastos", conexion.SqlC))
                    gastosTotal = Convert.ToDecimal(cmd3.ExecuteScalar());

                return (ordenes, balanceTotal, gastosTotal);
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar datos del dashboard: " + ex.Message, ex);
            }
        }

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaOrdenes(DateTime fechaDesde)
            => ObtenerDatosGraficaMensual(
                "SELECT YEAR(Fecha) AS Anio, MONTH(Fecha) AS Mes, SUM(OrdenPrecio_Total) AS Valor " +
                "FROM Orden_Trabajo WHERE Fecha >= @Desde GROUP BY YEAR(Fecha), MONTH(Fecha) ORDER BY Anio, Mes",
                fechaDesde, "Error al cargar gráfica de órdenes");

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaCantidadOrdenes(DateTime fechaDesde)
            => ObtenerDatosGraficaMensual(
                "SELECT YEAR(Fecha) AS Anio, MONTH(Fecha) AS Mes, COUNT(*) AS Valor " +
                "FROM Orden_Trabajo WHERE Fecha >= @Desde GROUP BY YEAR(Fecha), MONTH(Fecha) ORDER BY Anio, Mes",
                fechaDesde, "Error al cargar gráfica de cantidad de órdenes");

        public (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaGastos(DateTime fechaDesde)
            => ObtenerDatosGraficaMensual(
                "SELECT YEAR(Fecha_Gasto) AS Anio, MONTH(Fecha_Gasto) AS Mes, SUM(Precio_Gasto) AS Valor " +
                "FROM Contabilidad_Gastos WHERE Fecha_Gasto >= @Desde GROUP BY YEAR(Fecha_Gasto), MONTH(Fecha_Gasto) ORDER BY Anio, Mes",
                fechaDesde, "Error al cargar gráfica de gastos");

        /// <summary>
        /// Helper compartido por las 3 gráficas mensuales (órdenes, cantidad de órdenes y gastos),
        /// que solo se diferenciaban en el query. Evita triplicar la misma lógica de lectura.
        /// </summary>
        private static (List<double> valores, List<string> etiquetas) ObtenerDatosGraficaMensual(
            string query, DateTime fechaDesde, string mensajeError)
        {
            using var conexion = new ClsConexion();
            var vals = new List<double>();
            var labels = new List<string>();
            try
            {
                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Desde", fechaDesde);
                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    vals.Add(Convert.ToDouble(rd["Valor"]));
                    labels.Add($"{Convert.ToInt32(rd["Mes"]):D2}/{Convert.ToInt32(rd["Anio"])}");
                }
                return (vals, labels);
            }
            catch (SqlException ex)
            {
                throw new Exception(mensajeError + ": " + ex.Message, ex);
            }
        }

        #endregion

        #region NOTIFICACIONES

        public int ContarNotificacionesPendientes()
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Notificacion_ContarPendientes", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                conexion.Abrir();
                return (int)cmd.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al contar notificaciones: " + ex.Message, ex);
            }
        }

        public DataTable ObtenerNotificacionesPendientes()
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Notificacion_ObtenerPendientes", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar notificaciones pendientes: " + ex.Message, ex);
            }
        }

        public List<NotificacionItem> ObtenerTodasNotificaciones()
        {
            using var conexion = new ClsConexion();
            var lista = new List<NotificacionItem>();
            try
            {
                using var cmd = new SqlCommand("sp_Notificacion_ObtenerTodas", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
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
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar todas las notificaciones: " + ex.Message, ex);
            }
        }

        public void MarcarNotificacionLeida(int? id)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Notificacion_MarcarLeida", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@ID", id.HasValue ? id.Value : DBNull.Value);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al marcar notificación como leída: " + ex.Message, ex);
            }
        }

        #endregion

        #region LOGIN Y SEGURIDAD

        private static string CalcularHashSha512(string texto)
        {
            using var sha = System.Security.Cryptography.SHA512.Create();
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(texto));
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public bool ValidarLogin(string correo, string contrasena)
        {
            using var conexion = new ClsConexion();
            try
            {
                string inputHash = CalcularHashSha512(contrasena);

                const string query = @"
                    SELECT COUNT(1) FROM LOGIN 
                    WHERE Usuario_Email = @Correo 
                    AND Usuario_Contraseña = @Hash";

                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@Hash", inputHash);

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al validar login: " + ex.Message, ex);
            }
        }

        public bool ExisteCorreoLogin(string correo)
        {
            using var conexion = new ClsConexion();
            try
            {
                const string query = "SELECT COUNT(1) FROM LOGIN WHERE Usuario_Email = @Correo";
                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);

                conexion.Abrir();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al verificar correo: " + ex.Message, ex);
            }
        }

        public bool ActualizarContrasenaLogin(string correo, string nuevaContrasena)
        {
            using var conexion = new ClsConexion();
            try
            {
                string hash = CalcularHashSha512(nuevaContrasena);

                const string query = "UPDATE LOGIN SET Usuario_Contraseña = @Hash WHERE Usuario_Email = @Correo";
                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Hash", hash);
                cmd.Parameters.AddWithValue("@Correo", correo);

                conexion.Abrir();
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar contraseña: " + ex.Message, ex);
            }
        }

        public int ObtenerIntentosFallidos(string correo)
        {
            using var conexion = new ClsConexion();
            try
            {
                const string query = "SELECT ISNULL(IntentosFallidos, 0) FROM LOGIN WHERE Usuario_Email = @Correo";
                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);

                conexion.Abrir();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener intentos fallidos: " + ex.Message, ex);
            }
        }

        public DateTime? ObtenerFechaBloqueo(string correo)
        {
            using var conexion = new ClsConexion();
            try
            {
                const string query = "SELECT FechaBloqueo FROM LOGIN WHERE Usuario_Email = @Correo";
                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Correo", correo);

                conexion.Abrir();
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value ? Convert.ToDateTime(result) : null;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener fecha de bloqueo: " + ex.Message, ex);
            }
        }

        public void ActualizarBloqueo(string correo, int intentos, DateTime? fechaBloqueo)
        {
            using var conexion = new ClsConexion();
            try
            {
                const string query = @"
                    UPDATE LOGIN 
                    SET IntentosFallidos = @Intentos,
                        FechaBloqueo = @FechaBloqueo
                    WHERE Usuario_Email = @Correo";

                using var cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@Intentos", intentos);
                cmd.Parameters.AddWithValue("@FechaBloqueo", fechaBloqueo.HasValue ? fechaBloqueo.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@Correo", correo);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar bloqueo: " + ex.Message, ex);
            }
        }

        #endregion

        #region CÓDIGOS OTP

        public string GenerarCodigoOTP(string correo)
        {
            using var conexion = new ClsConexion();
            string codigo = Random.Shared.Next(100000, 999999).ToString();
            DateTime expiracion = DateTime.UtcNow.AddMinutes(5);

            try
            {
                conexion.Abrir();

                using (var cmdInvalidar = new SqlCommand(
                    "UPDATE CodigosOTP SET Usado = 1 WHERE Correo = @Correo AND Usado = 0", conexion.SqlC))
                {
                    cmdInvalidar.Parameters.AddWithValue("@Correo", correo);
                    cmdInvalidar.ExecuteNonQuery();
                }

                const string insertar = @"
                    INSERT INTO CodigosOTP (Correo, Codigo, FechaExpiracion, Usado, Intentos)
                    VALUES (@Correo, @Codigo, @Expiracion, 0, 0)";

                using var cmdInsertar = new SqlCommand(insertar, conexion.SqlC);
                cmdInsertar.Parameters.AddWithValue("@Correo", correo);
                cmdInsertar.Parameters.AddWithValue("@Codigo", codigo);
                cmdInsertar.Parameters.AddWithValue("@Expiracion", expiracion);
                cmdInsertar.ExecuteNonQuery();

                return codigo;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al generar el código OTP: " + ex.Message, ex);
            }
        }

        public bool ValidarCodigoOTP(string correo, string codigoIngresado)
        {
            using var conexion = new ClsConexion();
            try
            {
                conexion.Abrir();

                const string query = @"
                    SELECT Id FROM CodigosOTP 
                    WHERE Correo = @Correo 
                    AND Codigo = @Codigo 
                    AND Usado = 0 
                    AND FechaExpiracion > GETUTCDATE()
                    AND Intentos < 3";

                int? idEncontrado = null;
                using (var cmd = new SqlCommand(query, conexion.SqlC))
                {
                    cmd.Parameters.AddWithValue("@Correo", correo);
                    cmd.Parameters.AddWithValue("@Codigo", codigoIngresado);

                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                        idEncontrado = reader.GetInt32(0);
                }

                if (idEncontrado.HasValue)
                {
                    using var cmdUpdate = new SqlCommand(
                        "UPDATE CodigosOTP SET Usado = 1 WHERE Id = @Id", conexion.SqlC);
                    cmdUpdate.Parameters.AddWithValue("@Id", idEncontrado.Value);
                    cmdUpdate.ExecuteNonQuery();
                    return true;
                }

                using (var cmdIntentos = new SqlCommand(
                    "UPDATE CodigosOTP SET Intentos = Intentos + 1 WHERE Correo = @Correo AND Usado = 0", conexion.SqlC))
                {
                    cmdIntentos.Parameters.AddWithValue("@Correo", correo);
                    cmdIntentos.ExecuteNonQuery();
                }

                return false;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al validar el código OTP: " + ex.Message, ex);
            }
        }

        #endregion

        #region CORREO (SMTP)

        /// <summary>
        /// ⚠️ Las credenciales SMTP están hardcodeadas más abajo. Se recomienda moverlas
        /// a App.config junto con la cadena de conexión (ver claves EmailRemitente / EmailClave
        /// en connectionStrings o appSettings) para no exponerlas en el código fuente.
        /// </summary>
        public bool EnviarCorreoOTP(string correoDestino, string codigo)
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
                smtp.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                smtp.Authenticate("tallermecanicoind26@gmail.com", "igzy ooxe fmjr ippx");
                smtp.Send(mensaje);
                smtp.Disconnect(true);

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al enviar el correo: " + ex.Message, ex);
            }
        }

        #endregion

        #region USUARIOS

        public DataTable ObtenerUsuarios(string busqueda = null)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_ListarTodos", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Busqueda", string.IsNullOrWhiteSpace(busqueda)
                    ? DBNull.Value : busqueda.Trim());

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener usuarios: " + ex.Message, ex);
            }
        }

        public DataRow ObtenerUsuarioPorEmail(string email)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_ObtenerPorEmail", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);

                conexion.Abrir();
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al obtener el usuario: " + ex.Message, ex);
            }
        }

        public bool AgregarUsuario(string nombre, string apellido, string email,
                                    string telefono, string rol, string contrasenaPlano)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_Insertar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Apellido", apellido);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Rol", rol);
                cmd.Parameters.AddWithValue("@Contrasena", contrasenaPlano);
                cmd.Parameters.AddWithValue("@Activo", true);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al agregar usuario: " + ex.Message, ex);
            }
        }

        public bool ActualizarUsuario(string email, string nombre, string apellido,
                                       string telefono, string rol)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_Actualizar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Nombre", nombre);
                cmd.Parameters.AddWithValue("@Apellido", apellido);
                cmd.Parameters.AddWithValue("@Telefono", telefono);
                cmd.Parameters.AddWithValue("@Rol", rol);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al actualizar usuario: " + ex.Message, ex);
            }
        }

        public bool CambiarContrasenaUsuario(string email, string nuevaContrasenaPlano)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_CambiarContrasena", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@NuevaContrasena", nuevaContrasenaPlano);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cambiar contraseña: " + ex.Message, ex);
            }
        }

        public bool CambiarEstadoUsuario(string email, bool activo)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_CambiarEstado", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Activo", activo);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cambiar estado del usuario: " + ex.Message, ex);
            }
        }

        public bool GuardarBiometria(string email, byte[] foto)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_RegistrarBiometria", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Foto", foto);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al guardar biometría: " + ex.Message, ex);
            }
        }

        public bool EliminarBiometria(string email)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Usuario_EliminarBiometria", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);

                conexion.Abrir();
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al eliminar biometría: " + ex.Message, ex);
            }
        }

        #endregion

        #region BITÁCORA

        public List<BitacoraItem> ObtenerBitacora()
        {
            using var conexion = new ClsConexion();
            var lista = new List<BitacoraItem>();
            try
            {
                using var cmd = new SqlCommand("sp_Bitacora_ObtenerTodas", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                conexion.Abrir();
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                {
                    lista.Add(new BitacoraItem
                    {
                        Bitacora_Fecha = Convert.ToDateTime(rd["Bitacora_Fecha"]),
                        Bitacora_Usuario = rd["Bitacora_Usuario"].ToString() ?? string.Empty,
                        Bitacora_Rol = rd["Bitacora_Rol"].ToString() ?? string.Empty,
                        Bitacora_Modulo = rd["Bitacora_Modulo"].ToString() ?? string.Empty,
                        Bitacora_Accion = rd["Bitacora_Accion"].ToString() ?? string.Empty,
                        Bitacora_Descripcion = rd["Bitacora_Descripcion"].ToString() ?? string.Empty
                    });
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar bitácora: " + ex.Message, ex);
            }
        }


        public void RegistrarBitacora(string email, string modulo, string accion, string descripcion = null)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("sp_Bitacora_Registrar", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@Modulo", modulo);
                cmd.Parameters.AddWithValue("@Accion", accion);
                cmd.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(descripcion)
                    ? DBNull.Value : descripcion.Trim());

                conexion.Abrir();
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al registrar en bitácora: " + ex.Message, ex);
            }
        }

        #region RECONOCIMIENTO FACIAL

        /// <summary>Datos crudos de una persona registrada para reconocimiento facial.</summary>
        public sealed record PersonaReconocimiento(int Id, string Nombre, string Email, byte[] Foto);

        public List<PersonaReconocimiento> ObtenerPersonasReconocimiento()
        {
            using var conexion = new ClsConexion();
            var lista = new List<PersonaReconocimiento>();
            try
            {
                using var cmd = new SqlCommand("PA_RF_ObtenerTodasLasPersonas", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };

                AbrirOThrow(conexion);
                using var rd = cmd.ExecuteReader();

                int colId = rd.GetOrdinal("Id");
                int colNombre = rd.GetOrdinal("Nombre");
                int colEmail = rd.GetOrdinal("Usuario_Email");
                int colFoto = rd.GetOrdinal("Foto");

                while (rd.Read())
                {
                    if (rd.IsDBNull(colId) || rd.IsDBNull(colNombre) || rd.IsDBNull(colFoto))
                        continue; // fila incompleta; se descarta

                    string nombre = rd.GetString(colNombre);
                    byte[] foto = (byte[])rd[colFoto];

                    if (string.IsNullOrWhiteSpace(nombre) || foto.Length == 0)
                        continue;

                    lista.Add(new PersonaReconocimiento(
                        rd.GetInt32(colId),
                        nombre,
                        rd.IsDBNull(colEmail) ? string.Empty : rd.GetString(colEmail),
                        foto));
                }
                return lista;
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al cargar personas de reconocimiento facial: " + ex.Message, ex);
            }
        }

        public void RegistrarIntentoFallidoReconocimiento(string correo, int labelDetectado, double distancia)
        {
            using var conexion = new ClsConexion();
            try
            {
                using var cmd = new SqlCommand("PA_RF_RegistrarIntentoFallido", conexion.SqlC)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@LabelDetectado", labelDetectado);
                cmd.Parameters.AddWithValue("@Distancia", distancia);

                AbrirOThrow(conexion);
                cmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al registrar intento fallido de reconocimiento: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Abre la conexión SQL y lanza una excepción clara si falla.
        /// Usado por los métodos de reconocimiento facial, que necesitan
        /// diferenciar un fallo de conexión de un fallo de lectura de datos.
        /// </summary>
        private static void AbrirOThrow(ClsConexion conexion)
        {
            try
            {
                conexion.Abrir();
            }
            catch (SqlException ex)
            {
                throw new Exception("Error al conectar con la base de datos: " + ex.Message, ex);
            }
        }


        #endregion
        #endregion
    }

}