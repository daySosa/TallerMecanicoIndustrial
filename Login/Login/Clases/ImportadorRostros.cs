using System;
using System.IO;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace Login.Clases
{
    /// <summary>
    /// Utilidad de UN SOLO USO para migrar las fotos que están en disco
    /// (carpeta con subcarpetas por persona, ej: PersonasRegistradas/Gabriela/foto1.jpg)
    /// hacia la tabla RostrosRegistrados en la base de datos.
    ///
    /// Cómo usarla:
    /// 1. Asegúrate de que la tabla RostrosRegistrados ya exista (si la truncaste, no
    ///    hace falta recrearla, con TRUNCATE la estructura de la tabla se mantiene).
    /// 2. Llama una sola vez a este método, por ejemplo desde el constructor de
    ///    ReconocimientoFacial (antes de EntrenarReconocedor()):
    ///
    ///    Login.Clases.ImportadorRostros.ImportarDesdeCarpeta(@"C:\ruta\a\PersonasRegistradas");
    ///
    /// 3. Ejecuta el programa una vez para que corra la importación.
    /// 4. Quita esa línea de tu código (y borra este archivo si quieres) para que no
    ///    se vuelva a ejecutar ni duplique fotos.
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

            // NOTA: se usa una cadena de conexión propia (en vez de clsConexion) para no
            // modificar esa clase compartida por el equipo. Se agrega "Authentication=SqlPassword;"
            // porque las versiones recientes de Microsoft.Data.SqlClient, al detectar un servidor
            // "*.database.windows.net", intentan autenticar con Azure Active Directory por defecto
            // si no se especifica el tipo de autenticación.
            const string cadenaConexion =
                "Data Source=tallermecanic.database.windows.net;Initial Catalog=Taller_Mecanico_Sistema;" +
                "User ID=DayanaSosa;Password=Serv2026;Authentication=SqlPassword;";

            using var conexion = new SqlConnection(cadenaConexion);
            int totalImportadas = 0;

            try
            {
                conexion.Open();

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
                            conexion);
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
            // No se necesita "finally" con Cerrar(): el "using" en la declaración
            // de "conexion" ya cierra y libera la conexión automáticamente.
        }
    }
}