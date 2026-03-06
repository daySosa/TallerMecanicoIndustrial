using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks; // Necesario para el delay
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
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

        private List<(int Id, string Nombre, Drawing.Bitmap Foto)> personasRegistradas = new();
        private Drawing.Bitmap? fotoCapturada = null;

        private EigenFaceRecognizer? reconocedor;
        private bool reconocedorEntrenado = false;

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

        // ══ CARGAR PERSONAS DESDE BD ══
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

        // ══ GUARDAR PERSONA EN BD ══
        private void GuardarPersonaEnBD(string nombre, Drawing.Bitmap foto)
        {
            try
            {
                // Creamos una copia para que la cámara no bloquee el archivo
                using (Drawing.Bitmap copia = new Drawing.Bitmap(foto))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        copia.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] fotoBytes = ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        // ══ CÁMARA Y PROCESAMIENTO ══
        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (camarasDisponibles.Count == 0) return;

            accesoOtorgado = false;
            camaraActiva = new VideoCaptureDevice(camarasDisponibles[0].MonikerString);
            camaraActiva.NewFrame += CamaraActiva_NewFrame;
            camaraActiva.Start();

            txtSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            btnCapturarFoto.IsEnabled = true;
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
                        using (Drawing.Graphics g = Drawing.Graphics.FromImage(bitmap))
                        {
                            g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 3), rostro);
                        }

                        if (reconocedorEntrenado && reconocedor != null)
                        {
                            using var rostroRecortado = imgGris.Copy(rostro).Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
                            var resultado = reconocedor.Predict(rostroRecortado.Mat);

                            // Umbral de reconocimiento
                            if (resultado.Distance < 3500 && resultado.Label >= 0)
                            {
                                string nombre = personasRegistradas[resultado.Label].Nombre;
                                FinalizarAcceso(nombre); // Llamada al método con delay
                            }
                        }
                    }
                }

                Dispatcher.Invoke(() => {
                    imgCamara.Source = BitmapToImageSource(bitmap);
                    txtEstado.Text = rostroDetectado ? "Rostro detectado" : "Buscando...";
                });
            }
        }

        // MÉTODO MODIFICADO: Espera 5 segundos antes de entrar
        private async void FinalizarAcceso(string nombre)
        {
            if (accesoOtorgado) return;
            accesoOtorgado = true; // Bloquea futuros escaneos mientras espera

            Dispatcher.Invoke(() => {
                txtNombreReconocido.Text = $"Bienvenido, {nombre}!";
                txtEstado.Text = "Acceso concedido. Entrando en 5 segundos...";
            });

            // Espera asíncrona para no congelar la cámara
            await Task.Delay(5000);

            Dispatcher.Invoke(() => {
                DetenerCamara();
                new Dasboard_Prueba.MenuPrincipal().Show();
                this.Close();
            });
        }

        // ══ CONVERSIÓN Y CAPTURA ══
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
                    bitmapSource.PixelWidth, bitmapSource.PixelHeight, PixelFormat.Format32bppPArgb);

                BitmapData data = bmp.LockBits(
                    new Drawing.Rectangle(Drawing.Point.Empty, bmp.Size),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

                bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                bmp.UnlockBits(data);
                return bmp;
            }
            return null;
        }

        private void EntrenarReconocedor()
        {
            if (personasRegistradas.Count == 0) return;
            reconocedor = new EigenFaceRecognizer(personasRegistradas.Count, 5000);

            using var matVector = new VectorOfMat();
            using var etiquetas = new VectorOfInt();

            for (int i = 0; i < personasRegistradas.Count; i++)
            {
                using var imgColor = personasRegistradas[i].Foto.ToImage<Bgr, byte>();
                using var imgGris = imgColor.Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
                matVector.Push(imgGris.Mat);
                etiquetas.Push(new int[] { i });
            }
            reconocedor.Train(matVector, etiquetas);
            reconocedorEntrenado = true;
        }

        // ══ EVENTOS DE BOTONES ══
        private void btnCapturarFoto_Click(object sender, RoutedEventArgs e)
        {
            if (!rostroDetectado) { MessageBox.Show("No hay rostro."); return; }
            fotoCapturada?.Dispose();
            fotoCapturada = ObtenerFrameActual();
            txtEstado.Text = "Foto capturada";
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombrePersona.Text.Trim();
            if (string.IsNullOrEmpty(nombre) || fotoCapturada == null) return;

            GuardarPersonaEnBD(nombre, fotoCapturada);
            CargarPersonasDesdeBD();
            txtNombrePersona.Text = "";
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
            // Abrimos la ventana de opciones de sesión
            OpcionSesion ventana = new OpcionSesion("");
            ventana.Show();
            this.Close();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e) => DetenerCamara();

        protected override void OnClosed(EventArgs e) { DetenerCamara(); base.OnClosed(e); }
    }
}