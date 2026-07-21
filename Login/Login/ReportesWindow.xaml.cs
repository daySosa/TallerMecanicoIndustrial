using Login.Clases;
using Microsoft.Data.SqlClient;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Login
{
    /// <summary>
    /// Ventana para generar reportes en PDF de los distintos módulos del sistema
    /// (Clientes, Inventario, Vehículos, Órdenes, Egresos, Ingresos).
    /// La ventana nace invisible (Opacity="0" en el Window) y su Border raíz nace
    /// ligeramente reducido (ScaleTransform en el XAML) para una transición de
    /// apertura fluida sin parpadeos; se anima simétricamente al cerrarse.
    /// </summary>
    public partial class ReportesWindow : Window
    {
        // ════════════════════════════════════════════════════════════
        // ESTADO Y RECURSOS COMPARTIDOS
        // ════════════════════════════════════════════════════════════

        private readonly string _modulo;
        private readonly RepositorioSql _db = new();

        // Cultura fija en español (Honduras) para que los reportes nunca
        // salgan con nombres de días/meses en inglés, sin importar el
        // idioma del sistema operativo del usuario.
        private static readonly CultureInfo CulturaES = new("es-HN");

        // Duración y easing compartidos para las animaciones de apertura/cierre de la ventana.
        private static readonly Duration DuracionAnimacion = new(TimeSpan.FromMilliseconds(220));

        // ── Datos del negocio (encabezado/pie de los reportes) ──────
        private const string NombreNegocio = "OSM";
        private const string DireccionNegocio = "Col. 21 de Febrero, Bloque 5, Calle Principal, dos cuadras antes del Cementerio Santa Anita";
        private const string TelefonoNegocio = "9575-9819";

        // ── Literales reutilizados en varios reportes (S1192) ───────
        private const string AccentColorDefault = "#1e2d5f";
        private const string FormatoFechaCorta = "dd/MM/yyyy";
        private const string ColumnaClienteDni = "Cliente_DNI";
        private const string GrupoTipoReporte = "tipoReporte";

        // Ruta relativa del logo, reutilizada tanto para el pack URI como para la
        // búsqueda en disco junto al ejecutable (S1075: se evita repetir la ruta
        // absoluta/URI directamente en el código de negocio).
        private const string LogoResourceRelativePath = "Imagenes/OSM_LOGO.png"; // NOSONAR: ruta de recurso empaquetado, no de sistema de archivos externo.

        /// <summary>
        /// Crea la ventana de generación de reportes para el módulo indicado.
        /// </summary>
        /// <param name="modulo">Nombre del módulo ("Clientes", "Inventario", "Vehiculos", "Ordenes", "Egresos" o "Ingresos").</param>
        public ReportesWindow(string modulo)
        {
            InitializeComponent();
            _modulo = modulo;
            txtModulo.Text = $"Reporte de {modulo}";

            Loaded += ReportesWindow_Loaded;
        }

        // ════════════════════════════════════════════════════════════
        // TRANSICIÓN DE APERTURA / CIERRE
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Dispara la animación de entrada una sola vez, apenas la ventana termina de cargar
        /// (ya renderizada en su estado inicial invisible definido en el XAML).
        /// </summary>
        private void ReportesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= ReportesWindow_Loaded; // Solo debe ocurrir una vez.
            AnimarEntrada();
        }

        /// <summary>
        /// Anima la opacidad del <see cref="Window"/> (0→1) y la escala del Border raíz
        /// <c>rootScale</c> (0.94→1, vía <c>scaleEntrada</c>), con <see cref="QuadraticEase"/>
        /// para un movimiento natural y fluido.
        /// </summary>
        private void AnimarEntrada()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var fadeIn = new DoubleAnimation(0, 1, DuracionAnimacion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(0.94, 1, DuracionAnimacion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(0.94, 1, DuracionAnimacion) { EasingFunction = easing };

            BeginAnimation(OpacityProperty, fadeIn);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        /// <summary>
        /// Reproduce la animación inversa a la de entrada y, al completarse, cierra la ventana.
        /// Se usa para toda salida de esta ventana (Cancelar o generación de PDF exitosa),
        /// así la transición es consistente sin importar el camino de cierre.
        /// </summary>
        private void AnimarSalidaYCerrar()
        {
            var easing = new QuadraticEase { EasingMode = EasingMode.EaseIn };

            var fadeOut = new DoubleAnimation(1, 0, DuracionAnimacion) { EasingFunction = easing };
            var scaleX = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };
            var scaleY = new DoubleAnimation(1, 0.94, DuracionAnimacion) { EasingFunction = easing };

            fadeOut.Completed += (_, _) => Close();

            BeginAnimation(OpacityProperty, fadeOut);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            scaleEntrada.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e) => AnimarSalidaYCerrar();

        /// <summary>
        /// Punto de entrada del botón "Generar PDF": despacha al generador correspondiente
        /// según el módulo con el que se abrió la ventana.
        /// </summary>
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

        // ════════════════════════════════════════════════════════════
        // HELPERS DE FECHA EN ESPAÑOL
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Formatea una fecha con <see cref="CulturaES"/> y capitaliza la primera letra
        /// (para nombres de día/mes como "lunes" → "Lunes").
        /// </summary>
        private static string FormatearFechaEs(DateTime fecha, string formato)
        {
            string texto = fecha.ToString(formato, CulturaES);
            return string.IsNullOrEmpty(texto)
                ? texto
                : char.ToUpper(texto[0], CulturaES) + texto[1..];
        }

        // ════════════════════════════════════════════════════════════
        // LOGO
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Obtiene el logo del negocio como Base64 para incrustarlo en el HTML del reporte.
        /// Intenta, en orden: recurso empaquetado (pack URI), carpeta "Imagenes" junto al
        /// ejecutable, y por último buscando esa carpeta subiendo directorios (por si el
        /// build cambia de ubicación relativa). Cualquier fallo en un intento simplemente
        /// pasa al siguiente; si los tres fallan, se muestra el texto "OSM" en su lugar.
        /// </summary>
        private static string GetLogoBase64()
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/{LogoResourceRelativePath}", UriKind.Absolute);
                var si = Application.GetResourceStream(uri);
                if (si != null)
                {
                    using var ms = new MemoryStream();
                    si.Stream.CopyTo(ms);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch { /* Intencional: se prueba el siguiente método de carga del logo. */ }

            string rutaRelativaDisco = LogoResourceRelativePath.Replace('/', Path.DirectorySeparatorChar);

            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string path = Path.Combine(exeDir!, rutaRelativaDisco);
                if (File.Exists(path)) return Convert.ToBase64String(File.ReadAllBytes(path));
            }
            catch { /* Intencional: se prueba el siguiente método de carga del logo. */ }

            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                for (int i = 0; i < 5; i++)
                {
                    string candidate = Path.Combine(dir!, rutaRelativaDisco);
                    if (File.Exists(candidate)) return Convert.ToBase64String(File.ReadAllBytes(candidate));
                    dir = Path.GetDirectoryName(dir);
                    if (dir == null) break;
                }
            }
            catch { /* Intencional: no se encontró el logo en ninguna ubicación conocida. */ }

            return string.Empty;
        }

        // ════════════════════════════════════════════════════════════
        // ESTILOS BASE Y PLANTILLAS HTML
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el bloque &lt;style&gt; compartido por todos los reportes, parametrizado
        /// por el color de acento propio de cada módulo.
        /// </summary>
        private string GetBaseStyles(string accentColor = AccentColorDefault)
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

        /// <summary>
        /// Aclara un color hexadecimal en +40 por canal, usado para el degradado del encabezado.
        /// Si el color no se puede parsear, se devuelve sin cambios.
        /// </summary>
        private static string AdjustColor(string hex)
        {
            try
            {
                hex = hex.TrimStart('#');
                int r = Math.Min(255, Convert.ToInt32(hex[..2], 16) + 40);
                int g = Math.Min(255, Convert.ToInt32(hex[2..4], 16) + 40);
                int b = Math.Min(255, Convert.ToInt32(hex[4..6], 16) + 40);
                return $"#{r:X2}{g:X2}{b:X2}";
            }
            catch { return hex; }
        }

        /// <summary>
        /// Genera el encabezado (logo, nombre del negocio, fecha/período/hora) común a todos los reportes.
        /// </summary>
        private string GetHeader(string periodo = "")
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
                        <div class='meta-value'>{DateTime.Now.ToString(FormatoFechaCorta, CulturaES)}</div>
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

        /// <summary>
        /// Genera la barra de título con el nombre del reporte y su subtítulo
        /// (por defecto, la fecha de generación en español).
        /// </summary>
        private static string GetTitleBar(string titulo, string subtitulo = "")
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

        /// <summary>
        /// Genera el pie de página común a todos los reportes.
        /// </summary>
        private static string GetFooter() => $@"
            <div class='footer'>
                <span class='footer-brand'>{NombreNegocio}</span>
                <span>Documento confidencial · Uso interno</span>
                <span>Generado: {DateTime.Now.ToString(FormatoFechaCorta, CulturaES)} {DateTime.Now.ToString("hh:mm tt", CulturaES)}</span>
            </div>";

        /// <summary>
        /// Devuelve el HTML de un badge de estado con el color correspondiente
        /// ("Finalizado", "En Proceso", "Pendiente", "Cancelado", "Sin Empezar").
        /// </summary>
        private static string BadgeEstado(string estado) => estado switch
        {
            "Finalizado" => $"<span class='badge badge-pagado'>{estado}</span>",
            "En Proceso" => $"<span class='badge badge-proceso'>{estado}</span>",
            "Pendiente" => $"<span class='badge badge-pendiente'>{estado}</span>",
            "Cancelado" => $"<span class='badge badge-cancelado'>{estado}</span>",
            "Sin Empezar" => $"<span class='badge badge-sinempezar'>{estado}</span>",
            _ => $"<span class='badge'>{estado}</span>"
        };

        // ════════════════════════════════════════════════════════════
        // EXPORTAR PDF
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Renderiza el HTML dado en un WebView2 invisible y lo exporta a PDF mediante el
        /// diálogo de guardado del sistema. Si todo sale bien, registra la bitácora, abre
        /// el PDF generado y cierra esta ventana con animación.
        /// </summary>
        /// <param name="html">Documento HTML completo del reporte.</param>
        /// <param name="nombreArchivo">Nombre base sugerido para el archivo (se le agrega la fecha).</param>
        /// <param name="landscape">Si es true, exporta en orientación horizontal.</param>
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
                if (!success) throw new InvalidOperationException("No se pudo generar el PDF.");

                hiddenWindow.Close();
                hiddenWindow = null;

                MessageBox.Show("✅ Reporte generado correctamente.", "Éxito",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                try
                {
                    _db.RegistrarBitacora(SesionActual.Email, "Reportes", "Generar reporte",
                        $"Reporte de {_modulo} generado ({Path.GetFileName(dialog.FileName)})");
                }
                catch
                {
                    // Intencional: un fallo al registrar la bitácora no debe impedir
                    // que el usuario reciba y abra su PDF ya generado.
                }

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dialog.FileName,
                    UseShellExecute = true
                });

                AnimarSalidaYCerrar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al generar el PDF:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                hiddenWindow?.Close();
            }
        }

        // ════════════════════════════════════════════════════════════
        // SELECTOR DE PERÍODO (Semanal / Mensual / Anual / Todo)
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Muestra un diálogo modal para que el usuario elija el período del reporte
        /// (semanal, mensual, anual o todo el histórico) y calcula el rango de fechas resultante.
        /// </summary>
        /// <returns><c>true</c> si el usuario confirmó una selección; <c>false</c> si canceló.</returns>
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
                Background = new SolidColorBrush(Color.FromRgb(30, 45, 95))
            };

            var panelPrincipal = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };

            panelPrincipal.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Tipo de reporte:",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            var (tipoPanel, rbSemanal, rbMensual, rbAnual, rbTodo) = CrearPanelTipoReporte();
            panelPrincipal.Children.Add(tipoPanel);

            var dpSemana = new System.Windows.Controls.DatePicker
            {
                SelectedDate = DateTime.Today,
                FirstDayOfWeek = DayOfWeek.Monday
            };
            var panelSemanal = CrearPanelSemanal(dpSemana);

            var cmbMes = new System.Windows.Controls.ComboBox { Width = 150, Margin = new Thickness(0, 0, 8, 0) };
            var cmbAnioMes = new System.Windows.Controls.ComboBox { Width = 90 };
            var panelMensual = CrearPanelMensual(cmbMes, cmbAnioMes);

            var cmbAnioSolo = new System.Windows.Controls.ComboBox { Width = 90, HorizontalAlignment = HorizontalAlignment.Left };
            var panelAnual = CrearPanelAnual(cmbAnioSolo);

            panelPrincipal.Children.Add(panelSemanal);
            panelPrincipal.Children.Add(panelMensual);
            panelPrincipal.Children.Add(panelAnual);

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

            bool confirmado = false;
            var btnPanel = CrearPanelBotones(ventana, () => confirmado = true);
            panelPrincipal.Children.Add(btnPanel);

            ventana.Content = panelPrincipal;
            ventana.ShowDialog();

            if (!confirmado) return false;

            (fechaInicio, fechaFin, periodoTexto) = CalcularRangoFechas(
                rbSemanal.IsChecked == true, rbMensual.IsChecked == true, rbAnual.IsChecked == true,
                dpSemana, cmbMes, cmbAnioMes, cmbAnioSolo);

            return true;
        }

        /// <summary>Crea los radio buttons de tipo de reporte (Semanal/Mensual/Anual/Todo).</summary>
        private static (System.Windows.Controls.StackPanel panel, System.Windows.Controls.RadioButton semanal,
            System.Windows.Controls.RadioButton mensual, System.Windows.Controls.RadioButton anual,
            System.Windows.Controls.RadioButton todo) CrearPanelTipoReporte()
        {
            var tipoPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 0, 0, 14)
            };

            var rbSemanal = new System.Windows.Controls.RadioButton
            {
                Content = "Semanal",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                IsChecked = true,
                GroupName = GrupoTipoReporte
            };
            var rbMensual = new System.Windows.Controls.RadioButton
            {
                Content = "Mensual",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                GroupName = GrupoTipoReporte
            };
            var rbAnual = new System.Windows.Controls.RadioButton
            {
                Content = "Anual",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 14, 0),
                GroupName = GrupoTipoReporte
            };
            var rbTodo = new System.Windows.Controls.RadioButton
            {
                Content = "Todo",
                Foreground = Brushes.White,
                GroupName = GrupoTipoReporte
            };

            tipoPanel.Children.Add(rbSemanal);
            tipoPanel.Children.Add(rbMensual);
            tipoPanel.Children.Add(rbAnual);
            tipoPanel.Children.Add(rbTodo);

            return (tipoPanel, rbSemanal, rbMensual, rbAnual, rbTodo);
        }

        /// <summary>Crea el panel del selector semanal.</summary>
        private static System.Windows.Controls.StackPanel CrearPanelSemanal(System.Windows.Controls.DatePicker dpSemana)
        {
            var panelSemanal = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 14) };
            panelSemanal.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona un día dentro de la semana deseada:",
                Foreground = Brushes.White,
                FontSize = 11
            });
            panelSemanal.Children.Add(dpSemana);
            return panelSemanal;
        }

        /// <summary>Crea el panel del selector mensual (combo de mes + combo de año).</summary>
        private static System.Windows.Controls.StackPanel CrearPanelMensual(
            System.Windows.Controls.ComboBox cmbMes, System.Windows.Controls.ComboBox cmbAnioMes)
        {
            var panelMensual = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 14),
                Visibility = Visibility.Collapsed
            };
            panelMensual.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona el mes y el año:",
                Foreground = Brushes.White,
                FontSize = 11
            });

            var mesAnioPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

            foreach (var nombreMes in CulturaES.DateTimeFormat.MonthNames.Where(m => !string.IsNullOrEmpty(m)))
                cmbMes.Items.Add(char.ToUpper(nombreMes[0], CulturaES) + nombreMes[1..]);
            cmbMes.SelectedIndex = DateTime.Today.Month - 1;

            int anioActual = DateTime.Today.Year;
            for (int y = anioActual; y >= anioActual - 10; y--) cmbAnioMes.Items.Add(y);
            cmbAnioMes.SelectedIndex = 0;

            mesAnioPanel.Children.Add(cmbMes);
            mesAnioPanel.Children.Add(cmbAnioMes);
            panelMensual.Children.Add(mesAnioPanel);

            return panelMensual;
        }

        /// <summary>Crea el panel del selector anual.</summary>
        private static System.Windows.Controls.StackPanel CrearPanelAnual(System.Windows.Controls.ComboBox cmbAnioSolo)
        {
            var panelAnual = new System.Windows.Controls.StackPanel
            {
                Margin = new Thickness(0, 0, 0, 14),
                Visibility = Visibility.Collapsed
            };
            panelAnual.Children.Add(new System.Windows.Controls.Label
            {
                Content = "Selecciona el año:",
                Foreground = Brushes.White,
                FontSize = 11
            });

            int anioActual = DateTime.Today.Year;
            for (int y = anioActual; y >= anioActual - 10; y--) cmbAnioSolo.Items.Add(y);
            cmbAnioSolo.SelectedIndex = 0;
            panelAnual.Children.Add(cmbAnioSolo);

            return panelAnual;
        }

        /// <summary>Crea los botones "Generar" y "Cancelar" del diálogo de período.</summary>
        private static System.Windows.Controls.StackPanel CrearPanelBotones(Window ventana, Action onAceptar)
        {
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
                Background = new SolidColorBrush(Colors.White),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 45, 95)),
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnAceptar.Click += (s, e) => { onAceptar(); ventana.Close(); };

            var btnCancelarPeriodo = new System.Windows.Controls.Button
            {
                Content = "Cancelar",
                Width = 80,
                Height = 32,
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderBrush = Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancelarPeriodo.Click += (s, e) => ventana.Close();

            btnPanel.Children.Add(btnAceptar);
            btnPanel.Children.Add(btnCancelarPeriodo);

            return btnPanel;
        }

        /// <summary>Calcula el rango de fechas y el texto de período según el tipo elegido.</summary>
        private static (DateTime inicio, DateTime fin, string texto) CalcularRangoFechas(
            bool esSemanal, bool esMensual, bool esAnual,
            System.Windows.Controls.DatePicker dpSemana,
            System.Windows.Controls.ComboBox cmbMes,
            System.Windows.Controls.ComboBox cmbAnioMes,
            System.Windows.Controls.ComboBox cmbAnioSolo)
        {
            if (esSemanal) return CalcularRangoSemanal(dpSemana);
            if (esMensual) return CalcularRangoMensual(cmbMes, cmbAnioMes);
            if (esAnual) return CalcularRangoAnual(cmbAnioSolo);
            return (DateTime.MinValue, DateTime.MaxValue, "Todo");
        }

        private static (DateTime, DateTime, string) CalcularRangoSemanal(System.Windows.Controls.DatePicker dpSemana)
        {
            DateTime fechaBase = dpSemana.SelectedDate ?? DateTime.Today;
            int diasDesdeLunes = ((int)fechaBase.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            DateTime inicio = fechaBase.AddDays(-diasDesdeLunes);
            DateTime fin = inicio.AddDays(7).AddTicks(-1);
            string texto = $"Semana del {FormatearFechaEs(inicio, "dd MMM")} al {FormatearFechaEs(inicio.AddDays(6), "dd MMM yyyy")}";
            return (inicio, fin, texto);
        }

        private static (DateTime, DateTime, string) CalcularRangoMensual(
            System.Windows.Controls.ComboBox cmbMes, System.Windows.Controls.ComboBox cmbAnioMes)
        {
            int mes = cmbMes.SelectedIndex + 1;
            int anio = (int)cmbAnioMes.SelectedItem;
            DateTime inicio = new(anio, mes, 1, 0, 0, 0, DateTimeKind.Local);
            DateTime fin = inicio.AddMonths(1).AddTicks(-1);
            string texto = FormatearFechaEs(inicio, "MMMM yyyy");
            return (inicio, fin, texto);
        }

        private static (DateTime, DateTime, string) CalcularRangoAnual(System.Windows.Controls.ComboBox cmbAnioSolo)
        {
            int anio = (int)cmbAnioSolo.SelectedItem;
            DateTime inicio = new(anio, 1, 1, 0, 0, 0, DateTimeKind.Local);
            DateTime fin = new(anio, 12, 31, 23, 59, 59, DateTimeKind.Local);
            string texto = anio.ToString();
            return (inicio, fin, texto);
        }

        // ════════════════════════════════════════════════════════════
        // REPORTE CLIENTES
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de todos los clientes registrados, ordenados por apellido.
        /// </summary>
        private async Task GenerarReporteClientes()
        {
            const string accent = AccentColorDefault;
            var filas = new StringBuilder();
            var db = new ClsConexion();
            try
            {
                db.Abrir();
                using var cmd = new SqlCommand(
                    @"SELECT Cliente_DNI, Cliente_Nombres, Cliente_Apellidos,
                             Cliente_TelefonoPrincipal, Cliente_Email
                      FROM Cliente ORDER BY Cliente_Apellidos", db.SqlC);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                    filas.Append($@"<tr>
                        <td>{r[ColumnaClienteDni]}</td>
                        <td class='left'>{r["Cliente_Nombres"]}</td>
                        <td class='left'>{r["Cliente_Apellidos"]}</td>
                        <td>{r["Cliente_TelefonoPrincipal"]}</td>
                        <td class='left'>{r["Cliente_Email"]}</td>
                    </tr>");
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader()}
                {GetTitleBar("Reporte de Clientes")}
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

        // ════════════════════════════════════════════════════════════
        // REPORTE INVENTARIO
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de inventario. Sin período seleccionado ("Todo") lista todos
        /// los productos; con un período, lista solo los usados en órdenes de ese rango,
        /// ordenados por cantidad usada.
        /// </summary>
        private async Task GenerarReporteInventario()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            const string accent = AccentColorDefault;
            var filas = new StringBuilder();
            var db = new ClsConexion();
            try
            {
                db.Abrir();

                if (periodoTexto == "Todo")
                {
                    using var cmd = new SqlCommand(
                        @"SELECT Producto_Nombre, Producto_Categoria, Producto_Marca,
                                 Producto_Modelo, Producto_Precio,
                                 Producto_Cantidad_Actual, Producto_Stock_Minimo
                          FROM Producto ORDER BY Producto_Nombre", db.SqlC);
                    using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        int cant = Convert.ToInt32(r["Producto_Cantidad_Actual"]);
                        int min = Convert.ToInt32(r["Producto_Stock_Minimo"]);
                        string stock = cant <= min
                            ? $"<span class='badge badge-cancelado'>{cant}</span>"
                            : $"<span class='badge badge-activo'>{cant}</span>";
                        filas.Append($@"<tr>
                            <td class='left'>{r["Producto_Nombre"]}</td>
                            <td class='left'>{r["Producto_Categoria"]}</td>
                            <td>{r["Producto_Marca"]}</td>
                            <td>{r["Producto_Modelo"]}</td>
                            <td>L {Convert.ToDecimal(r["Producto_Precio"]):N2}</td>
                            <td>{stock}</td><td>{min}</td><td>—</td>
                        </tr>");
                    }
                }
                else
                {
                    using var cmd = new SqlCommand(
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
                          ORDER BY Cantidad_Usada DESC", db.SqlC);
                    cmd.Parameters.AddWithValue("@FI", fechaInicio);
                    cmd.Parameters.AddWithValue("@FF", fechaFin);
                    using var r = await cmd.ExecuteReaderAsync();
                    bool hay = false;
                    while (await r.ReadAsync())
                    {
                        hay = true;
                        int cant = Convert.ToInt32(r["Producto_Cantidad_Actual"]);
                        int min = Convert.ToInt32(r["Producto_Stock_Minimo"]);
                        string stock = cant <= min
                            ? $"<span class='badge badge-cancelado'>{cant}</span>"
                            : $"<span class='badge badge-activo'>{cant}</span>";
                        filas.Append($@"<tr>
                            <td class='left'>{r["Producto_Nombre"]}</td>
                            <td class='left'>{r["Producto_Categoria"]}</td>
                            <td>{r["Producto_Marca"]}</td><td>{r["Producto_Modelo"]}</td>
                            <td>L {Convert.ToDecimal(r["Producto_Precio"]):N2}</td>
                            <td>{stock}</td><td>{min}</td>
                            <td><b>{r["Cantidad_Usada"]}</b></td>
                        </tr>");
                    }
                    if (!hay)
                        filas.Append($"<tr><td colspan='8' style='text-align:center;padding:20px;color:#888;'>Sin datos para {periodoTexto}</td></tr>");
                }
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(periodoTexto)}
                {GetTitleBar($"Reporte de Inventario — {periodoTexto}")}
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

        // ════════════════════════════════════════════════════════════
        // REPORTE VEHÍCULOS
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de todos los vehículos registrados, ordenados por marca.
        /// </summary>
        private async Task GenerarReporteVehiculos()
        {
            const string accent = AccentColorDefault;
            var filas = new StringBuilder();
            var db = new ClsConexion();
            try
            {
                db.Abrir();
                using var cmd = new SqlCommand(
                    @"SELECT Vehiculo_Placa, Vehiculo_Marca, Vehiculo_Modelo, Vehiculo_Año,
                             Vehiculo_Tipo, Cliente_DNI, Vehiculo_Activo, Vehiculo_Observaciones
                      FROM Vehiculo ORDER BY Vehiculo_Marca", db.SqlC);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    bool activo = r["Vehiculo_Activo"] != DBNull.Value && (bool)r["Vehiculo_Activo"];
                    string badge = activo
                        ? "<span class='badge badge-activo'>Activo</span>"
                        : "<span class='badge badge-inactivo'>Inactivo</span>";
                    filas.Append($@"<tr>
                        <td><b>{r["Vehiculo_Placa"]}</b></td>
                        <td>{r["Vehiculo_Marca"]}</td><td>{r["Vehiculo_Modelo"]}</td>
                        <td>{r["Vehiculo_Año"]}</td><td>{r["Vehiculo_Tipo"]}</td>
                        <td>{r[ColumnaClienteDni]}</td><td>{badge}</td>
                        <td class='obs'>{(r["Vehiculo_Observaciones"] == DBNull.Value ? "—" : r["Vehiculo_Observaciones"])}</td>
                    </tr>");
                }
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader()}
                {GetTitleBar("Reporte de Vehículos")}
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

        // ════════════════════════════════════════════════════════════
        // REPORTE ÓRDENES
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de órdenes de trabajo dentro del período elegido, con el
        /// total general acumulado.
        /// </summary>
        private async Task GenerarReporteOrdenes()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            const string accent = AccentColorDefault;
            var filas = new StringBuilder();
            decimal grandTotal = 0;
            var db = new ClsConexion();
            try
            {
                db.Abrir();
                using var cmd = new SqlCommand(
                    @"SELECT Orden_ID, Cliente_DNI, Vehiculo_Placa, Estado,
                             Fecha, Fecha_Entrega, Servicio_Precio, OrdenPrecio_Total, Observaciones
                      FROM Orden_Trabajo
                      WHERE Fecha BETWEEN @FI AND @FF
                      ORDER BY Fecha DESC", db.SqlC);
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    decimal total = Convert.ToDecimal(r["OrdenPrecio_Total"]);
                    grandTotal += total;
                    filas.Append($@"<tr>
                        <td><b>{r["Orden_ID"]}</b></td>
                        <td>{r[ColumnaClienteDni]}</td><td>{r["Vehiculo_Placa"]}</td>
                        <td>{BadgeEstado(r["Estado"].ToString())}</td>
                        <td>{Convert.ToDateTime(r["Fecha"]).ToString(FormatoFechaCorta, CulturaES)}</td>
                        <td>{(r["Fecha_Entrega"] == DBNull.Value ? "—" : Convert.ToDateTime(r["Fecha_Entrega"]).ToString(FormatoFechaCorta, CulturaES))}</td>
                        <td>L {Convert.ToDecimal(r["Servicio_Precio"]):N2}</td>
                        <td>L {total:N2}</td>
                        <td class='obs'>{(r["Observaciones"] == DBNull.Value ? "—" : r["Observaciones"])}</td>
                    </tr>");
                }
                if (filas.Length == 0)
                    filas.Append($"<tr><td colspan='9' style='text-align:center;padding:20px;color:#888;'>Sin órdenes para {periodoTexto}</td></tr>");
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(periodoTexto)}
                {GetTitleBar($"Reporte de Órdenes de Trabajo — {periodoTexto}")}
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

        // ════════════════════════════════════════════════════════════
        // REPORTE EGRESOS
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de gastos (egresos) dentro del período elegido, con el
        /// total general acumulado.
        /// </summary>
        private async Task GenerarReporteEgresos()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            const string accent = "#7f1d1d";
            var filas = new StringBuilder();
            decimal total = 0;
            var db = new ClsConexion();
            try
            {
                db.Abrir();
                using var cmd = new SqlCommand(
                    @"SELECT Tipo_Gasto, Nombre_Gasto, Observaciones_Gasto, Precio_Gasto, Fecha_Gasto
                      FROM Contabilidad_Gastos
                      WHERE Fecha_Gasto BETWEEN @FI AND @FF
                      ORDER BY Fecha_Gasto DESC", db.SqlC);
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    decimal precio = r["Precio_Gasto"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Gasto"]);
                    total += precio;
                    filas.Append($@"<tr>
                        <td class='left'>{r["Tipo_Gasto"]}</td>
                        <td class='left'>{r["Nombre_Gasto"]}</td>
                        <td class='obs'>{(r["Observaciones_Gasto"] == DBNull.Value ? "—" : r["Observaciones_Gasto"])}</td>
                        <td>L {precio:N2}</td>
                        <td>{Convert.ToDateTime(r["Fecha_Gasto"]).ToString(FormatoFechaCorta, CulturaES)}</td>
                    </tr>");
                }
                if (filas.Length == 0)
                    filas.Append($"<tr><td colspan='5' style='text-align:center;padding:20px;color:#888;'>Sin egresos para {periodoTexto}</td></tr>");
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(periodoTexto)}
                {GetTitleBar($"Reporte de Egresos — {periodoTexto}")}
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

        // ════════════════════════════════════════════════════════════
        // REPORTE INGRESOS
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Genera el reporte de pagos (ingresos) dentro del período elegido, con el
        /// total general acumulado.
        /// </summary>
        private async Task GenerarReporteIngresos()
        {
            if (!ObtenerRangoFechas(out DateTime fechaInicio, out DateTime fechaFin, out string periodoTexto))
                return;

            const string accent = "#1b4332";
            var filas = new StringBuilder();
            decimal total = 0;
            var db = new ClsConexion();
            try
            {
                db.Abrir();
                using var cmd = new SqlCommand(
                    @"SELECT p.Pago_ID,
                             c.Cliente_Nombres + ' ' + c.Cliente_Apellidos AS NombreCompleto,
                             p.Cliente_DNI, p.Orden_ID, p.Precio_Pago, p.Fecha_Pago
                      FROM Contabilidad_Pago p
                      INNER JOIN Cliente c ON p.Cliente_DNI = c.Cliente_DNI
                      WHERE p.Fecha_Pago BETWEEN @FI AND @FF
                      ORDER BY p.Pago_ID DESC", db.SqlC);
                cmd.Parameters.AddWithValue("@FI", fechaInicio);
                cmd.Parameters.AddWithValue("@FF", fechaFin);
                using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    decimal monto = r["Precio_Pago"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Precio_Pago"]);
                    total += monto;
                    filas.Append($@"<tr>
                        <td><b>{r["Pago_ID"]}</b></td>
                        <td class='left'>{r["NombreCompleto"]}</td>
                        <td>{r[ColumnaClienteDni]}</td><td>{r["Orden_ID"]}</td>
                        <td>L {monto:N2}</td>
                        <td><span class='badge badge-pagado'>Pagado</span></td>
                    </tr>");
                }
                if (filas.Length == 0)
                    filas.Append($"<tr><td colspan='6' style='text-align:center;padding:20px;color:#888;'>Sin ingresos para {periodoTexto}</td></tr>");
            }
            finally
            {
                db.Cerrar();
            }

            string html = $@"<html><head>{GetBaseStyles(accent)}</head><body>
                {GetHeader(periodoTexto)}
                {GetTitleBar($"Reporte de Ingresos — {periodoTexto}")}
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