using NBomber.CSharp;
using NBomber.Contracts;
using Microsoft.Data.SqlClient;
using System;

string connectionString = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Taller2026;Authentication=SqlPassword;";

var scenarioFlujoCompleto = Scenario.Create("limite_flujo_completo_orden", async context =>
{
    try
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction();

        // Datos únicos por cada "usuario simulado" para no chocar con otros hilos
        string sufijo = Guid.NewGuid().ToString("N").Substring(0, 8);
        string dni = "T" + sufijo;                 // <= 13 caracteres
        string placa = "P" + sufijo;                // <= 20 caracteres
        string email = "prueba_" + sufijo + "@nbomber.test";

        // 1. Recepción: alta de cliente
        using (var cmdCliente = new SqlCommand(
            @"INSERT INTO Cliente (Cliente_DNI, Cliente_Nombres, Cliente_Apellidos, Cliente_TelefonoPrincipal, Cliente_Email, Cliente_Direccion, Cliente_Activo)
              VALUES (@dni, 'Prueba', 'NBomber', '00000000', @email, 'N/A', 1)", connection, transaction))
        {
            cmdCliente.Parameters.AddWithValue("@dni", dni);
            cmdCliente.Parameters.AddWithValue("@email", email);
            await cmdCliente.ExecuteNonQueryAsync();
        }

        // 2. Recepción: alta de vehículo asociado
        using (var cmdVehiculo = new SqlCommand(
            @"INSERT INTO Vehiculo (Vehiculo_Placa, Cliente_DNI, Vehiculo_Marca, Vehiculo_Modelo, Vehiculo_Año, Vehiculo_Tipo, Vehiculo_Activo)
              VALUES (@placa, @dni, 'Marca Prueba', 'Modelo Prueba', 2024, 'Turismo', 1)", connection, transaction))
        {
            cmdVehiculo.Parameters.AddWithValue("@placa", placa);
            cmdVehiculo.Parameters.AddWithValue("@dni", dni);
            await cmdVehiculo.ExecuteNonQueryAsync();
        }

        // 3. Inventario: se toma un producto existente para la orden
        int productoId;
        decimal productoPrecio;
        using (var cmdProducto = new SqlCommand("SELECT TOP 1 Producto_ID, Producto_Precio FROM Producto", connection, transaction))
        using (var reader = await cmdProducto.ExecuteReaderAsync())
        {
            await reader.ReadAsync();
            productoId = reader.GetInt32(0);
            productoPrecio = reader.GetDecimal(1);
        }

        // 4. Mecánicos: se crea la orden de trabajo
        int ordenId;
        using (var cmdOrden = new SqlCommand(
            @"INSERT INTO Orden_Trabajo (Cliente_DNI, Vehiculo_Placa, Producto_ID, Estado, Fecha, Servicio_Precio, OrdenPrecio_Total)
              OUTPUT INSERTED.Orden_ID
              VALUES (@dni, @placa, @productoId, 'Sin Empezar', GETDATE(), @precio, @precio)", connection, transaction))
        {
            cmdOrden.Parameters.AddWithValue("@dni", dni);
            cmdOrden.Parameters.AddWithValue("@placa", placa);
            cmdOrden.Parameters.AddWithValue("@productoId", productoId);
            cmdOrden.Parameters.AddWithValue("@precio", productoPrecio);
            ordenId = (int)await cmdOrden.ExecuteScalarAsync();
        }

        // 5. Inventario: se asocia el repuesto usado a la orden
        using (var cmdRepuesto = new SqlCommand(
            @"INSERT INTO Orden_Repuesto (Orden_ID, Producto_ID, Repuesto_Nombre, Repuesto_Cantidad, Repuesto_Precio, OrdenPrecio_Total)
              VALUES (@ordenId, @productoId, 'Repuesto Prueba', 1, @precio, @precio)", connection, transaction))
        {
            cmdRepuesto.Parameters.AddWithValue("@ordenId", ordenId);
            cmdRepuesto.Parameters.AddWithValue("@productoId", productoId);
            cmdRepuesto.Parameters.AddWithValue("@precio", productoPrecio);
            await cmdRepuesto.ExecuteNonQueryAsync();
        }

        // 6. Contabilidad: se registra el pago de la orden
        using (var cmdPago = new SqlCommand(
            @"INSERT INTO Contabilidad_Pago (Precio_Pago, Cliente_DNI, Orden_ID, Fecha_Pago)
              VALUES (@precio, @dni, @ordenId, GETDATE())", connection, transaction))
        {
            cmdPago.Parameters.AddWithValue("@precio", productoPrecio);
            cmdPago.Parameters.AddWithValue("@dni", dni);
            cmdPago.Parameters.AddWithValue("@ordenId", ordenId);
            await cmdPago.ExecuteNonQueryAsync();
        }

        // Revertimos todo: nada queda guardado en la base real
        transaction.Rollback();

        return Response.Ok();
    }
    catch (Exception ex)
    {
        return Response.Fail(message: ex.Message);
    }
})
.WithLoadSimulations(
    Simulation.Inject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromSeconds(30)),
    Simulation.RampingInject(rate: 1000, interval: TimeSpan.FromSeconds(10), during: TimeSpan.FromMinutes(1)),
    Simulation.RampingInject(rate: 1500, interval: TimeSpan.FromSeconds(10), during: TimeSpan.FromMinutes(1)),
    Simulation.RampingInject(rate: 2000, interval: TimeSpan.FromSeconds(10), during: TimeSpan.FromMinutes(1))
);

NBomberRunner
    .RegisterScenarios(scenarioFlujoCompleto)
    .Run();