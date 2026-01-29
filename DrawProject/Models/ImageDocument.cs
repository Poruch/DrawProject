using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawProject.Models
{
    public class ImageDocument : IDisposable
    {
        // === ДАННЫЕ ИЗОБРАЖЕНИЯ ===
        public WriteableBitmap Bitmap { get; private set; }
        public WriteableBitmap ActiveLayer => Bitmap;
        public int Width { get; }
        public int Height { get; }

        // Предвычисленные значения
        private readonly int _stride;
        private readonly int _bufferSize;

        // КЭШИРОВАННЫЕ БУФЕРЫ (создаются один раз!)
        private readonly byte[] _vectorBuffer;
        private readonly byte[] _rasterBuffer; // ← Добавляем буфер для raster

        // === КОНСТРУКТОР ===
        public ImageDocument(int width, int height)
        {
            Width = width;
            Height = height;
            _stride = width * 4;
            _bufferSize = _stride * height;

            // Создаем буферы ОДИН РАЗ в конструкторе
            _vectorBuffer = new byte[_bufferSize];
            _rasterBuffer = new byte[_bufferSize]; // ← Кэшируем raster буфер

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
            Bitmap.Lock();
            try
            {
                // Используем WritePixels для быстрой очистки
                var clearPixels = new byte[_bufferSize];
                for (int i = 0; i < clearPixels.Length; i += 4)
                {
                    clearPixels[i] = 255;     // B
                    clearPixels[i + 1] = 255; // G
                    clearPixels[i + 2] = 255; // R
                    clearPixels[i + 3] = 255; // A
                }

                Bitmap.WritePixels(new Int32Rect(0, 0, Width, Height),
                    clearPixels, _stride, 0);
            }
            finally
            {
                Bitmap.Unlock();
            }
        }

        public WriteableBitmap GetCompositeImage()
        {
            return Bitmap;
        }

        // === ПЕРЕНОС ВЕКТОРА В РАСТР ===
        public void ApplyVectorLayer(BitmapSource vectorLayer, bool isEraser = false)
        {
            if (vectorLayer == null) return;


            // Копируем векторный слой в кэшированный буфер
            vectorLayer.CopyPixels(_vectorBuffer, _stride, 0);

            // Копируем текущий Bitmap в кэшированный буфер
            Bitmap.CopyPixels(_rasterBuffer, _stride, 0);

            if (!isEraser)
            {
                ApplyEraser(_rasterBuffer, _vectorBuffer);
            }
            else
            {
                ApplyBrush(_rasterBuffer, _vectorBuffer);
            }

            // Записываем обратно
            Bitmap.WritePixels(new Int32Rect(0, 0, Width, Height),
                _rasterBuffer, _stride, 0);

        }

        private void ApplyBrush(byte[] rasterPixels, byte[] vectorPixels)
        {
            // Кисть: обычный альфа-блендинг
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                byte vectorAlpha = vectorPixels[i + 3];

                if (vectorAlpha > 0)
                {
                    if (vectorAlpha == 255)
                    {
                        // Полная замена (самый быстрый случай)
                        rasterPixels[i] = vectorPixels[i];     // B
                        rasterPixels[i + 1] = vectorPixels[i + 1]; // G
                        rasterPixels[i + 2] = vectorPixels[i + 2]; // R
                        rasterPixels[i + 3] = 255; // A
                    }
                    else
                    {
                        // Альфа-блендинг
                        double alpha = vectorAlpha / 255.0;
                        double invAlpha = 1.0 - alpha;

                        rasterPixels[i] = (byte)(vectorPixels[i] * alpha +
                                               rasterPixels[i] * invAlpha);
                        rasterPixels[i + 1] = (byte)(vectorPixels[i + 1] * alpha +
                                                   rasterPixels[i + 1] * invAlpha);
                        rasterPixels[i + 2] = (byte)(vectorPixels[i + 2] * alpha +
                                                   rasterPixels[i + 2] * invAlpha);
                        rasterPixels[i + 3] = 255;
                    }
                }
            }
        }

        private void ApplyEraser(byte[] rasterPixels, byte[] vectorPixels)
        {
            // Ластик: где векторный слой не прозрачен - стираем
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                // Проверяем, есть ли в векторном слое что-то для стирания
                if (vectorPixels[i + 3] > 10) // Порог 10 для игнорирования шума
                {
                    // Делаем пиксель полностью прозрачным
                    rasterPixels[i + 3] = 0;
                }
            }
        }


        // === IDisposable для освобождения ресурсов ===
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы
                    Bitmap = null;
                }
                _disposed = true;
            }
        }

        ~ImageDocument()
        {
            Dispose(false);
        }
    }
}