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
                    // aquí irán los demás módulos después
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
