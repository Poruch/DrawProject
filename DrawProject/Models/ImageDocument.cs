using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawProject.Models
{
    public class ImageDocument
    {
        // === ДАННЫЕ ИЗОБРАЖЕНИЯ ===
        public WriteableBitmap Bitmap { get; private set; }

        public WriteableBitmap ActiveLayer
        {
            get => Bitmap;
        }
        public int Width { get; }
        public int Height { get; }

        // === КОНСТРУКТОР ===
        public ImageDocument(int width, int height)
        {
            Width = width;
            Height = height;
            InitializeBitmap();
        }

        private void InitializeBitmap()
        {
            Bitmap = new WriteableBitmap(Width, Height, 96, 96,
                PixelFormats.Pbgra32, null);
            Clear();
        }

        // === ОЧИСТКА ===
        public void Clear()
        {
            var pixels = new byte[Width * Height * 4];
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;     // B
                pixels[i + 1] = 255; // G
                pixels[i + 2] = 255; // R
                pixels[i + 3] = 255; // A
            }

            Bitmap.WritePixels(new Int32Rect(0, 0, Width, Height),
                pixels, Width * 4, 0);
        }

        public WriteableBitmap GetCompositeImage()
        {
            return Bitmap;
            // 1. Создаем пустой битмап
            var composite = new WriteableBitmap(Width, Height, 96, 96,
                PixelFormats.Pbgra32, null);

            // 2. Очищаем его (прозрачный)
            //ClearBitmap(composite);

            // 3. Накладываем ВСЕ видимые слои сверху вниз
            //foreach (var layer in Layers.Where(l => l.IsVisible))
            {
                //BlendLayer(composite, layer);
            }

            return composite; // ← Возвращаем WriteableBitmap!
        }

        // === ПЕРЕНОС ВЕКТОРА В РАСТР ===
        public void ApplyVectorLayer(BitmapSource vectorLayer)
        {
            // Смешиваем векторный слой с растровым
            BlendBitmaps(vectorLayer);
        }

        private void BlendBitmaps(BitmapSource source)
        {
            int width = Bitmap.PixelWidth;
            int height = Bitmap.PixelHeight;

            byte[] rasterPixels = new byte[width * height * 4];
            byte[] vectorPixels = new byte[width * height * 4];

            Bitmap.CopyPixels(rasterPixels, width * 4, 0);
            source.CopyPixels(vectorPixels, width * 4, 0);

            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                double alpha = vectorPixels[i + 3] / 255.0;

                if (alpha > 0)
                {
                    rasterPixels[i] = (byte)(vectorPixels[i] * alpha +
                        rasterPixels[i] * (1 - alpha));
                    rasterPixels[i + 1] = (byte)(vectorPixels[i + 1] * alpha +
                        rasterPixels[i + 1] * (1 - alpha));
                    rasterPixels[i + 2] = (byte)(vectorPixels[i + 2] * alpha +
                        rasterPixels[i + 2] * (1 - alpha));
                    rasterPixels[i + 3] = 255;
                }
            }

            Bitmap.WritePixels(new Int32Rect(0, 0, width, height),
                rasterPixels, width * 4, 0);
        }
    }
}