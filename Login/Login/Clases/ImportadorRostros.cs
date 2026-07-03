using Microsoft.Data.SqlClient;
using System.Data;
using System.IO;
using System.Windows;

namespace Login.Clases
{
    /// <summary>
    /// Utilidad de UN SOLO USO para migrar las fotos que están en disco
    /// (carpeta con subcarpetas por persona, ej: PersonasRegistradas/Gabriela/foto1.jpg)
    /// hacia la tabla RostrosRegistrados en la base de datos.
    ///
    /// Cómo usarla:
    /// 1. Asegúrate de que la tabla RostrosRegistrados ya exista.
    /// 2. Llama una sola vez a este método, por ejemplo desde el constructor de
    ///    ReconocimientoFacial (antes de EntrenarReconocedor()):
    ///
    ///    Login.Clases.ImportadorRostros.ImportarDesdeCarpeta(@"C:\ruta\a\PersonasRegistradas");
    ///
    /// 3. Ejecuta el programa una vez para que corra la importación.
    /// 4. Quita esa línea de tu código (y borra este archivo si quieres) para que no
    ///    se vuelva a ejecutar ni duplique fotos.
    ///
    /// NOTA: revisa si esta tabla ("RostrosRegistrados") sigue siendo la que realmente usa
    /// tu módulo de reconocimiento activo, ya que ReconocimientoFacial.xaml.cs entrena
    /// desde "ReconocimientoFacial", no desde esta tabla.
    /// </summary>
    internal static class ImportadorRostros
    {
        private static readonly string[] ExtensionesValidas = { ".jpg", ".jpeg", ".png", ".bmp" };

        public static void ImportarDesdeCarpeta(string rutaCarpeta)
        {
            if (!Directory.Exists(rutaCarpeta))
            {
                MessageBox.Show("No se encontró la carpeta: " + rutaCarpeta,
                    "Importación de rostros", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int totalImportadas = 0;

            using var conexion = new ClsConexion();

            try
            {
                if (!conexion.Abrir())
                    return;

                using var transaccion = conexion.SqlC.BeginTransaction();
                using var cmd = new SqlCommand(
                    "INSERT INTO RostrosRegistrados (Nombre, Foto) VALUES (@Nombre, @Foto)",
                    conexion.SqlC, transaccion);

                var paramNombre = cmd.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100);
                var paramFoto = cmd.Parameters.Add("@Foto", SqlDbType.VarBinary, -1);

                foreach (var carpetaPersona in Directory.GetDirectories(rutaCarpeta))
                {
                    string nombre = Path.GetFileName(carpetaPersona);

                    foreach (var archivo in Directory.GetFiles(carpetaPersona))
                    {
                        string ext = Path.GetExtension(archivo).ToLowerInvariant();
                        if (Array.IndexOf(ExtensionesValidas, ext) < 0)
                            continue;

                        paramNombre.Value = nombre;
                        paramFoto.Value = File.ReadAllBytes(archivo);
                        cmd.ExecuteNonQuery();

                        totalImportadas++;
                    }
                }

                transaccion.Commit();

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