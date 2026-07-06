using Login.Clases;
using Microsoft.Data.SqlClient;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;

namespace Login
{
    public partial class ReportesWindow : Window
    {
        private string _modulo;

        // Cultura fija en español (Honduras) para que los reportes nunca
        // salgan con nombres de días/meses en inglés, sin importar el
        // idioma del sistema operativo del usuario.
        private static readonly CultureInfo CulturaES = new CultureInfo("es-HN");

        // ─────────────────────────────────────────────
        //  DATOS DEL NEGOCIO
        // ─────────────────────────────────────────────
        private const string NombreNegocio = "OSM";
        private const string DireccionNegocio = "Col. 21 de Febrero, Bloque 5, Calle Principal, dos cuadras antes del Cementerio Santa Anita";
        private const string TelefonoNegocio = "9575-9819"; // Ajusta el formato si hace falta

        public ReportesWindow(string modulo)
        {
            InitializeComponent();
            _modulo = modulo;
            txtModulo.Text = $"Reporte de {modulo}";
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => this.Close();

        private async void BtnGenerarPDF_Click(object sender, RoutedEventArgs e)
        {
            switch (_modulo)
            {
                case "Clientes": await GenerarReporteClientes(); break;
                case "Inventario": await GenerarReporteInventario(); break;
                case "Vehiculos": await GenerarReporteVehiculos(); break;
                case "Ordenes": await GenerarReporteOrdenes(); break;
                case "Egresos": await GenerarReporteEgresos(); break;
                case "Ingresos": await GenerarReporteIngresos(); break;
            }
        }

        // ─────────────────────────────────────────────
        //  HELPERS DE FECHA EN ESPAÑOL
        // ─────────────────────────────────────────────
        private static string FormatearFechaEs(DateTime fecha, string formato)
        {
            string texto = fecha.ToString(formato, CulturaES);
            return string.IsNullOrEmpty(texto)
                ? texto
                : char.ToUpper(texto[0], CulturaES) + texto.Substring(1);
        }

        // ─────────────────────────────────────────────
        //  LOGO
        // ─────────────────────────────────────────────
        private string GetLogoBase64()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Imagenes/OSM_LOGO.png", UriKind.Absolute);
                var si = System.Windows.Application.GetResourceStream(uri);
                if (si != null)
                {
                    using var ms = new MemoryStream();
                    si.Stream.CopyTo(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch { }

            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(exeDir, "Imagenes", "OSM_LOGO.png");
                if (File.Exists(path)) return Convert.ToBase64String(File.ReadAllBytes(path));
            }
            catch { }

            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 5; i++)
                {
                    string candidate = Path.Combine(dir, "Imagenes", "OSM_LOGO.png");
                    if (File.Exists(candidate)) return Convert.ToBase64String(File.ReadAllBytes(candidate));
                    dir = Path.GetDirectoryName(dir);
                    if (dir == null) break;
                }
            }
            catch { }

            return string.Empty;
        }

