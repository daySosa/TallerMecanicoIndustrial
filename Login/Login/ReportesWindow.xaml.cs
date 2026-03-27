using Login.Clases;
using Microsoft.Win32;
using SelectPdf;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Windows;

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
                case "Clientes": GenerarReporteClientes(); break;
                case "Inventario": GenerarReporteInventario(); break;
                case "Vehiculos": GenerarReporteVehiculos(); break;
                case "Ordenes": GenerarReporteOrdenes(); break;
                case "Egresos": GenerarReporteEgresos(); break;
                case "Ingresos": GenerarReporteIngresos(); break;
            }
        }

        private string GetLogoBase64()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Imagenes/OSM_LOGO.png", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(uri);
                if (streamInfo != null)
                {
                    using (var ms = new MemoryStream())
                    {
                        streamInfo.Stream.CopyTo(ms);
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch { }

            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(exeDir, "Imagenes", "OSM_LOGO.png");
                if (File.Exists(path))
                    return Convert.ToBase64String(File.ReadAllBytes(path));
            }
            catch { }

            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 5; i++)
                {
                    string candidate = Path.Combine(dir, "Imagenes", "OSM_LOGO.png");
                    if (File.Exists(candidate))
                        return Convert.ToBase64String(File.ReadAllBytes(candidate));
                    dir = Path.GetDirectoryName(dir);
                    if (dir == null) break;
                }
            }
            catch { }

            return string.Empty;
        }

        private string GetBaseStyles(string accentColor = "#1e2d5f")
        {
            return $@"
            <style>
                * {{ margin:0; padding:0; box-sizing:border-box; }}
                body {{ font-family: Arial, sans-serif; font-size:11px; color:#222; background:#fff; }}

                .header {{
                    background:{accentColor};
                    color:white;
                    padding:16px 24px;
                    display:flex;
                    align-items:center;
                    justify-content:space-between;
                }}
                .header-left {{ display:flex; align-items:center; gap:20px; }}
                .logo-circle {{
                    width:120px; height:120px; border-radius:50%;
                    background:rgba(255,255,255,0.15);
                    display:flex; align-items:center; justify-content:center;
                    font-size:28px; font-weight:bold; letter-spacing:1px;
                    overflow:hidden;
                    flex-shrink:0;
                }}
                .logo-circle img {{
                    width:120px; height:120px;
                    border-radius:50%;
                    object-fit:cover;
                }}
                .company-name {{ font-size:22px; font-weight:bold; }}
                .company-sub  {{ font-size:11px; opacity:0.85; margin-top:4px; }}
                .header-right {{
                    display:flex; gap:0;
                    border-left:1px solid rgba(255,255,255,0.3);
                }}
                .header-meta {{
                    padding:0 20px;
                    border-right:1px solid rgba(255,255,255,0.3);
                    text-align:center;
                }}
                .header-meta:last-child {{ border-right:none; }}
                .meta-label {{ font-size:9px; text-transform:uppercase; opacity:0.75; letter-spacing:1px; }}
                .meta-value {{ font-size:13px; font-weight:bold; margin-top:4px; }}

                .title-bar {{
                    background:#f0f2f5;
                    padding:10px 24px;
                    display:flex;
                    align-items:center;
                    gap:10px;
                    border-bottom:2px solid {accentColor};
                }}
                .report-title {{ font-size:15px; font-weight:bold; color:{accentColor}; }}

                .content {{ padding:16px 24px; }}
                table {{
                    width:100%;
                    border-collapse:collapse;
                    margin-top:8px;
                    font-size:11px;
                    table-layout:fixed;
                }}
                thead tr {{ background:{accentColor}; color:white; }}
                thead th {{
                    padding:9px 8px;
                    text-align:center;
                    font-weight:600;
                    font-size:10px;
                    text-transform:uppercase;
                    letter-spacing:0.5px;
                    white-space:nowrap;
                    overflow:hidden;
                }}
                tbody tr {{ border-bottom:1px solid #e8eaf0; }}
                tbody tr:nth-child(even) {{ background:#f7f8fc; }}
                tbody td {{
                    padding:7px 8px;
                    text-align:center;
                    white-space:nowrap;
                    overflow:hidden;
                    text-overflow:ellipsis;
                }}
                tbody td.left {{ text-align:left; }}
                tbody td.obs {{
                    text-align:left;
                    white-space:normal;
                    word-break:break-word;
                }}

                .badge-pagado    {{ background:#e8f5e9; color:#2e7d32; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-pendiente {{ background:#fff8e1; color:#f57f17; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-proceso   {{ background:#e3f2fd; color:#1565c0; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-cancelado {{ background:#fce4ec; color:#c62828; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-sinempezar{{ background:#f3e5f5; color:#6a1b9a; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-activo    {{ background:#e8f5e9; color:#2e7d32; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}
                .badge-inactivo  {{ background:#fce4ec; color:#c62828; padding:2px 8px; border-radius:10px; font-size:10px; font-weight:bold; white-space:nowrap; display:inline-block; }}

                .total-row td {{
                    background:{accentColor};
                    color:white;
                    font-weight:bold;
                    padding:10px 8px;
                    font-size:12px;
                    white-space:nowrap;
                    text-align:center;
                }}

                .footer {{
                    margin-top:24px;
                    padding:10px 24px;
                    border-top:1px solid #ddd;
                    display:flex;
                    justify-content:space-between;
                    font-size:10px;
                    color:#888;
                }}
            </style>";
        }

        private string GetHeader(string accentColor, string periodo = "")
        {
            string periodoTexto = string.IsNullOrEmpty(periodo)
                ? $"Q{((DateTime.Now.Month - 1) / 3 + 1)} {DateTime.Now.Year}"
                : periodo;

            string logoBase64 = GetLogoBase64();
            string logoHtml = !string.IsNullOrEmpty(logoBase64)
                ? $"<img src='data:image/png;base64,{logoBase64}' />"
                : "TM";

            return $@"
            <div class='header'>
                <div class='header-left'>
                    <div class='logo-circle'>{logoHtml}</div>
                    <div>
                        <div class='company-name'>Taller Mecánico AutoExpress</div>
                        <div class='company-sub'>Tegucigalpa, Honduras &nbsp;·&nbsp; Tel: +504 2230-0000</div>
                    </div>
                </div>
                <div class='header-right'>
                    <div class='header-meta'>
                        <div class='meta-label'>Fecha Emitida</div>
                        <div class='meta-value'>{DateTime.Now:dd/MM/yyyy}</div>
                    </div>
                    <div class='header-meta'>
                        <div class='meta-label'>Período</div>
                        <div class='meta-value'>{periodoTexto}</div>
                    </div>
                    <div class='header-meta'>
                        <div class='meta-label'>Fuente</div>
                        <div class='meta-value'>TallerDB</div>
                    </div>
                </div>
            </div>";
        }

        private string GetTitleBar(string accentColor, string titulo)
        {
            return $@"
            <div class='title-bar'>
                <span class='report-title'>{titulo}</span>
            </div>";
        }

        private string GetFooter()
        {
            return $@"
            <div class='footer'>
                <span>Taller Mecánico AutoExpress &nbsp;·&nbsp; Documento confidencial</span>
                <span>Generado automáticamente: {DateTime.Now:dd/MM/yyyy} — {DateTime.Now:hh:mm tt}</span>
                <span>Página 1 de 1</span>
            </div>";
        }

        private string BadgeEstado(string estado)
        {
            return estado switch
            {
                "Finalizado" => $"<span class='badge-pagado'>{estado}</span>",
                "En Proceso" => $"<span class='badge-proceso'>{estado}</span>",
                "Pendiente" => $"<span class='badge-pendiente'>{estado}</span>",
                "Cancelado" => $"<span class='badge-cancelado'>{estado}</span>",
                "Sin Empezar" => $"<span class='badge-sinempezar'>{estado}</span>",
                _ => $"<span>{estado}</span>"
            };
        }

        private void GenerarReporteClientes()
        {
            string accent = "#1e2d5f";
            var db = new clsConexion(); db.Abrir();
            string sql = "SELECT Cliente_DNI, Cliente_Nombres, Cliente_Apellidos, Cliente_TelefonoPrincipal, Cliente_Email FROM Cliente ORDER BY Cliente_Apellidos";

            string filas = "";
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                    filas += $@"<tr>
                        <td>{r["Cliente_DNI"]}</td>
                        <td class='left'>{r["Cliente_Nombres"]}</td>
                        <td class='left'>{r["Cliente_Apellidos"]}</td>
                        <td>{r["Cliente_TelefonoPrincipal"]}</td>
                        <td class='left'>{r["Cliente_Email"]}</td>
                    </tr>";
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Clientes")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:16%'/>
                        <col style='width:20%'/>
                        <col style='width:20%'/>
                        <col style='width:14%'/>
                        <col style='width:30%'/>
                    </colgroup>
                    <thead><tr>
                        <th>DNI</th><th>Nombres</th><th>Apellidos</th><th>Teléfono</th><th>Email</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Clientes", landscape: false);
        }

        private void GenerarReporteInventario()
        {
            string accent = "#1e2d5f";
            var db = new clsConexion(); db.Abrir();
            string sql = "SELECT Producto_Nombre, Producto_Categoria, Producto_Marca, Producto_Modelo, Producto_Precio, Producto_Cantidad_Actual, Producto_Stock_Minimo FROM Producto ORDER BY Producto_Nombre";

            string filas = "";
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                {
                    int cant = Convert.ToInt32(r["Producto_Cantidad_Actual"]);
                    int min = Convert.ToInt32(r["Producto_Stock_Minimo"]);
                    string stockBadge = cant <= min
                        ? $"<span class='badge-cancelado'>{cant}</span>"
                        : $"<span class='badge-pagado'>{cant}</span>";

                    filas += $@"<tr>
                        <td class='left'>{r["Producto_Nombre"]}</td>
                        <td class='left'>{r["Producto_Categoria"]}</td>
                        <td>{r["Producto_Marca"]}</td>
                        <td>{r["Producto_Modelo"]}</td>
                        <td>L {Convert.ToDecimal(r["Producto_Precio"]):N2}</td>
                        <td>{stockBadge}</td>
                        <td>{min}</td>
                    </tr>";
                }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Inventario")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:22%'/>
                        <col style='width:15%'/>
                        <col style='width:13%'/>
                        <col style='width:13%'/>
                        <col style='width:12%'/>
                        <col style='width:13%'/>
                        <col style='width:12%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Nombre</th><th>Categoría</th><th>Marca</th><th>Modelo</th><th>Precio</th><th>Stock Actual</th><th>Stock Mín.</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Inventario", landscape: false);
        }

        private void GenerarReporteVehiculos()
        {
            string accent = "#1e2d5f";
            var db = new clsConexion(); db.Abrir();
            string sql = "SELECT Vehiculo_Placa, Vehiculo_Marca, Vehiculo_Modelo, Vehiculo_Año, Vehiculo_Tipo, Cliente_DNI, Vehiculo_Activo, Vehiculo_Observaciones FROM Vehiculo ORDER BY Vehiculo_Marca";

            string filas = "";
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                {
                    bool activo = r["Vehiculo_Activo"] != DBNull.Value && (bool)r["Vehiculo_Activo"];
                    string estadoBadge = activo
                        ? "<span class='badge-activo'>Activo</span>"
                        : "<span class='badge-inactivo'>Inactivo</span>";

                    filas += $@"<tr>
                        <td><b>{r["Vehiculo_Placa"]}</b></td>
                        <td>{r["Vehiculo_Marca"]}</td>
                        <td>{r["Vehiculo_Modelo"]}</td>
                        <td>{r["Vehiculo_Año"]}</td>
                        <td>{r["Vehiculo_Tipo"]}</td>
                        <td>{r["Cliente_DNI"]}</td>
                        <td>{estadoBadge}</td>
                        <td class='obs'>{(r["Vehiculo_Observaciones"] == DBNull.Value ? "—" : r["Vehiculo_Observaciones"])}</td>
                    </tr>";
                }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Vehículos")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:10%'/>
                        <col style='width:10%'/>
                        <col style='width:14%'/>
                        <col style='width:6%'/>
                        <col style='width:10%'/>
                        <col style='width:14%'/>
                        <col style='width:9%'/>
                        <col style='width:27%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Placa</th><th>Marca</th><th>Modelo</th><th>Año</th><th>Tipo</th><th>Cliente DNI</th><th>Estado</th><th>Observaciones</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Vehiculos", landscape: true);
        }

        private void GenerarReporteOrdenes()
        {
            string accent = "#1e2d5f";
            var db = new clsConexion(); db.Abrir();
            string sql = @"SELECT Orden_ID, Cliente_DNI, Vehiculo_Placa, Estado,
                                  Fecha, Fecha_Entrega, Servicio_Precio,
                                  OrdenPrecio_Total, Observaciones
                           FROM Orden_Trabajo ORDER BY Fecha DESC";

            string filas = "";
            decimal grandTotal = 0;
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                {
                    decimal total = Convert.ToDecimal(r["OrdenPrecio_Total"]);
                    grandTotal += total;
                    filas += $@"<tr>
                        <td><b>{r["Orden_ID"]}</b></td>
                        <td>{r["Cliente_DNI"]}</td>
                        <td>{r["Vehiculo_Placa"]}</td>
                        <td>{BadgeEstado(r["Estado"].ToString())}</td>
                        <td>{Convert.ToDateTime(r["Fecha"]):dd/MM/yyyy}</td>
                        <td>{(r["Fecha_Entrega"] == DBNull.Value ? "—" : Convert.ToDateTime(r["Fecha_Entrega"]).ToString("dd/MM/yyyy"))}</td>
                        <td>L {Convert.ToDecimal(r["Servicio_Precio"]):N2}</td>
                        <td>L {total:N2}</td>
                        <td class='obs'>{(r["Observaciones"] == DBNull.Value ? "—" : r["Observaciones"])}</td>
                    </tr>";
                }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Órdenes de Trabajo")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:4%'/>
                        <col style='width:14%'/>
                        <col style='width:10%'/>
                        <col style='width:11%'/>
                        <col style='width:9%'/>
                        <col style='width:9%'/>
                        <col style='width:10%'/>
                        <col style='width:10%'/>
                        <col style='width:23%'/>
                    </colgroup>
                    <thead><tr>
                        <th>ID</th><th>Cliente DNI</th><th>Placa</th><th>Estado</th>
                        <th>Fecha</th><th>Entrega</th><th>Servicio</th><th>Total</th><th>Observaciones</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='7'>TOTAL GENERAL</td>
                            <td>L {grandTotal:N2}</td>
                            <td></td>
                        </tr>
                    </tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Ordenes", landscape: true);
        }

        private void GenerarReporteEgresos()
        {
            string accent = "#7f1d1d";
            var db = new clsConexion(); db.Abrir();
            string sql = "SELECT Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto FROM Contabilidad_Gastos ORDER BY Fecha_Gasto DESC";

            string filas = "";
            decimal total = 0;
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                {
                    decimal precio = r["Precio_Gasto"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Gasto"]);
                    total += precio;
                    filas += $@"<tr>
                        <td class='left'>{r["Tipo_Gasto"]}</td>
                        <td class='left'>{r["Nombre_Gasto"]}</td>
                        <td class='obs'>{(r["Observaciones_Gasto"] == DBNull.Value ? "—" : r["Observaciones_Gasto"])}</td>
                        <td>L {precio:N2}</td>
                        <td>{Convert.ToDateTime(r["Fecha_Gasto"]):dd/MM/yyyy}</td>
                    </tr>";
                }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Egresos")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:15%'/>
                        <col style='width:20%'/>
                        <col style='width:40%'/>
                        <col style='width:13%'/>
                        <col style='width:12%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Tipo</th><th>Nombre</th><th>Observaciones</th><th>Precio</th><th>Fecha</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='3'>TOTAL GENERAL</td>
                            <td>L {total:N2}</td>
                            <td></td>
                        </tr>
                    </tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Egresos", landscape: false);
        }

        private void GenerarReporteIngresos()
        {
            string accent = "#1b4332";
            var db = new clsConexion(); db.Abrir();
            string sql = @"
                SELECT p.Pago_ID,
                       c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                       p.Cliente_DNI, p.Orden_ID, p.Precio_Pago
                FROM Contabilidad_Pago p
                INNER JOIN Cliente c ON p.Cliente_DNI = c.Cliente_DNI
                ORDER BY p.Pago_ID DESC";

            string filas = "";
            decimal total = 0;
            using (SqlCommand cmd = new SqlCommand(sql, db.SqlC))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                {
                    decimal monto = r["Precio_Pago"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Pago"]);
                    total += monto;
                    filas += $@"<tr>
                        <td><b>{r["Pago_ID"]}</b></td>
                        <td class='left'>{r["NombreCompleto"]}</td>
                        <td>{r["Cliente_DNI"]}</td>
                        <td>{r["Orden_ID"]}</td>
                        <td>L {monto:N2}</td>
                        <td><span class='badge-pagado'>Pagado</span></td>
                    </tr>";
                }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent)}
                {GetTitleBar(accent, "Reporte de Ingresos")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:10%'/>
                        <col style='width:30%'/>
                        <col style='width:20%'/>
                        <col style='width:15%'/>
                        <col style='width:15%'/>
                        <col style='width:10%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Pago ID</th><th>Cliente</th><th>DNI</th><th>Orden ID</th><th>Monto</th><th>Estado</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='4'>TOTAL GENERAL</td>
                            <td>L {total:N2}</td>
                            <td></td>
                        </tr>
                    </tbody>
                </table>
                </div>
                {GetFooter()}
            </body></html>";

            ExportarPDF(html, "Reporte_Ingresos", landscape: false);
        }

        private void ExportarPDF(string html, string nombreArchivo, bool landscape = false)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"{nombreArchivo}_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                HtmlToPdf converter = new HtmlToPdf();
                converter.Options.PdfPageSize = PdfPageSize.Letter;
                converter.Options.PdfPageOrientation = landscape
                    ? PdfPageOrientation.Landscape
                    : PdfPageOrientation.Portrait;
                converter.Options.MarginTop = 10;
                converter.Options.MarginBottom = 10;
                converter.Options.MarginLeft = 10;
                converter.Options.MarginRight = 10;

                PdfDocument pdf = converter.ConvertHtmlString(html);
                pdf.Save(dialog.FileName);
                pdf.Close();

                MessageBox.Show("✅ Reporte generado correctamente.", "Éxito",
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