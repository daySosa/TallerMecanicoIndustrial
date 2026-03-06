using AForge.Video;
using AForge.Video.DirectShow;
using Dasboard_Prueba;
using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Collections.Generic;
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
                txtEstado.Text = "No se encontro haarcascade_frontalface_default.xml";

            CargarPersonasDesdeBD();
        }

        // ══ CARGAR PERSONAS DESDE BD ══
        private void CargarPersonasDesdeBD()
        {
            try
            {
                personasRegistradas.Clear();

                clsConexion conexion = new clsConexion();
                conexion.Abrir();

                string query = "SELECT Id, Nombre, Foto FROM ReconocimientoFacial";
                SqlCommand cmd = new SqlCommand(query, conexion.SqlC);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nombre = reader.GetString(1);
                    byte[] fotoBytes = (byte[])reader[2];

                    using MemoryStream ms = new MemoryStream(fotoBytes);
                    Drawing.Bitmap foto = new Drawing.Bitmap(ms);
                    personasRegistradas.Add((id, nombre, foto));
                }

                reader.Close();
                conexion.Cerrar();

                if (personasRegistradas.Count > 0)
                    EntrenarReconocedor();

                txtEstado.Text = $"{personasRegistradas.Count} persona(s) cargadas de BD";
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
                // Creamos una copia nueva para evitar el error de GDI+
                using (Drawing.Bitmap copiaSegura = new Drawing.Bitmap(foto))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        copiaSegura.Save(ms, ImageFormat.Png); // Aquí ya no fallará
                        byte[] fotoBytes = ms.ToArray();
                        // ... resto de tu código SQL ...
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error GDI+: " + ex.Message);
            }
        }

        // ══ CÁMARA ══
        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (camarasDisponibles.Count == 0)
            {
                txtEstado.Text = "No se encontro camara";
                return;
            }

            accesoOtorgado = false;
            camaraActiva = new VideoCaptureDevice(camarasDisponibles[0].MonikerString);
            camaraActiva.NewFrame += CamaraActiva_NewFrame;
            camaraActiva.Start();

            txtSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            btnCapturarFoto.IsEnabled = true;
            txtEstado.Text = "Estado: Camara activa";
        }

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (accesoOtorgado) return;

            Drawing.Bitmap bitmap = (Drawing.Bitmap)eventArgs.Frame.Clone();

            if (detectorRostros != null)
            {
                using Image<Bgr, byte> imgEmgu = bitmap.ToImage<Bgr, byte>();
                using Image<Gray, byte> imgGris = imgEmgu.Convert<Gray, byte>();

                var rostros = detectorRostros.DetectMultiScale(
                    imgGris, 1.1, 10, new Drawing.Size(80, 80));

                rostroDetectado = rostros.Length > 0;

                foreach (var rostro in rostros)
                {
                    using Drawing.Graphics g = Drawing.Graphics.FromImage(bitmap);
                    g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 3), rostro);

                    if (reconocedorEntrenado && reconocedor != null)
                    {
                        using Image<Gray, byte> rostroRecortado = imgGris
                            .Copy(rostro)
                            .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

                        var resultado = reconocedor.Predict(rostroRecortado.Mat);

                        if (resultado.Distance < 5000 && resultado.Label >= 0
                            && resultado.Label < personasRegistradas.Count)
                        {
                            string nombre = personasRegistradas[resultado.Label].Nombre;
                            accesoOtorgado = true;

                            Dispatcher.Invoke(() =>
                            {
                                txtNombreReconocido.Text = $"Bienvenido, {nombre}!";
                                txtEstado.Text = "Acceso concedido, abriendo menu...";
                                DetenerCamara();

                                MenuPrincipal menu = new MenuPrincipal();
                                menu.Show();
                                this.Close();
                            });
                        }
                        else
                        {
                            Dispatcher.Invoke(() =>
                            {
                                txtNombreReconocido.Text = "Persona no reconocida";
                                txtEstado.Text = "Rostro no registrado";
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtEstado.Text = "Rostro detectado";
                        });
                    }
                }

                if (!rostroDetectado)
                {
                    Dispatcher.Invoke(() =>
                    {
                        txtEstado.Text = "Buscando rostro...";
                        txtNombreReconocido.Text = "";
                    });
                }
            }

            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = BitmapToImageSource(bitmap);
            });

            bitmap.Dispose();
        }

        // ══ CAPTURAR FOTO ══
        private void btnCapturarFoto_Click(object sender, RoutedEventArgs e)
        {
            if (!rostroDetectado)
            {
                MessageBox.Show("Asegurate de que haya un rostro visible.",
                    "Sin rostro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            fotoCapturada = ObtenerFrameActual();
            txtEstado.Text = "Foto capturada correctamente";
        }

        // ══ REGISTRAR PERSONA ══
        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombrePersona.Text.Trim();

            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("Escribe el nombre.", "Campo vacio",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (fotoCapturada == null)
            {
                MessageBox.Show("Captura una foto primero.", "Sin foto",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GuardarPersonaEnBD(nombre, fotoCapturada);
            CargarPersonasDesdeBD();

            fotoCapturada = null;
            txtNombrePersona.Text = "";

            MessageBox.Show($"'{nombre}' registrado exitosamente.", "Registrado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ══ ENTRENAR RECONOCEDOR ══
        private void EntrenarReconocedor()
        {
            if (personasRegistradas.Count == 0) return;

            reconocedor = new EigenFaceRecognizer(personasRegistradas.Count, 5000);

            var matVector = new VectorOfMat();
            var etiquetas = new VectorOfInt();

            for (int i = 0; i < personasRegistradas.Count; i++)
            {
                using Image<Bgr, byte> imgColor = personasRegistradas[i].Foto
                    .ToImage<Bgr, byte>();
                using Image<Gray, byte> imgGris = imgColor
                    .Convert<Gray, byte>()
                    .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

                matVector.Push(imgGris.Mat);
                etiquetas.Push(new int[] { i });
            }

            reconocedor.Train(matVector, etiquetas);
            reconocedorEntrenado = true;
        }

        private Drawing.Bitmap? ObtenerFrameActual()
        {
            if (imgCamara.Source is BitmapImage bmp)
            {
                using MemoryStream ms = new MemoryStream();
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                encoder.Save(ms);
                ms.Position = 0;
                return new Drawing.Bitmap(ms);
            }
            return null;
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            OpcionSesion ventana = new OpcionSesion("");
            ventana.Show();
            this.Close();
        }

        private void DetenerCamara()
        {
            if (camaraActiva != null)
            {
                camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                camaraActiva.SignalToStop();
                camaraActiva = null;
            }

            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = null;
                txtSinCamara.Visibility = Visibility.Visible;
                txtEstado.Text = "Estado: En espera...";
                txtNombreReconocido.Text = "";
                btnIniciarCamara.IsEnabled = true;
                btnDetenerCamara.IsEnabled = false;
                btnCapturarFoto.IsEnabled = false;
            });
        }

        private BitmapImage BitmapToImageSource(Drawing.Bitmap bitmap)
        {
            using MemoryStream memory = new MemoryStream();
            bitmap.Save(memory, Drawing.Imaging.ImageFormat.Bmp);
            memory.Position = 0;

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            DetenerCamara();
            base.OnClosed(e);
        }
    }
}