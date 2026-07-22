using Microsoft.Data.SqlClient;
using NBomber.Contracts;
using NBomber.CSharp;

string connectionString = "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;User ID=DayanaSosa;Password=Taller2026;Authentication=SqlPassword;";

// Helper para no repetir código en cada escenario
ScenarioProps CrearEscenario(string nombre, string query) =>
    Scenario.Create(nombre, async context =>
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
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

// Recepción
var scenarioClientes = CrearEscenario("limite_clientes", "SELECT TOP 10 * FROM Cliente");
var scenarioVehiculos = CrearEscenario("limite_vehiculos", "SELECT TOP 10 * FROM Vehiculo");

// Mecánicos
var scenarioOrdenes = CrearEscenario("limite_ordenes", "SELECT TOP 10 * FROM Orden_Trabajo");

// Inventario
var scenarioProductos = CrearEscenario("limite_productos", "SELECT TOP 10 * FROM Producto");
var scenarioRepuestos = CrearEscenario("limite_repuestos", "SELECT TOP 10 * FROM Orden_Repuesto");

// Contabilidad
var scenarioPagos = CrearEscenario("limite_pagos", "SELECT TOP 10 * FROM Contabilidad_Pago");
var scenarioGastos = CrearEscenario("limite_gastos", "SELECT TOP 10 * FROM Contabilidad_Gastos");

NBomberRunner
    .RegisterScenarios(
        scenarioClientes,
        scenarioVehiculos,
        scenarioOrdenes,
        scenarioProductos,
        scenarioRepuestos,
        scenarioPagos,
        scenarioGastos
    )
    .Run();