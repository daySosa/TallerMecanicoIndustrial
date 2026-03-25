using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Drawing = System.Drawing;

namespace Login.Clases
{
    public class clsServicioReconocimiento : IDisposable
    {
        private const int LbphRadius = 2;
        private const int LbphNeighbors = 8;
        private const int LbphGridX = 8;
        private const int LbphGridY = 8;
        public const int TamanoRostro = 100;

        private LBPHFaceRecognizer? _reconocedor;
        public bool Entrenado { get; private set; }

        public void Entrenar(IReadOnlyList<(int Id, string Nombre, Drawing.Bitmap Foto)> personas)
        {
            if (personas.Count == 0) return;

            _reconocedor?.Dispose();
            _reconocedor = new LBPHFaceRecognizer(
                LbphRadius, LbphNeighbors, LbphGridX, LbphGridY,
                double.MaxValue);

            using var mats = new VectorOfMat();
            using var etiquetas = new VectorOfInt();

            for (int i = 0; i < personas.Count; i++)
                AgregarMuestraConAumentacion(personas[i].Foto, i, mats, etiquetas);

            AgregarRuidoSintetico(personas.Count, mats, etiquetas);

            _reconocedor.Train(mats, etiquetas);
            Entrenado = true;
        }

        private static void AgregarMuestraConAumentacion(
            Drawing.Bitmap foto, int etiqueta,
            VectorOfMat mats, VectorOfInt etiquetas)
        {
            using var imgColor = foto.ToImage<Bgr, byte>();
            using var imgGris = PrepararRostro(imgColor.ToBitmap());

            // Original
            mats.Push(imgGris.Mat);
            etiquetas.Push(new[] { etiqueta });

            // Espejo horizontal
            using var espejo = imgGris.Flip(Emgu.CV.CvEnum.FlipType.Horizontal);
            mats.Push(espejo.Mat);
            etiquetas.Push(new[] { etiqueta });

            // Brillo +20
            using var brilloAlto = imgGris.Add(new Gray(20));
            mats.Push(brilloAlto.Mat);
            etiquetas.Push(new[] { etiqueta });

            // Brillo -20
            using var brilloBajo = imgGris.Sub(new Gray(20));
            mats.Push(brilloBajo.Mat);
            etiquetas.Push(new[] { etiqueta });
        }

        private static void AgregarRuidoSintetico(int etiquetaRuido,
            VectorOfMat mats, VectorOfInt etiquetas)
        {
            var rng = new Random(42);
            for (int k = 0; k < 4; k++)
            {
                using var imgRuido = new Image<Gray, byte>(TamanoRostro, TamanoRostro);
                for (int y = 0; y < TamanoRostro; y++)
                    for (int x = 0; x < TamanoRostro; x++)
                        imgRuido.Data[y, x, 0] = (byte)rng.Next(256);

                mats.Push(imgRuido.Mat);
                etiquetas.Push(new[] { etiquetaRuido });
            }
        }

        public (int Label, double Distance) Predecir(Image<Gray, byte> rostroGris)
        {
            if (!Entrenado || _reconocedor == null)
                return (-1, double.MaxValue);

            using var normalizado = PrepararRostro(rostroGris.ToBitmap());
            var resultado = _reconocedor.Predict(normalizado.Mat);
            return (resultado.Label, resultado.Distance);
        }

        public static Image<Gray, byte> PrepararRostro(Drawing.Bitmap bitmap)
        {
            using var img = bitmap.ToImage<Bgr, byte>();
            using var gris = img.Convert<Gray, byte>()
                                .Resize(TamanoRostro, TamanoRostro, Emgu.CV.CvEnum.Inter.Linear);

            var ecualizado = new Image<Gray, byte>(TamanoRostro, TamanoRostro);
            CvInvoke.EqualizeHist(gris, ecualizado);
            return ecualizado;
        }

        public void Dispose()
        {
            _reconocedor?.Dispose();
            _reconocedor = null;
        }
    }
}