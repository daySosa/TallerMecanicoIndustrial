using System;
using System.IO;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace Login.Clases
{
    /// <summary>
    /// Utilidad de UN SOLO USO para migrar las fotos que actualmente tienes en disco
    /// (carpeta con subcarpetas por persona, ej: RostrosRegistrados/Juan/foto1.jpg)
    /// hacia la tabla RostrosRegistrados en la base de datos.
    ///
    /// Cómo usarla:
    /// 1. Asegúrate de haber ejecutado antes el script "crear_tabla_rostros.sql".
    /// 2. Llama una sola vez a este método (por ejemplo desde el constructor de
    ///    tu ventana principal, o agregando un botón temporal en cualquier ventana):
    ///
    ///    Login.Clases.ImportadorRostros.ImportarDesdeCarpeta(@"C:\ruta\a\RostrosRegistrados");
    ///
    /// 3. Ejecuta el programa una vez para que corra la importación.
    /// 4. Quita esa línea de tu código (y esta clase, si quieres) para que no se
    ///    vuelva a ejecutar ni duplique fotos.
    /// </summary>
    internal static class ImportadorRostros
    {
        public static void ImportarDesdeCarpeta(string rutaCarpeta)
        {
            if (!Directory.Exists(rutaCarpeta))
            {
                MessageBox.Show("No se encontró la carpeta: " + rutaCarpeta,
                    "Importación de rostros", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var conexion = new clsConexion();
            int totalImportadas = 0;

            try
            {
                conexion.Abrir();

                foreach (var carpetaPersona in Directory.GetDirectories(rutaCarpeta))
                {
                    string nombre = Path.GetFileName(carpetaPersona);

                    foreach (var archivo in Directory.GetFiles(carpetaPersona))
                    {
                        string ext = Path.GetExtension(archivo).ToLowerInvariant();
                        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".bmp")
                            continue;

                        byte[] datosFoto = File.ReadAllBytes(archivo);

                        using var cmd = new SqlCommand(
                            "INSERT INTO RostrosRegistrados (Nombre, Foto) VALUES (@Nombre, @Foto)",
                            conexion.SqlC);
                        cmd.Parameters.AddWithValue("@Nombre", nombre);
                        cmd.Parameters.AddWithValue("@Foto", datosFoto);
                        cmd.ExecuteNonQuery();

                        totalImportadas++;
                    }
                }

                MessageBox.Show($"Se importaron {totalImportadas} fotos a la base de datos.",
                    "Importación de rostros", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al importar fotos: " + ex.Message,
                    "Importación de rostros", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                conexion.Cerrar();
            }
        }
    }
}