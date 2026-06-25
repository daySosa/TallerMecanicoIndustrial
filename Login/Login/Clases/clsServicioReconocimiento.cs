using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Drawing = System.Drawing;

namespace Login.Clases
{
    /// <summary>
    /// Encapsula el ciclo de vida del reconocedor LBPH:
    /// entrenamiento con aumento de datos, predicción y preparación de rostros.
    /// </summary>
    public class clsServicioReconocimiento : IDisposable
    {
        // ── Hiperparámetros LBPH ──────────────────────────────────────────────────

        private const int LbphRadius = 2;
        private const int LbphNeighbors = 8;
        private const int LbphGridX = 8;
        private const int LbphGridY = 8;

        /// <summary>Tamaño (px) al que se normaliza cada rostro.</summary>
        public const int TamanoRostro = 100;

        // ── Estado interno ────────────────────────────────────────────────────────

        private LBPHFaceRecognizer? _reconocedor;

        /// <summary>
        /// Indica si el modelo ha sido entrenado con al menos una persona.
        /// </summary>
        public bool Entrenado { get; private set; }

        // ── Entrenamiento ─────────────────────────────────────────────────────────

        /// <summary>
        /// Entrena el modelo LBPH con las personas registradas.
        /// Aplica aumento de datos (espejo y variaciones de brillo) y muestras
        /// de ruido sintético para reducir falsos positivos.
        /// </summary>
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

        /// <summary>
        /// Genera cuatro variantes de la foto para enriquecer el entrenamiento:
        /// original, espejo horizontal, brillo alto (+20) y brillo bajo (-20).
        /// </summary>
        private static void AgregarMuestraConAumentacion(
            Drawing.Bitmap foto,
            int etiqueta,
            VectorOfMat mats,
            VectorOfInt etiquetas)
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

            // Brillo alto
            using var brilloAlto = imgGris.Add(new Gray(20));
            mats.Push(brilloAlto.Mat);
            etiquetas.Push(new[] { etiqueta });

            // Brillo bajo
            using var brilloBajo = imgGris.Sub(new Gray(20));
            mats.Push(brilloBajo.Mat);
            etiquetas.Push(new[] { etiqueta });
        }

        /// <summary>
        /// Agrega cuatro imágenes de ruido aleatorio para reducir falsos positivos.
        /// Se etiquetan con un índice fuera del rango de personas reales.
        /// </summary>
        private static void AgregarRuidoSintetico(
            int etiquetaRuido,
            VectorOfMat mats,
            VectorOfInt etiquetas)
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

        // ── Predicción ────────────────────────────────────────────────────────────

        /// <summary>
        /// Predice la identidad del rostro proporcionado.
        /// Devuelve (-1, double.MaxValue) si el modelo no está entrenado.
        /// </summary>
        public (int Label, double Distance) Predecir(Image<Gray, byte> rostroGris)
        {
            if (!Entrenado || _reconocedor == null)
                return (-1, double.MaxValue);

            using var normalizado = PrepararRostro(rostroGris.ToBitmap());
            var resultado = _reconocedor.Predict(normalizado.Mat);
            return (resultado.Label, resultado.Distance);
        }

        // ── Utilidades ────────────────────────────────────────────────────────────

        /// <summary>
        /// Convierte un Bitmap a escala de grises, lo redimensiona y aplica
        /// ecualización de histograma para normalizar la iluminación.
        /// </summary>
        public static Image<Gray, byte> PrepararRostro(Drawing.Bitmap bitmap)
        {
            using var img = bitmap.ToImage<Bgr, byte>();
            using var gris = img.Convert<Gray, byte>()
                                .Resize(TamanoRostro, TamanoRostro, Emgu.CV.CvEnum.Inter.Linear);

            var ecualizado = new Image<Gray, byte>(TamanoRostro, TamanoRostro);
            CvInvoke.EqualizeHist(gris, ecualizado);
            return ecualizado;
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        /// <summary>Libera el reconocedor LBPH.</summary>
        public void Dispose()
        {
            _reconocedor?.Dispose();
            _reconocedor = null;
            Entrenado = false;
        }
    }
}