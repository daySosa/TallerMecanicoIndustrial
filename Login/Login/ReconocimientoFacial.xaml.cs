using AForge.Video;
using AForge.Video.DirectShow;
using Emgu.CV;
using Emgu.CV.Structure;
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

        public ReconocimientoFacial()
        {
            InitializeComponent();

            string rutaXml = "haarcascade_frontalface_default.xml";
            if (File.Exists(rutaXml))
                detectorRostros = new CascadeClassifier(rutaXml);
            else
                txtEstado.Text = "No se encontro el archivo haarcascade";
        }

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (camarasDisponibles.Count == 0)
            {
                txtEstado.Text = "No se encontro camara";
                return;
            }

            camaraActiva = new VideoCaptureDevice(camarasDisponibles[0].MonikerString);
            camaraActiva.NewFrame += CamaraActiva_NewFrame;
            camaraActiva.Start();

            txtSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
        }

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Drawing.Bitmap bitmap = (Drawing.Bitmap)eventArgs.Frame.Clone();

            if (detectorRostros != null)
            {
                using Image<Bgr, byte> imagenEmgu = bitmap.ToImage<Bgr, byte>();
                using Image<Gray, byte> imagenGris = imagenEmgu.Convert<Gray, byte>();

                var rostros = detectorRostros.DetectMultiScale(
                    imagenGris, 1.1, 10, new Drawing.Size(80, 80));

                rostroDetectado = rostros.Length > 0;

                foreach (var rostro in rostros)
                {
                    using Drawing.Graphics g = Drawing.Graphics.FromImage(bitmap);
                    g.DrawRectangle(new Drawing.Pen(Drawing.Color.LimeGreen, 3), rostro);
                }

                Dispatcher.Invoke(() =>
                {
                    txtEstado.Text = rostroDetectado
                        ? "Rostro detectado"
                        : "Buscando rostro...";
                });
            }

            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = BitmapToImageSource(bitmap);
            });

            bitmap.Dispose();
        }

        private void btnDetenerCamara_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
        }

        private void DetenerCamara()
        {
            if (camaraActiva != null && camaraActiva.IsRunning)
            {
                camaraActiva.SignalToStop();
                camaraActiva.WaitForStop();
                camaraActiva = null;
            }

            Dispatcher.Invoke(() =>
            {
                imgCamara.Source = null;
                txtSinCamara.Visibility = Visibility.Visible;
                txtEstado.Text = "Estado: En espera...";
                btnIniciarCamara.IsEnabled = true;
                btnDetenerCamara.IsEnabled = false;
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