using Login.Clases;
using Microsoft.Win32;
using SelectPdf;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Login
{
    public partial class ReportesWindow : Window
    {
        private string _modulo;

        public ReportesWindow(string modulo)
        {
            InitializeComponent();
            _modulo = modulo;
            txtModulo.Text = $"Reporte de {modulo}";
        }

        private void BtnGenerarPDF_Click(object sender, RoutedEventArgs e)
        {
            switch (_modulo)
            {
                case "Clientes":
                    GenerarReporteClientes();
                    break;
                case "Inventario":
                    GenerarReporteInventario();
                    break;
                case "Vehiculos":
                    GenerarReporteVehiculos();
                    break;
                case "Ordenes":
                    GenerarReporteOrdenes();
                    break;
                case "Egresos":
                    GenerarReporteEgresos();
                    break;
                case "Ingresos":
                    GenerarReporteIngresos();
                    break;
            }
        }

        private void GenerarReporteClientes()
        {
            string html = "<h2 style='color:#1565C0'>Reporte de Clientes</h2>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#1565C0;color:white'><th>DNI</th><th>Nombres</th><th>Apellidos</th><th>Teléfono</th><th>Email</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = "SELECT Cliente_DNI, Cliente_Nombres, Cliente_Apellidos, Cliente_TelefonoPrincipal, Cliente_Email FROM Cliente ORDER BY Cliente_Apellidos";

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Cliente_DNI"]}</td>";
                    html += $"<td>{r["Cliente_Nombres"]}</td>";
                    html += $"<td>{r["Cliente_Apellidos"]}</td>";
                    html += $"<td>{r["Cliente_TelefonoPrincipal"]}</td>";
                    html += $"<td>{r["Cliente_Email"]}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();
            html += "</table>";

            ExportarPDF(html, "Reporte_Clientes");
        }

        private void GenerarReporteInventario()
        {
            string html = "<h2 style='color:#1565C0'>Reporte de Inventario</h2>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#1565C0;color:white'><th>Nombre</th><th>Categoría</th><th>Marca</th><th>Modelo</th><th>Precio</th><th>Cantidad</th><th>Stock Mínimo</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = "SELECT Producto_Nombre, Producto_Categoria, Producto_Marca, Producto_Modelo, Producto_Precio, Producto_Cantidad_Actual, Producto_Stock_Minimo FROM Producto ORDER BY Producto_Nombre";

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Producto_Nombre"]}</td>";
                    html += $"<td>{r["Producto_Categoria"]}</td>";
                    html += $"<td>{r["Producto_Marca"]}</td>";
                    html += $"<td>{r["Producto_Modelo"]}</td>";
                    html += $"<td>L {r["Producto_Precio"]:N2}</td>";
                    html += $"<td>{r["Producto_Cantidad_Actual"]}</td>";
                    html += $"<td>{r["Producto_Stock_Minimo"]}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();
            html += "</table>";

            ExportarPDF(html, "Reporte_Inventario");
        }

        private void GenerarReporteVehiculos()
        {
            string html = "<h2 style='color:#1565C0'>Reporte de Vehículos</h2>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#1565C0;color:white'><th>Placa</th><th>Marca</th><th>Modelo</th><th>Año</th><th>Tipo</th><th>Cliente DNI</th><th>Observaciones</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = "SELECT Vehiculo_Placa, Vehiculo_Marca, Vehiculo_Modelo, Vehiculo_Año, Vehiculo_Tipo, Cliente_DNI, Vehiculo_Observaciones FROM Vehiculo ORDER BY Vehiculo_Marca";

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Vehiculo_Placa"]}</td>";
                    html += $"<td>{r["Vehiculo_Marca"]}</td>";
                    html += $"<td>{r["Vehiculo_Modelo"]}</td>";
                    html += $"<td>{r["Vehiculo_Año"]}</td>";
                    html += $"<td>{r["Vehiculo_Tipo"]}</td>";
                    html += $"<td>{r["Cliente_DNI"]}</td>";
                    html += $"<td>{(r["Vehiculo_Observaciones"] == DBNull.Value ? "" : r["Vehiculo_Observaciones"])}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();
            html += "</table>";

            ExportarPDF(html, "Reporte_Vehiculos");
        }

        private void GenerarReporteOrdenes()
        {
            string html = "<h2 style='color:#1565C0'>Reporte de Órdenes de Trabajo</h2>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#1565C0;color:white'><th>ID</th><th>Cliente DNI</th><th>Placa</th><th>Estado</th><th>Fecha</th><th>Fecha Entrega</th><th>Precio Servicio</th><th>Total</th><th>Observaciones</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = @"SELECT Orden_ID, Cliente_DNI, Vehiculo_Placa, Estado, 
                          Fecha, Fecha_Entrega, Servicio_Precio, 
                          OrdenPrecio_Total, Observaciones 
                   FROM Orden_Trabajo 
                   ORDER BY Fecha DESC";

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Orden_ID"]}</td>";
                    html += $"<td>{r["Cliente_DNI"]}</td>";
                    html += $"<td>{r["Vehiculo_Placa"]}</td>";
                    html += $"<td>{r["Estado"]}</td>";
                    html += $"<td>{Convert.ToDateTime(r["Fecha"]):dd/MM/yyyy}</td>";
                    html += $"<td>{(r["Fecha_Entrega"] == DBNull.Value ? "-" : Convert.ToDateTime(r["Fecha_Entrega"]).ToString("dd/MM/yyyy"))}</td>";
                    html += $"<td>L {r["Servicio_Precio"]:N2}</td>";
                    html += $"<td>L {r["OrdenPrecio_Total"]:N2}</td>";
                    html += $"<td>{(r["Observaciones"] == DBNull.Value ? "" : r["Observaciones"])}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();
            html += "</table>";

            ExportarPDF(html, "Reporte_Ordenes");
        }

        private void GenerarReporteEgresos()
        {
            string html = "<h2 style='color:#B71C1C'>Reporte de Egresos</h2>";
            html += $"<p>Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}</p>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#B71C1C;color:white'><th>Tipo</th><th>Nombre</th><th>Observaciones</th><th>Precio</th><th>Fecha</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = "SELECT Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto FROM Contabilidad_Gastos ORDER BY Fecha_Gasto DESC";

            decimal total = 0;

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    decimal precio = r["Precio_Gasto"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Gasto"]);
                    total += precio;

                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Tipo_Gasto"]}</td>";
                    html += $"<td>{r["Nombre_Gasto"]}</td>";
                    html += $"<td>{(r["Observaciones_Gasto"] == DBNull.Value ? "" : r["Observaciones_Gasto"])}</td>";
                    html += $"<td>L {precio:N2}</td>";
                    html += $"<td>{Convert.ToDateTime(r["Fecha_Gasto"]):dd/MM/yyyy}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();

            html += $"<tr style='background:#B71C1C;color:white;font-weight:bold'>";
            html += $"<td colspan='3'>TOTAL</td><td>L {total:N2}</td><td></td></tr>";
            html += "</table>";

            ExportarPDF(html, "Reporte_Egresos");
        }

        private void GenerarReporteIngresos()
        {
            string html = "<h2 style='color:#1B5E20'>Reporte de Ingresos</h2>";
            html += $"<p>Generado el: {DateTime.Now:dd/MM/yyyy HH:mm}</p>";
            html += "<table border='1' cellpadding='6' width='100%' style='border-collapse:collapse'>";
            html += "<tr style='background:#1B5E20;color:white'><th>Pago ID</th><th>Cliente</th><th>DNI</th><th>Orden ID</th><th>Monto</th></tr>";

            var db = new clsConexion();
            db.Abrir();

            string sql = @"
        SELECT p.Pago_ID, 
               c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
               p.Cliente_DNI,
               p.Orden_ID,
               p.Precio_Pago
        FROM Contabilidad_Pago p
        INNER JOIN Cliente c ON p.Cliente_DNI = c.Cliente_DNI
        ORDER BY p.Pago_ID DESC";

            decimal total = 0;

            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
            {
                bool alterno = false;
                while (r.Read())
                {
                    string bg = alterno ? "#f5f5f5" : "#ffffff";
                    decimal monto = r["Precio_Pago"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Pago"]);
                    total += monto;

                    html += $"<tr style='background:{bg}'>";
                    html += $"<td>{r["Pago_ID"]}</td>";
                    html += $"<td>{r["NombreCompleto"]}</td>";
                    html += $"<td>{r["Cliente_DNI"]}</td>";
                    html += $"<td>{r["Orden_ID"]}</td>";
                    html += $"<td>L {monto:N2}</td>";
                    html += "</tr>";
                    alterno = !alterno;
                }
            }
            db.Cerrar();

            html += $"<tr style='background:#1B5E20;color:white;font-weight:bold'>";
            html += $"<td colspan='4'>TOTAL</td><td>L {total:N2}</td></tr>";
            html += "</table>";

            ExportarPDF(html, "Reporte_Ingresos");
        }

        private void ExportarPDF(string html, string nombreArchivo)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"{nombreArchivo}_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                HtmlToPdf converter = new HtmlToPdf();
                PdfDocument pdf = converter.ConvertHtmlString(html);
                pdf.Save(dialog.FileName);
                pdf.Close();

                MessageBox.Show("✅ Reporte generado!", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });
            }
        }
    }
}