        // ─────────────────────────────────────────────
        //  ESTILOS BASE
        // ─────────────────────────────────────────────
        private string GetBaseStyles(string accentColor = "#1e2d5f")
        {
            return $@"
            <style>
                * {{ margin:0; padding:0; box-sizing:border-box; }}
                body {{ font-family: Arial, sans-serif; font-size:11px; color:#1a1a2e; background:#fff; }}
                .header {{
                    background: linear-gradient(135deg, {accentColor} 0%, {AdjustColor(accentColor)} 100%);
                    color: white; padding: 20px 28px;
                    display: flex; align-items: center; justify-content: space-between;
                }}
                .header-left {{ display:flex; align-items:center; gap:18px; }}
                .logo-wrap {{
                    width:72px; height:72px; border-radius:50%;
                    background:rgba(255,255,255,0.15);
                    border:2px solid rgba(255,255,255,0.4);
                    display:flex; align-items:center; justify-content:center;
                    font-size:20px; font-weight:700; overflow:hidden; flex-shrink:0;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.25);
                }}
                .logo-wrap img {{ width:72px; height:72px; border-radius:50%; object-fit:cover; }}
                .company-name {{ font-size:20px; font-weight:700; }}
                .company-sub  {{ font-size:10px; opacity:0.80; margin-top:3px; }}
                .header-right {{ display:flex; align-items:center; }}
                .header-meta {{ padding: 6px 18px; text-align:center; border-left:1px solid rgba(255,255,255,0.2); }}
                .meta-label {{ font-size:8px; text-transform:uppercase; opacity:0.70; letter-spacing:1.2px; }}
                .meta-value {{ font-size:13px; font-weight:700; margin-top:3px; }}
                .title-bar {{
                    background:#f4f6fb; padding:10px 28px;
                    display:flex; align-items:center; justify-content:space-between;
                    border-bottom:3px solid {accentColor};
                }}
                .report-title {{ font-size:14px; font-weight:700; color:{accentColor}; }}
                .report-subtitle {{ font-size:10px; color:#888; }}
                .content {{ padding:18px 28px; }}
                table {{
                    width:100%; border-collapse:collapse; margin-top:10px;
                    font-size:10.5px; table-layout:fixed;
                    box-shadow: 0 1px 4px rgba(0,0,0,0.08);
                }}
                thead tr {{ background:{accentColor}; color:white; }}
                thead th {{
                    padding:10px 9px; text-align:center; font-weight:700;
                    font-size:9.5px; text-transform:uppercase; letter-spacing:0.7px;
                }}
                tbody tr {{ border-bottom:1px solid #eceef5; }}
                tbody tr:nth-child(even) {{ background:#f8f9fd; }}
                tbody tr:hover {{ background:#eef1fa; }}
                tbody td {{
                    padding:8px 9px; text-align:center;
                    white-space:nowrap; overflow:hidden;
                    text-overflow:ellipsis; color:#2c2c3e;
                }}
                tbody td.left {{ text-align:left; }}
                tbody td.obs {{ text-align:left; white-space:normal; word-break:break-word; color:#555; }}
                .badge {{
                    padding:2px 9px; border-radius:20px; font-size:9.5px;
                    font-weight:700; display:inline-block;
                }}
                .badge-pagado    {{ background:#d1fae5; color:#065f46; }}
                .badge-pendiente {{ background:#fef3c7; color:#92400e; }}
                .badge-proceso   {{ background:#dbeafe; color:#1e40af; }}
                .badge-cancelado {{ background:#fee2e2; color:#991b1b; }}
                .badge-sinempezar{{ background:#ede9fe; color:#5b21b6; }}
                .badge-activo    {{ background:#d1fae5; color:#065f46; }}
                .badge-inactivo  {{ background:#fee2e2; color:#991b1b; }}
                .total-row td {{
                    background:{accentColor}; color:white; font-weight:700;
                    padding:10px 9px; font-size:11px; text-align:center;
                }}
                .footer {{
                    margin-top:20px; padding:10px 28px;
                    border-top:1px solid #e0e3ef;
                    display:flex; justify-content:space-between;
                    align-items:center; font-size:9px; color:#aaa;
                }}
                .footer-brand {{ font-weight:700; color:#999; font-size:9.5px; }}
            </style>";
        }

        private string AdjustColor(string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                int r = Math.Min(255, Convert.ToInt32(hex.Substring(0, 2), 16) + 40);
                int g = Math.Min(255, Convert.ToInt32(hex.Substring(2, 2), 16) + 40);
                int b = Math.Min(255, Convert.ToInt32(hex.Substring(4, 2), 16) + 40);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch { return hex; }
        }

        // ─────────────────────────────────────────────
        //  HEADER / TITLE / FOOTER
        // ─────────────────────────────────────────────
        private string GetHeader(string accentColor, string periodo = "")
        {
            string periodoTexto = string.IsNullOrEmpty(periodo)
                ? $"Q{((DateTime.Now.Month - 1) / 3 + 1)} {DateTime.Now.Year}" : periodo;

            string logoBase64 = GetLogoBase64();
            string logoHtml = !string.IsNullOrEmpty(logoBase64)
                ? $"<img src='data:image/png;base64,{logoBase64}' />" : "OSM";

            return $@"
            <div class='header'>
                <div class='header-left'>
                    <div class='logo-wrap'>{logoHtml}</div>
                    <div>
                        <div class='company-name'>{NombreNegocio}</div>
                        <div class='company-sub'>{DireccionNegocio} · Tel: {TelefonoNegocio}</div>
                    </div>
                </div>
                <div class='header-right'>
                    <div class='header-meta'>
                        <div class='meta-label'>Fecha Emitida</div>
                        <div class='meta-value'>{DateTime.Now.ToString("dd/MM/yyyy", CulturaES)}</div>
                    </div>
                    <div class='header-meta'>
                        <div class='meta-label'>Período</div>
                        <div class='meta-value'>{periodoTexto}</div>
                    </div>
                    <div class='header-meta'>
                        <div class='meta-label'>Hora</div>
                        <div class='meta-value'>{DateTime.Now.ToString("hh:mm tt", CulturaES)}</div>
                    </div>
                </div>
            </div>";
        }

        private string GetTitleBar(string accentColor, string titulo, string subtitulo = "")
        {
            string sub = string.IsNullOrEmpty(subtitulo)
                ? $"Generado el {FormatearFechaEs(DateTime.Now, "dddd, dd 'de' MMMM 'de' yyyy")}"
                : subtitulo;
            return $@"
            <div class='title-bar'>
                <span class='report-title'>{titulo}</span>
                <span class='report-subtitle'>{sub}</span>
            </div>";
        }

        private string GetFooter() => $@"
            <div class='footer'>
                <span class='footer-brand'>{NombreNegocio}</span>
                <span>Documento confidencial · Uso interno</span>
                <span>Generado: {DateTime.Now.ToString("dd/MM/yyyy", CulturaES)} {DateTime.Now.ToString("hh:mm tt", CulturaES)}</span>
            </div>";

        private string BadgeEstado(string estado) => estado switch
        {
            "Finalizado" => $"<span class='badge badge-pagado'>{estado}</span>",
            "En Proceso" => $"<span class='badge badge-proceso'>{estado}</span>",
            "Pendiente" => $"<span class='badge badge-pendiente'>{estado}</span>",
            "Cancelado" => $"<span class='badge badge-cancelado'>{estado}</span>",
            "Sin Empezar" => $"<span class='badge badge-sinempezar'>{estado}</span>",
            _ => $"<span class='badge'>{estado}</span>"
        };

        // ─────────────────────────────────────────────
        //  EXPORTAR PDF
        // ─────────────────────────────────────────────
        private async Task ExportarPDF(string html, string nombreArchivo, bool landscape = false)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"{nombreArchivo}_{DateTime.Now:yyyyMMdd}"
            };
            if (dialog.ShowDialog() != true) return;

            WebView2 webView = null;
            Window hiddenWindow = null;
            try
            {
                webView = new WebView2();
                hiddenWindow = new Window
                {
                    Width = 1,
                    Height = 1,
                    ShowInTaskbar = false,
                    WindowStyle = WindowStyle.None,
                    Content = webView,
                    Owner = this
                };
                hiddenWindow.Show();
                hiddenWindow.Hide();

                await webView.EnsureCoreWebView2Async();

                var tcs = new TaskCompletionSource<bool>();
                webView.CoreWebView2.NavigationCompleted += (s, e) => tcs.TrySetResult(e.IsSuccess);
                webView.CoreWebView2.NavigateToString(html);
                await tcs.Task;

                var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.Orientation = landscape
                    ? CoreWebView2PrintOrientation.Landscape
                    : CoreWebView2PrintOrientation.Portrait;

                bool success = await webView.CoreWebView2.PrintToPdfAsync(dialog.FileName, printSettings);
                if (!success) throw new Exception("No se pudo generar el PDF.");

                hiddenWindow.Close();
                hiddenWindow = null;

                MessageBox.Show("✅ Reporte generado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al generar el PDF:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { hiddenWindow?.Close(); }
        }

        // ─────────────────────────────────────────────
        //  SELECTOR DE PERÍODO (Semanal / Mensual / Anual / Todo)
        // ─────────────────────────────────────────────
        private bool ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto)
        {
            fechaInicio = DateTime.MinValue;
            fechaFin = DateTime.MaxValue;
            periodoTexto = "Todo";

            var ventana = new Window
            {
                Title = "Seleccionar Período",
                Width = 380,
                Height = 340,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(30, 45, 95))
            };

            var panelPrincipal = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            panelPrincipal.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Tipo de reporte:",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var tipoPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };

            var rbSemanal = new System.Windows.Controls.RadioButton
            {
                Content = "Semanal",
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                IsChecked = true,
                GroupName = "tipoReporte"
            };
            var rbMensual = new System.Windows.Controls.RadioButton
            {
                Content = "Mensual",
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                GroupName = "tipoReporte"
            };
            var rbAnual = new System.Windows.Controls.RadioButton
            {
                Content = "Anual",
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                GroupName = "tipoReporte"
            };
            var rbTodo = new System.Windows.Controls.RadioButton
            {
                Content = "Todo",
                Foreground = System.Windows.Media.Brushes.White,
                GroupName = "tipoReporte"
            };
            tipoPanel.Children.Add(rbSemanal);
            tipoPanel.Children.Add(rbMensual);
            tipoPanel.Children.Add(rbAnual);
            tipoPanel.Children.Add(rbTodo);
            panelPrincipal.Children.Add(tipoPanel);

            // ---- Panel Semanal ----
            var panelSemanal = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            panelSemanal.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona un día dentro de la semana deseada:",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 11
            });
            var dpSemana = new System.Windows.Controls.DatePicker
            {
                SelectedDate = DateTime.Today,
                FirstDayOfWeek = DayOfWeek.Monday
            };
            panelSemanal.Children.Add(dpSemana);

            // ---- Panel Mensual ----
            var panelMensual = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 14),
                Visibility = Visibility.Collapsed
            };
            panelMensual.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona el mes y el año:",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 11
            });
            var mesAnioPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            var cmbMes = new System.Windows.Controls.ComboBox { Width = 150, Margin = new Thickness(0, 0, 8, 0) };
            foreach (var nombreMes in CulturaES.DateTimeFormat.MonthNames)
                if (!string.IsNullOrEmpty(nombreMes))
                    cmbMes.Items.Add(char.ToUpper(nombreMes[0], CulturaES) + nombreMes.Substring(1));
            cmbMes.SelectedIndex = DateTime.Today.Month - 1;
            var cmbAnioMes = new System.Windows.Controls.ComboBox { Width = 90 };
            int anioActual = DateTime.Today.Year;
            for (int y = anioActual; y >= anioActual - 10; y--) cmbAnioMes.Items.Add(y);
            cmbAnioMes.SelectedIndex = 0;
            mesAnioPanel.Children.Add(cmbMes);
            mesAnioPanel.Children.Add(cmbAnioMes);
            panelMensual.Children.Add(mesAnioPanel);

            // ---- Panel Anual ----
            var panelAnual = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 14),
                Visibility = Visibility.Collapsed
            };
            panelAnual.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona el año:",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 11
            });
            var cmbAnioSolo = new System.Windows.Controls.ComboBox { Width = 90, HorizontalAlignment = HorizontalAlignment.Left };
            for (int y = anioActual; y >= anioActual - 10; y--) cmbAnioSolo.Items.Add(y);
            cmbAnioSolo.SelectedIndex = 0;
            panelAnual.Children.Add(cmbAnioSolo);

            panelPrincipal.Children.Add(panelSemanal);
            panelPrincipal.Children.Add(panelMensual);
            panelPrincipal.Children.Add(panelAnual);

            // ---- Alternar paneles según el tipo elegido ----
            void ActualizarVisibilidad()
            {
                panelSemanal.Visibility = rbSemanal.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                panelMensual.Visibility = rbMensual.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
                panelAnual.Visibility = rbAnual.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            }
            rbSemanal.Checked += (s, e) => ActualizarVisibilidad();
            rbMensual.Checked += (s, e) => ActualizarVisibilidad();
            rbAnual.Checked += (s, e) => ActualizarVisibilidad();
            rbTodo.Checked += (s, e) => ActualizarVisibilidad();

            // ---- Botones ----
            bool confirmado = false;
            var btnPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var btnAceptar = new System.Windows.Controls.Button
            {
                Content = "Generar",
                Width = 90,
                Height = 32,
                Margin = new Thickness(0, 0, 8, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 45, 95)),
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnAceptar.Click += (s, e) => { confirmado = true; ventana.Close(); };

            var btnCancelarPeriodo = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 80,
                Height = 32,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancelarPeriodo.Click += (s, e) => ventana.Close();

            btnPanel.Children.Add(btnAceptar);
            btnPanel.Children.Add(btnCancelarPeriodo);
            panelPrincipal.Children.Add(btnPanel);

            ventana.Content = panelPrincipal;
            ventana.ShowDialog();

            if (!confirmado) return false;

            if (rbSemanal.IsChecked == true)
            {
                DateTime fechaBase = dpSemana.SelectedDate ?? DateTime.Today;
                int diasDesdeLunes = ((int)fechaBase.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
                fechaInicio = fechaBase.AddDays(-diasDesdeLunes);
                fechaFin = fechaInicio.AddDays(7).AddTicks(-1);
                periodoTexto = $"Semana del {FormatearFechaEs(fechaInicio, "dd MMM")} al {FormatearFechaEs(fechaInicio.AddDays(6), "dd MMM yyyy")}";
            }
            else if (rbMensual.IsChecked == true)
            {
                int mes = cmbMes.SelectedIndex + 1;
                int anio = (int)cmbAnioMes.SelectedItem;
                fechaInicio = new DateTime(anio, mes, 1);
                fechaFin = fechaInicio.AddMonths(1).AddTicks(-1);
                periodoTexto = FormatearFechaEs(fechaInicio, "MMMM yyyy");
            }
            else if (rbAnual.IsChecked == true)
            {
                int anio = (int)cmbAnioSolo.SelectedItem;
                fechaInicio = new DateTime(anio, 1, 1);
                fechaFin = new DateTime(anio, 12, 31, 23, 59, 59);
                periodoTexto = anio.ToString();
            }
            else
            {
                fechaInicio = DateTime.MinValue;
                fechaFin = DateTime.MaxValue;
                periodoTexto = "Todo";
            }

            return true;
        }

        // ─────────────────────────────────────────────
        //  REPORTE CLIENTES
        // ─────────────────────────────────────────────
        private async Task GenerarReporteClientes()
        {
            string accent = "#1e2d5f";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";
            using (var cmd = new SqlCommand(
                @"SELECT Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                         Cliente_TelefonoPrincipal, Cliente_Email
                  FROM Cliente ORDER BY Cliente_Apellidos", db.SqlC))
            using (var r = cmd.ExecuteReader())
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
                        <col style='width:16%'/><col style='width:20%'/><col style='width:20%'/>
                        <col style='width:14%'/><col style='width:30%'/>
                    </colgroup>
                    <thead><tr>
                        <th>DNI</th><th>Nombres</th><th>Apellidos</th><th>Teléfono</th><th>Email</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, "Reporte_Clientes");
        }

        // ─────────────────────────────────────────────
        //  REPORTE INVENTARIO
        // ─────────────────────────────────────────────
        private async Task GenerarReporteInventario()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            string accent = "#1e2d5f";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";

            if (periodoTexto == "Todo")
            {
                using (var cmd = new SqlCommand(
                    @"SELECT Producto_Nombre, Producto_Categoria, Producto_Marca,
                             Producto_Modelo, Producto_Precio,
                             Producto_Cantidad_Actual, Producto_Stock_Minimo
                      FROM Producto ORDER BY Producto_Nombre", db.SqlC))
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                    {
                        int cant = Convert.ToInt32(r["Producto_Cantidad_Actual"]);
                        int min = Convert.ToInt32(r["Producto_Stock_Minimo"]);
                        string stock = cant <= min
                            ? $"<span class='badge badge-cancelado'>{cant}</span>"
                            : $"<span class='badge badge-activo'>{cant}</span>";
                        filas += $@"<tr>
                            <td class='left'>{r["Producto_Nombre"]}</td>
                            <td class='left'>{r["Producto_Categoria"]}</td>
                            <td>{r["Producto_Marca"]}</td>
                            <td>{r["Producto_Modelo"]}</td>
                            <td>L {Convert.ToDecimal(r["Producto_Precio"]):N2}</td>
                            <td>{stock}</td><td>{min}</td><td>—</td>
                        </tr>";
                    }
            }
            else
            {
                using (var cmd = new SqlCommand(
                    @"SELECT p.Producto_Nombre, p.Producto_Categoria, p.Producto_Marca,
                             p.Producto_Modelo, p.Producto_Precio,
                             p.Producto_Cantidad_Actual, p.Producto_Stock_Minimo,
                             SUM(orep.Repuesto_Cantidad) AS Cantidad_Usada
                      FROM Orden_Repuesto orep
                      INNER JOIN Producto p ON orep.Producto_ID = p.Producto_ID
                      INNER JOIN Orden_Trabajo ot ON orep.Orden_ID = ot.Orden_ID
                      WHERE ot.Fecha BETWEEN @FI AND @FF
                      GROUP BY p.Producto_ID, p.Producto_Nombre, p.Producto_Categoria,
                               p.Producto_Marca, p.Producto_Modelo, p.Producto_Precio,
                               p.Producto_Cantidad_Actual, p.Producto_Stock_Minimo
                      ORDER BY Cantidad_Usada DESC", db.SqlC))
                {
                    cmd.Parameters.AddWithValue("@FI", fechaInicio);
                    cmd.Parameters.AddWithValue("@FF", fechaFin);
                    using var r = cmd.ExecuteReader();
                    bool hay = false;
                    while (r.Read())
                    {
                        hay = true;
                        int cant = Convert.ToInt32(r["Producto_Cantidad_Actual"]);
                        int min = Convert.ToInt32(r["Producto_Stock_Minimo"]);
                        string stock = cant <= min
                            ? $"<span class='badge badge-cancelado'>{cant}</span>"
                            : $"<span class='badge badge-activo'>{cant}</span>";
                        filas += $@"<tr>
                            <td class='left'>{r["Producto_Nombre"]}</td>
                            <td class='left'>{r["Producto_Categoria"]}</td>
                            <td>{r["Producto_Marca"]}</td><td>{r["Producto_Modelo"]}</td>
                            <td>L {Convert.ToDecimal(r["Producto_Precio"]):N2}</td>
                            <td>{stock}</td><td>{min}</td>
                            <td><b>{r["Cantidad_Usada"]}</b></td>
                        </tr>";
                    }
                    if (!hay) filas = $"<tr><td colspan='8' style='text-align:center;padding:20px;color:#888;'>Sin datos para {periodoTexto}</td></tr>";
                }
            }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent, periodoTexto)}
                {GetTitleBar(accent, $"Reporte de Inventario — {periodoTexto}")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:21%'/><col style='width:14%'/><col style='width:11%'/>
                        <col style='width:12%'/><col style='width:11%'/><col style='width:11%'/>
                        <col style='width:10%'/><col style='width:10%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Nombre</th><th>Categoría</th><th>Marca</th><th>Modelo</th>
                        <th>Precio</th><th>Stock Actual</th><th>Stock Mín.</th><th>Usado</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, $"Reporte_Inventario_{periodoTexto.Replace(" ", "_")}", landscape: true);
        }

        // ─────────────────────────────────────────────
        //  REPORTE VEHÍCULOS
        // ─────────────────────────────────────────────
        private async Task GenerarReporteVehiculos()
        {
            string accent = "#1e2d5f";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";
            using (var cmd = new SqlCommand(
                @"SELECT Vehiculo_Placa, Vehiculo_Marca, Vehiculo_Modelo, Vehiculo_Año,
                         Vehiculo_Tipo, Cliente_DNI, Vehiculo_Activo, Vehiculo_Observaciones
                  FROM Vehiculo ORDER BY Vehiculo_Marca", db.SqlC))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    bool activo = r["Vehiculo_Activo"] != DBNull.Value && (bool)r["Vehiculo_Activo"];
                    string badge = activo
                        ? "<span class='badge badge-activo'>Activo</span>"
                        : "<span class='badge badge-inactivo'>Inactivo</span>";
                    filas += $@"<tr>
                        <td><b>{r["Vehiculo_Placa"]}</b></td>
                        <td>{r["Vehiculo_Marca"]}</td><td>{r["Vehiculo_Modelo"]}</td>
                        <td>{r["Vehiculo_Año"]}</td><td>{r["Vehiculo_Tipo"]}</td>
                        <td>{r["Cliente_DNI"]}</td><td>{badge}</td>
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
                        <col style='width:10%'/><col style='width:10%'/><col style='width:14%'/>
                        <col style='width:6%'/><col style='width:10%'/><col style='width:14%'/>
                        <col style='width:9%'/><col style='width:27%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Placa</th><th>Marca</th><th>Modelo</th><th>Año</th>
                        <th>Tipo</th><th>Cliente DNI</th><th>Estado</th><th>Observaciones</th>
                    </tr></thead>
                    <tbody>{filas}</tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, "Reporte_Vehiculos", landscape: true);
        }

        // ─────────────────────────────────────────────
        //  REPORTE ÓRDENES
        // ─────────────────────────────────────────────
        private async Task GenerarReporteOrdenes()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            string accent = "#1e2d5f";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";
            decimal grandTotal = 0;
            using (var cmd = new SqlCommand(
                @"SELECT Orden_ID, Cliente_DNI, Vehiculo_Placa, Estado,
                         Fecha, Fecha_Entrega, Servicio_Precio, OrdenPrecio_Total, Observaciones
                  FROM Orden_Trabajo
                  WHERE Fecha BETWEEN @FI AND @FF
                  ORDER BY Fecha DESC", db.SqlC))
            {
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal total = Convert.ToDecimal(r["OrdenPrecio_Total"]);
                    grandTotal += total;
                    filas += $@"<tr>
                        <td><b>{r["Orden_ID"]}</b></td>
                        <td>{r["Cliente_DNI"]}</td><td>{r["Vehiculo_Placa"]}</td>
                        <td>{BadgeEstado(r["Estado"].ToString())}</td>
                        <td>{Convert.ToDateTime(r["Fecha"]).ToString("dd/MM/yyyy", CulturaES)}</td>
                        <td>{(r["Fecha_Entrega"] == DBNull.Value ? "—" : Convert.ToDateTime(r["Fecha_Entrega"]).ToString("dd/MM/yyyy", CulturaES))}</td>
                        <td>L {Convert.ToDecimal(r["Servicio_Precio"]):N2}</td>
                        <td>L {total:N2}</td>
                        <td class='obs'>{(r["Observaciones"] == DBNull.Value ? "—" : r["Observaciones"])}</td>
                    </tr>";
                }
                if (string.IsNullOrEmpty(filas))
                    filas = $"<tr><td colspan='9' style='text-align:center;padding:20px;color:#888;'>Sin órdenes para {periodoTexto}</td></tr>";
            }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent, periodoTexto)}
                {GetTitleBar(accent, $"Reporte de Órdenes de Trabajo — {periodoTexto}")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:4%'/><col style='width:14%'/><col style='width:10%'/>
                        <col style='width:11%'/><col style='width:9%'/><col style='width:9%'/>
                        <col style='width:10%'/><col style='width:10%'/><col style='width:23%'/>
                    </colgroup>
                    <thead><tr>
                        <th>ID</th><th>Cliente DNI</th><th>Placa</th><th>Estado</th>
                        <th>Fecha</th><th>Entrega</th><th>Servicio</th><th>Total</th><th>Observaciones</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='7'>TOTAL GENERAL</td>
                            <td>L {grandTotal:N2}</td><td></td>
                        </tr>
                    </tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, $"Reporte_Ordenes_{periodoTexto.Replace(" ", "_")}", landscape: true);
        }

        // ─────────────────────────────────────────────
        //  REPORTE EGRESOS
        // ─────────────────────────────────────────────
        private async Task GenerarReporteEgresos()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            string accent = "#7f1d1d";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";
            decimal total = 0;
            using (var cmd = new SqlCommand(
                @"SELECT Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto
                  FROM Contabilidad_Gastos
                  WHERE Fecha_Gasto BETWEEN @FI AND @FF
                  ORDER BY Fecha_Gasto DESC", db.SqlC))
            {
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal precio = r["Precio_Gasto"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Gasto"]);
                    total += precio;
                    filas += $@"<tr>
                        <td class='left'>{r["Tipo_Gasto"]}</td>
                        <td class='left'>{r["Nombre_Gasto"]}</td>
                        <td class='obs'>{(r["Observaciones_Gasto"] == DBNull.Value ? "—" : r["Observaciones_Gasto"])}</td>
                        <td>L {precio:N2}</td>
                        <td>{Convert.ToDateTime(r["Fecha_Gasto"]).ToString("dd/MM/yyyy", CulturaES)}</td>
                    </tr>";
                }
                if (string.IsNullOrEmpty(filas))
                    filas = $"<tr><td colspan='5' style='text-align:center;padding:20px;color:#888;'>Sin egresos para {periodoTexto}</td></tr>";
            }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent, periodoTexto)}
                {GetTitleBar(accent, $"Reporte de Egresos — {periodoTexto}")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:15%'/><col style='width:20%'/><col style='width:40%'/>
                        <col style='width:13%'/><col style='width:12%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Tipo</th><th>Nombre</th><th>Observaciones</th><th>Precio</th><th>Fecha</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='3'>TOTAL GENERAL</td>
                            <td>L {total:N2}</td><td></td>
                        </tr>
                    </tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, $"Reporte_Egresos_{periodoTexto.Replace(" ", "_")}");
        }

        // ─────────────────────────────────────────────
        //  REPORTE INGRESOS
        // ─────────────────────────────────────────────
        private async Task GenerarReporteIngresos()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            string accent = "#1b4332";
            var db = new ClsConexion(); db.Abrir();
            string filas = "";
            decimal total = 0;
            using (var cmd = new SqlCommand(
                @"SELECT p.Pago_ID,
                         c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                         p.Cliente_DNI, p.Orden_ID, p.Precio_Pago, p.Fecha_Pago
                  FROM Contabilidad_Pago p
                  INNER JOIN Cliente c ON p.Cliente_DNI = c.Cliente_DNI
                  WHERE p.Fecha_Pago BETWEEN @FI AND @FF
                  ORDER BY p.Pago_ID DESC", db.SqlC))
            {
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    decimal monto = r["Precio_Pago"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Pago"]);
                    total += monto;
                    filas += $@"<tr>
                        <td><b>{r["Pago_ID"]}</b></td>
                        <td class='left'>{r["NombreCompleto"]}</td>
                        <td>{r["Cliente_DNI"]}</td><td>{r["Orden_ID"]}</td>
                        <td>L {monto:N2}</td>
                        <td><span class='badge badge-pagado'>Pagado</span></td>
                    </tr>";
                }
                if (string.IsNullOrEmpty(filas))
                    filas = $"<tr><td colspan='6' style='text-align:center;padding:20px;color:#888;'>Sin ingresos para {periodoTexto}</td></tr>";
            }
            db.Cerrar();

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(accent, periodoTexto)}
                {GetTitleBar(accent, $"Reporte de Ingresos — {periodoTexto}")}
                <div class='content'>
                <table>
                    <colgroup>
                        <col style='width:10%'/><col style='width:30%'/><col style='width:20%'/>
                        <col style='width:15%'/><col style='width:15%'/><col style='width:10%'/>
                    </colgroup>
                    <thead><tr>
                        <th>Pago ID</th><th>Cliente</th><th>DNI</th>
                        <th>Orden ID</th><th>Monto</th><th>Estado</th>
                    </tr></thead>
                    <tbody>
                        {filas}
                        <tr class='total-row'>
                            <td colspan='4'>TOTAL GENERAL</td>
                            <td>L {total:N2}</td><td></td>
                        </tr>
                    </tbody>
                </table></div>
                {GetFooter()}
            </body></html>";

            await ExportarPDF(html, $"Reporte_Ingresos_{periodoTexto.Replace(" ", "_")}");
        }
    }
}