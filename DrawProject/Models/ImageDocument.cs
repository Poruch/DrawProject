using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawProject.Models
{
    public class ImageDocument : IDisposable
    {
        // === ДАННЫЕ ИЗОБРАЖЕНИЯ ===
        bool _wasChanged = false;
        ObservableCollection<Layer> _layers = new();
        public ObservableCollection<Layer> GetLayers => _layers;
        int _selectedLayerIndex = 0;

        public WriteableBitmap ActiveSource => SelectedLayerIndex >= 0 ? _layers[SelectedLayerIndex].Source : null;
        public Layer SelectedLayer => SelectedLayerIndex >= 0 ? _layers[SelectedLayerIndex] : null;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool WasChanged { get => _wasChanged; set => _wasChanged = value; }
        public int SelectedLayerIndex { get => _selectedLayerIndex; set => _selectedLayerIndex = (int)Math.Clamp(value, -1, _layers.Count - 1); }

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

            AddNewLayer();
        }
        public void AddNewLayer()
        {
            _layers.Add(new(new WriteableBitmap(Width, Height, 96, 96,
                PixelFormats.Pbgra32, null)));
            SelectedLayerIndex = _layers.Count - 1;
        }
        public void AddNewLayer(BitmapSource source)
        {
            AddNewLayer();
            WriteableBitmap Bitmap = _layers[SelectedLayerIndex].Source;
            // Если размеры Bitmap не совпадают с источником, может понадобиться масштабирование
            if (Bitmap.PixelWidth != source.PixelWidth || Bitmap.PixelHeight != source.PixelHeight)
            {
                // Масштабируем источник под размер Bitmap
                var scaledSource = new TransformedBitmap(
                    source,
                    new ScaleTransform(
                        (double)Bitmap.PixelWidth / source.PixelWidth,
                        (double)Bitmap.PixelHeight / source.PixelHeight
                    )
                );

                // Копируем пиксели из масштабированного источника
                Bitmap.WritePixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight),
                    GetPixelsFromBitmapSource(scaledSource)
                    ,
                    Bitmap.BackBufferStride,
                    0
                );
            }
            else
            {
                // Копируем пиксели напрямую
                Bitmap.WritePixels(
                    new Int32Rect(0, 0, source.PixelWidth, source.PixelHeight),
                    GetPixelsFromBitmapSource(source),
                    Bitmap.BackBufferStride,
                    0
                );
            }
        }
        // === ОЧИСТКА ===
        public void ClearActiveLayer()
        {

            // Используем WritePixels для быстрой очистки
            var clearPixels = new byte[_bufferSize];

            _layers[SelectedLayerIndex].Source.WritePixels(new Int32Rect(0, 0, Width, Height),
                    clearPixels, _stride, 0);

        }

        public void ClearDocument()
        {
            SelectedLayerIndex = -1;
            _layers.Clear();
        }

        public void CreateNewImage(BitmapSource source)
        {
            ClearDocument();
            AddNewLayer(source);
        }
        public void CreateNewImage(int width, int height)
        {
            ClearDocument();
            Width = width;
            Height = height;
        }
        private byte[] GetPixelsFromBitmapSource(BitmapSource source)
        {
            int stride = (source.PixelWidth * source.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[source.PixelHeight * stride];
            source.CopyPixels(pixels, stride, 0);
            return pixels;
        }
        public WriteableBitmap GetCompositeImage()
        {
            if (_layers == null || _layers.Count == 0)
                return null;

            // Проверяем размер первого слоя (все слои одинакового размера)
            int width = _layers[0].Source.PixelWidth;
            int height = _layers[0].Source.PixelHeight;

            // Создаем пустой WriteableBitmap для результата
            WriteableBitmap result = new WriteableBitmap(
                width,
                height,
                96, 96,
                PixelFormats.Pbgra32,
                null
            );

            int stride = width * 4; // 4 байта на пиксель для Pbgra32
            byte[] resultPixels = new byte[height * stride];

            // Накладываем все слои (от нижнего к верхнему)
            foreach (var layer in _layers)
            {
                // Получаем пиксели слоя
                byte[] layerPixels = new byte[height * stride];
                layer.Source.CopyPixels(layerPixels, stride, 0);

                // Накладываем слой
                BlendPixels(resultPixels, layerPixels);
            }

            // Записываем результат
            result.WritePixels(
                new Int32Rect(0, 0, width, height),
                resultPixels,
                stride,
                0
            );

            return result;
        }

        // Простая функция наложения пикселей (альфа-блендинг)
        private void BlendPixels(byte[] result, byte[] layer)
        {
            for (int i = 0; i < result.Length; i += 4)
            {
                // Получаем альфа-канал слоя (от 0 до 1)
                float alpha = layer[i + 3] / 255f;

                // Накладываем каждый цветовой канал
                for (int channel = 0; channel < 3; channel++) // B, G, R
                {
                    result[i + channel] = (byte)(
                        (layer[i + channel] * alpha) +
                        (result[i + channel] * (1 - alpha))
                    );
                }

                // Альфа-канал результата (максимальное значение)
                result[i + 3] = Math.Max(result[i + 3], layer[i + 3]);
            }
        }

        // === ПЕРЕНОС ВЕКТОРА В РАСТР ===
        public void ApplyVectorLayer(BitmapSource vectorLayer, bool isEraser = false)
        {
            if (vectorLayer == null || SelectedLayerIndex == -1) return;


            // Копируем векторный слой в кэшированный буфер
            vectorLayer.CopyPixels(_vectorBuffer, _stride, 0);

            // Копируем текущий Bitmap в кэшированный буфер
            _layers[SelectedLayerIndex].Source.CopyPixels(_rasterBuffer, _stride, 0);

            if (!isEraser)
            {
                ApplyEraser(_rasterBuffer, _vectorBuffer);
            }
            else
            {
                ApplyBrush(_rasterBuffer, _vectorBuffer);
            }

            // Записываем обратно
            _layers[SelectedLayerIndex].UpdatePixels(new Int32Rect(0, 0, Width, Height),
                _rasterBuffer, _stride);

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
                    ClearDocument();
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