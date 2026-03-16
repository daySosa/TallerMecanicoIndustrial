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
        private FilterInfoCollection? _camarasDisponibles;
        private VideoCaptureDevice? _camaraActiva;
        private CascadeClassifier? _detectorRostros;
        private bool _rostroDetectado = false;
        private bool _accesoOtorgado = false;
        private bool _esModoRegistro = false;
        private readonly List<(int Id, string Nombre, Drawing.Bitmap Foto)> _personasRegistradas = new();
        private Drawing.Bitmap? _fotoCapturada = null;
        private LBPHFaceRecognizer? _reconocedor;
        private bool _reconocedorEntrenado = false;
        private readonly string _correoUsuario;
        private const double UmbralReconocimiento = 100.0;

        public ReconocimientoFacial(string correo = "")
        {
            InitializeComponent();
            _correoUsuario = correo;

            string rutaXml = "haarcascade_frontalface_default.xml";
            if (File.Exists(rutaXml))
                _detectorRostros = new CascadeClassifier(rutaXml);
            else
                txtEstado.Text = "No se encontró el archivo XML de detección de rostros.";

            CargarPersonasDesdeBD();
        }

        private void CargarPersonasDesdeBD()
        {
            try
            {
                foreach (var p in _personasRegistradas) p.Foto?.Dispose();
                _personasRegistradas.Clear();

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
                    _personasRegistradas.Add((id, nombre, foto));
                }

                conexion.Cerrar();
                if (_personasRegistradas.Count > 0)
                    EntrenarReconocedor();

                txtEstado.Text = $"{_personasRegistradas.Count} persona(s) cargadas.";
            }
            catch (Exception ex)
            {
                txtEstado.Text = "Error al cargar datos: " + ex.Message;
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

                MessageBox.Show($"Persona '{nombre}' registrada correctamente.",
                    "Registro exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnModoRegistro_Click(object sender, RoutedEventArgs e)
        {
            _esModoRegistro = true;
            txtEstado.Text = "Modo Registro — El reconocimiento está desactivado.";
            txtNombreReconocido.Text = "";
            btnModoRegistro.IsEnabled = false;
            btnModoLogin.IsEnabled = true;
        }

        private void btnModoLogin_Click(object sender, RoutedEventArgs e)
        {
            _esModoRegistro = false;
            _accesoOtorgado = false;
            _fotoCapturada = null;
            txtEstado.Text = "Modo Login — Coloca tu cara frente a la cámara.";
            txtNombreReconocido.Text = "";
            btnModoLogin.IsEnabled = false;
            btnModoRegistro.IsEnabled = true;
        }

        private void btnIniciarCamara_Click(object sender, RoutedEventArgs e)
        {
            _camarasDisponibles = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (_camarasDisponibles.Count == 0)
            {
                MessageBox.Show("No se encontró ninguna cámara disponible.",
                    "Sin cámara", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _accesoOtorgado = false;
            _esModoRegistro = false;
            _camaraActiva = new VideoCaptureDevice(_camarasDisponibles[0].MonikerString);
            _camaraActiva.NewFrame += CamaraActiva_NewFrame;
            _camaraActiva.Start();

            txtSinCamara.Visibility = Visibility.Collapsed;
            btnIniciarCamara.IsEnabled = false;
            btnDetenerCamara.IsEnabled = true;
            btnCapturarFoto.IsEnabled = true;
            btnModoRegistro.IsEnabled = true;
            btnModoLogin.IsEnabled = false;
        }

        private void CamaraActiva_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (_accesoOtorgado) return;

            using (Drawing.Bitmap bitmap = (Drawing.Bitmap)eventArgs.Frame.Clone())
            {
                if (_detectorRostros != null)
                {
                    using var imgEmgu = bitmap.ToImage<Bgr, byte>();
                    using var imgGris = imgEmgu.Convert<Gray, byte>();
                    var rostros = _detectorRostros.DetectMultiScale(imgGris, 1.1, 10, new Drawing.Size(80, 80));

                    _rostroDetectado = rostros.Length > 0;

                    foreach (var rostro in rostros)
                    {
                        var colorRect = _esModoRegistro ? Drawing.Color.Orange : Drawing.Color.LimeGreen;
                        using (Drawing.Graphics g = Drawing.Graphics.FromImage(bitmap))
                            g.DrawRectangle(new Drawing.Pen(colorRect, 3), rostro);

                        if (!_esModoRegistro && _reconocedorEntrenado && _reconocedor != null)
                        {
                            using var rostroRecortado = imgGris.Copy(rostro)
                                .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);

                            var resultado = _reconocedor.Predict(rostroRecortado.Mat);

                            bool esDesconocido = resultado.Label < 0
                                              || resultado.Label >= _personasRegistradas.Count
                                              || resultado.Distance >= UmbralReconocimiento;

                            if (!esDesconocido)
                            {
                                string nombre = _personasRegistradas[resultado.Label].Nombre;
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
                    if (!_esModoRegistro)
                        txtEstado.Text = _rostroDetectado ? "Rostro detectado." : "Buscando rostro...";
                });
            }
        }

        private async void FinalizarAcceso(string nombre)
        {
            if (_accesoOtorgado) return;
            _accesoOtorgado = true;

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
            if (_personasRegistradas.Count == 0) return;

            _reconocedor = new LBPHFaceRecognizer(1, 8, 8, 8, UmbralReconocimiento);

            using var matVector = new VectorOfMat();
            using var etiquetas = new VectorOfInt();

            for (int i = 0; i < _personasRegistradas.Count; i++)
            {
                using var imgColor = _personasRegistradas[i].Foto.ToImage<Bgr, byte>();
                using var imgGris = imgColor.Convert<Gray, byte>()
                    .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear);
                matVector.Push(imgGris.Mat);
                etiquetas.Push(new int[] { i });
            }

            using var imgRuido = new Image<Gray, byte>(100, 100);
            var rng = new Random(42);
            for (int y = 0; y < 100; y++)
                for (int x = 0; x < 100; x++)
                    imgRuido.Data[y, x, 0] = (byte)rng.Next(256);

            matVector.Push(imgRuido.Mat);
            etiquetas.Push(new int[] { _personasRegistradas.Count });

            _reconocedor.Train(matVector, etiquetas);
            _reconocedorEntrenado = true;
        }

        private void btnCapturarFoto_Click(object sender, RoutedEventArgs e)
        {
            if (!_esModoRegistro)
            {
                MessageBox.Show("Activa el Modo Registro antes de capturar una foto.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!_rostroDetectado)
            {
                MessageBox.Show("No hay ningún rostro detectado.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Drawing.Bitmap? frame = ObtenerFrameActual();
            if (frame == null)
            {
                MessageBox.Show("No se pudo obtener el fotograma actual.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using var imgEmgu = frame.ToImage<Bgr, byte>();
            using var imgGris = imgEmgu.Convert<Gray, byte>();
            var rostros = _detectorRostros!.DetectMultiScale(imgGris, 1.1, 10, new Drawing.Size(80, 80));

            if (rostros.Length > 0)
            {
                _fotoCapturada?.Dispose();
                _fotoCapturada = imgGris.Copy(rostros[0])
                                        .Resize(100, 100, Emgu.CV.CvEnum.Inter.Linear)
                                        .ToBitmap();
                txtEstado.Text = "Rostro capturado correctamente. Ingresa el nombre y registra.";
            }
            else
            {
                MessageBox.Show("No se detectó un rostro claro. Intenta de nuevo.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            frame.Dispose();
        }

        private void btnRegistrar_Click(object sender, RoutedEventArgs e)
        {
            string nombre = txtNombrePersona.Text.Trim();

            if (string.IsNullOrEmpty(nombre))
            {
                MessageBox.Show("⚠ Ingresa el nombre de la persona.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!nombre.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            {
                MessageBox.Show("⚠ El nombre solo puede contener letras y espacios.",
                    "Nombre inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_fotoCapturada == null)
            {
                MessageBox.Show("⚠ Primero captura una foto.",
                    "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GuardarPersonaEnBD(nombre, _fotoCapturada);
            CargarPersonasDesdeBD();

            txtNombrePersona.Text = "";
            _fotoCapturada?.Dispose();
            _fotoCapturada = null;
        }

        private void DetenerCamara()
        {
            if (_camaraActiva != null && _camaraActiva.IsRunning)
            {
                _camaraActiva.SignalToStop();
                _camaraActiva.NewFrame -= CamaraActiva_NewFrame;
                _camaraActiva = null;
            }
        }

        private void btnRegresar_Click(object sender, RoutedEventArgs e)
        {
            DetenerCamara();
            OpcionSesion ventana = new OpcionSesion(_correoUsuario);
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