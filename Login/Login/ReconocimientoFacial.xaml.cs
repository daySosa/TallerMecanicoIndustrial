using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Login.Clases;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Drawing = System.Drawing;

namespace Login
{
    public partial class ReconocimientoFacial : Window
    {
        private FilterInfoCollection? camarasDisponibles;
        private VideoCaptureDevice? camaraActiva;
        private CascadeClassifier? detectorRostros;
        private bool rostroDetectado = false;
        private bool accesoOtorgado = false;
        private bool esModoRegistro = false; // ✅ NUEVO: controla si estamos registrando o haciendo login
        private List<(int Id, string Nombre, Drawing.Bitmap Foto)> personasRegistradas = new();
        private Drawing.Bitmap? fotoCapturada = null;
        private LBPHFaceRecognizer? reconocedor;
        private bool reconocedorEntrenado = false;
        private const double UMBRAL_RECONOCIMIENTO = 100.0;

        public ReconocimientoFacial()
        {
            InitializeComponent();
            string rutaXml = "haarcascade_frontalface_default.xml";
            if (File.Exists(rutaXml))
                detectorRostros = new CascadeClassifier(rutaXml);
            else
                txtEstado.Text = "No se encontró XML de rostros";

            CargarPersonasDesdeBD();
        }

        private void CargarPersonasDesdeBD()
        {
            try
            {
                foreach (var p in personasRegistradas) p.Foto?.Dispose();
                personasRegistradas.Clear();

                clsConexion conexion = new clsConexion();
                conexion.Abrir();
                string query = "SELECT Id, Nombre, Foto FROM ReconocimientoFacial";
                SqlCommand cmd = new SqlCommand(query, conexion.SqlC);
                using SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    byte[] fotoBytes = (byte[])reader[2];
                    using MemoryStream ms = new MemoryStream(fotoBytes);
                    Drawing.Bitmap foto = new Drawing.Bitmap(new Drawing.Bitmap(ms));
                    personasRegistradas.Add((id, nombre, foto));
                }

                conexion.Cerrar();
                if (personasRegistradas.Count > 0)
                    EntrenarReconocedor();

                txtEstado.Text = $"{personasRegistradas.Count} persona(s) cargadas";
            }
            catch (Exception ex)
            {
                txtEstado.Text = "Error cargando BD: " + ex.Message;
            }
        }

        private void GuardarPersonaEnBD(string nombre, Drawing.Bitmap foto)
        {
            try
            {
                using Drawing.Bitmap copia = new Drawing.Bitmap(foto);
                using MemoryStream ms = new MemoryStream();
                copia.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] fotoBytes = ms.ToArray();

                clsConexion conexion = new clsConexion();
                conexion.Abrir();
                string query = "INSERT INTO ReconocimientoFacial (Nombre, Foto) VALUES (@nombre, @foto)";
                SqlCommand cmd = new SqlCommand(query, conexion.SqlC);
                cmd.Parameters.AddWithValue("@nombre", nombre);
                cmd.Parameters.AddWithValue("@foto", fotoBytes);
                cmd.ExecuteNonQuery();
                conexion.Cerrar();

                MessageBox.Show($"Persona '{nombre}' registrada correctamente.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message);
            }
        }

        // ✅ NUEVO: Activa modo registro — desactiva el reconocimiento automático
        private void btnModoRegistro_Click(object sender, RoutedEventArgs e)
        {
            esModoRegistro = true;
            txtEstado.Text = "MODO REGISTRO — El reconocimiento está desactivado";
            txtNombreReconocido.Text = "";
            btnModoRegistro.IsEnabled = false;
            btnModoLogin.IsEnabled = true;
        }

        // ✅ NUEVO: Activa modo login — reactiva el reconocimiento automático
        private void btnModoLogin_Click(object sender, RoutedEventArgs e)
        {
            esModoRegistro = false;
            accesoOtorgado = false;
            fotoCapturada = null;
            txtEstado.Text = "MODO LOGIN — Coloca tu cara frente a la cámara";
            txtNombreReconocido.Text = "";
            btnModoLogin.IsEnabled = false;
            btnModoRegistro.IsEnabled = true;
        }

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (camarasDisponibles.Count == 0) return;

            accesoOtorgado = false;
            esModoRegistro = false; // Por defecto inicia en modo login
            camaraActiva = new VideoCaptureDevice(camarasDisponibles[0].MonikerString);
            camaraActiva.NewFrame += CamaraActiva_NewFrame;
            camaraActiva.Start();

            txtSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            btnCapturarFoto.IsEnabled = true;
            btnModoRegistro.IsEnabled = true;
            btnModoLogin.IsEnabled = false;
        }

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (accesoOtorgado) return;

            using (Drawing.Bitmap bitmap = (Drawing.Bitmap)eventArgs.Frame.Clone())
            {
                if (detectorRostros != null)
                {
                    using var imgEmgu = bitmap.ToImage<Bgr, byte>();
                    using var imgGris = imgEmgu.Convert<Gray, byte>();
                    var rostros = detectorRostros.DetectMultiScale(imgGris, 1.1, 10, new Drawing.Size(80, 80));

                    rostroDetectado = rostros.Length > 0;

                    foreach (var rostro in rostros)
                    {
                        // Color del rectángulo según el modo
                        var colorRect = esModoRegistro ? Drawing.Color.Orange : Drawing.Color.LimeGreen;
                        using (Drawing.Graphics g = Drawing.Graphics.FromImage(bitmap))
                            g.DrawRectangle(new Drawing.Pen(colorRect, 3), rostro);

                        // ✅ Solo reconoce si estamos en modo LOGIN
                        if (!esModoRegistro && reconocedorEntrenado && reconocedor != null)
                        {
                            using var rostroRecortado = imgGris.Copy(rostro)
                                .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

                            var resultado = reconocedor.Predict(rostroRecortado.Mat);

                            bool esDesconocido = resultado.Label < 0
                                              || resultado.Label >= personasRegistradas.Count
                                              || resultado.Distance >= UMBRAL_RECONOCIMIENTO;

                            if (!esDesconocido)
                            {
                                string nombre = personasRegistradas[resultado.Label].Nombre;
                                Dispatcher.Invoke(() =>
                                    txtNombreReconocido.Text = $"Identificado: {nombre} (dist: {resultado.Distance:F1})");
                                FinalizarAcceso(nombre);
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                    txtNombreReconocido.Text = $"Desconocido (dist: {resultado.Distance:F1})");
                            }
                        }
                    }

                    if (rostros.Length == 0)
                        Dispatcher.Invoke(() => txtNombreReconocido.Text = "");
                }

                Dispatcher.Invoke(() =>
                {
                    imgCamara.Source = BitmapToImageSource(bitmap);
                    if (!esModoRegistro)
                        txtEstado.Text = rostroDetectado ? "Rostro detectado" : "Buscando...";
                });
            }
        }

        private async void FinalizarAcceso(string nombre)
        {
            if (accesoOtorgado) return;
            accesoOtorgado = true;

            Dispatcher.Invoke(() =>
            {
                txtNombreReconocido.Text = $"Bienvenido, {nombre}!";
                txtEstado.Text = "Acceso concedido. Entrando en 5 segundos...";
            });

            await Task.Delay(5000);

            Dispatcher.Invoke(() =>
            {
                DetenerCamara();
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            });
        }

        private BitmapImage BitmapToImageSource(Drawing.Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, ImageFormat.Bmp);
            memory.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private Drawing.Bitmap? ObtenerFrameActual()
        {
            if (imgCamara.Source is BitmapSource bitmapSource)
            {
                Drawing.Bitmap bmp = new Drawing.Bitmap(
                    bitmapSource.PixelWidth,
                    bitmapSource.PixelHeight,
                    PixelFormat.Format32bppPArgb);

                BitmapData data = bmp.LockBits(
                    new Drawing.Rectangle(Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppPArgb);

                bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                bmp.UnlockBits(data);
                return bmp;
            }
            return null;
        }

        private void EntrenarReconocedor()
        {
            if (personasRegistradas.Count == 0) return;

            reconocedor = new LBPHFaceRecognizer(1, 8, 8, 8, UMBRAL_RECONOCIMIENTO);

            using var matVector = new VectorOfMat();
            using var etiquetas = new VectorOfInt();

            for (int i = 0; i < personasRegistradas.Count; i++)
            {
                using var imgColor = personasRegistradas[i].Foto.ToImage<Bgr, byte>();
                using var imgGris = imgColor.Convert<Gray, byte>()
                    .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
                matVector.Push(imgGris.Mat);
                etiquetas.Push(new int[] { i });
            }

            // Imagen de ruido como "desconocido" de referencia
            using var imgNoise = new Image<Gray, byte>(100, 100);
            var rng = new Random(42);
            for (int y = 0; y < 100; y++)
                for (int x = 0; x < 100; x++)
                    imgNoise.Data[y, x, 0] = (byte)rng.Next(256);

            matVector.Push(imgNoise.Mat);
            etiquetas.Push(new int[] { personasRegistradas.Count });

            reconocedor.Train(matVector, etiquetas);
            reconocedorEntrenado = true;
        }

        private void btnCapturarFoto_Click(object sender, RoutedEventArgs e)
        {
            if (!esModoRegistro)
            {
                MessageBox.Show("Activa el Modo Registro primero antes de capturar.");
                return;
            }
            if (!rostroDetectado) { MessageBox.Show("No hay rostro detectado."); return; }

            Drawing.Bitmap? frame = ObtenerFrameActual();
            if (frame == null) { MessageBox.Show("No se pudo obtener el frame."); return; }

            using var imgEmgu = frame.ToImage<Bgr, byte>();
            using var imgGris = imgEmgu.Convert<Gray, byte>();
            var rostros = detectorRostros!.DetectMultiScale(imgGris, 1.1, 10, new Drawing.Size(80, 80));

            if (rostros.Length > 0)
            {
                fotoCapturada?.Dispose();
                fotoCapturada = imgGris.Copy(rostros[0])
                                       .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear)
                                       .ToBitmap();
                txtEstado.Text = "Rostro capturado ✓ — Ingresa el nombre y registra";
            }
            else
            {
                MessageBox.Show("No se detectó un rostro claro. Intenta de nuevo.");
            }

            frame.Dispose();
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombrePersona.Text.Trim();

            if (string.IsNullOrEmpty(nombre)) { MessageBox.Show("Ingresa un nombre."); return; }
            if (fotoCapturada == null) { MessageBox.Show("Primero captura una foto."); return; }

            GuardarPersonaEnBD(nombre, fotoCapturada);
            CargarPersonasDesdeBD();

            txtNombrePersona.Text = "";
            fotoCapturada?.Dispose();
            fotoCapturada = null;
        }

        private void DetenerCamara()
        {
            if (camaraActiva != null && camaraActiva.IsRunning)
            {
                camaraActiva.SignalToStop();
                camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                camaraActiva = null;
            }
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            OpcionSesion ventana = new OpcionSesion("");
            ventana.Show();
            this.Close();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e) => DetenerCamara();

        protected override void OnClosed(EventArgs e)
        {
            DetenerCamara();
            base.OnClosed(e);
        }
    }
}