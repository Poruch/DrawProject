using DrawProject.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DrawProject.Models
{
    public class ImageDocument : ObservableObject, IDisposable
    {
        // === ДАННЫЕ ИЗОБРАЖЕНИЯ ===
        bool _wasChanged = false;
        ObservableCollection<Layer> _layers = new();
        public ObservableCollection<Layer> GetLayers => _layers;
        int _selectedLayerIndex = 0;

        private bool isUnSaved = false;
        public WriteableBitmap ActiveSource => SelectedLayerIndex >= 0 ? _layers[SelectedLayerIndex].Source : null;
        public Layer SelectedLayer => SelectedLayerIndex >= 0 ? _layers[SelectedLayerIndex] : null;
        private int _width = 0;
        public int Width
        {
            get => _width;
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _height = 0;
        public int Height
        {
            get => _height;
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool WasChanged { get => _wasChanged; set => _wasChanged = value; }
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set => _selectedLayerIndex = (int)Math.Clamp(value, -1, _layers.Count - 1);
        }
        public bool IsUnSaved { get => isUnSaved; set => isUnSaved = value; }

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
            WasChanged = true;
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
            isUnSaved = true;
            WasChanged = true;
        }

        public void ClearDocument()
        {
            SelectedLayerIndex = -1;
            _layers.Clear();
            isUnSaved = true;
            WasChanged = true;
        }

        public void CreateNewImage(BitmapSource source)
        {
            ClearDocument();
            AddNewLayer(source);
            WasChanged = true;
        }
        public void CreateNewImage(int width, int height)
        {
            ClearDocument();
            Width = width;
            Height = height;
            WasChanged = true;
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
            isUnSaved = true;
            WasChanged = true;
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
            isUnSaved = true;
            WasChanged = true;
        }

        private void ApplyBrush(byte[] rasterPixels, byte[] vectorPixels)
        {
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                byte srcB = vectorPixels[i];
                byte srcG = vectorPixels[i + 1];
                byte srcR = vectorPixels[i + 2];
                byte srcA = vectorPixels[i + 3];

                if (srcA == 0) continue; // Нечего накладывать

                byte dstB = rasterPixels[i];
                byte dstG = rasterPixels[i + 1];
                byte dstR = rasterPixels[i + 2];
                byte dstA = rasterPixels[i + 3];

                if (srcA == 255 && dstA == 255)
                {
                    // Быстрый путь для полностью непрозрачных пикселей
                    rasterPixels[i] = srcB;
                    rasterPixels[i + 1] = srcG;
                    rasterPixels[i + 2] = srcR;
                    // Альфа остаётся 255
                }
                else
                {
                    // === Мягкий блендинг "source over" ===
                    // Используем целочисленную арифметику для скорости и плавности

                    // Нормализуем альфы в диапазон 0..256
                    int srcAlpha = srcA + 1;
                    int dstAlpha = dstA + 1;
                    int invSrcAlpha = 256 - srcAlpha;

                    // Результирующая альфа: outA = srcA + dstA * (1 - srcA)
                    int outAlpha = srcAlpha + ((dstAlpha * invSrcAlpha) >> 8);
                    if (outAlpha > 256) outAlpha = 256;

                    // Смешиваем цвета с плавным переходом
                    // Формула: outC = (srcC * srcA + dstC * dstA * (1 - srcA)) / outA
                    int b = (srcB * srcAlpha + dstB * dstAlpha * invSrcAlpha / 256);
                    int g = (srcG * srcAlpha + dstG * dstAlpha * invSrcAlpha / 256);
                    int r = (srcR * srcAlpha + dstR * dstAlpha * invSrcAlpha / 256);

                    // Нормализуем по результирующей альфе (только если не полностью непрозрачно)
                    if (outAlpha < 256)
                    {
                        b = (b * 255) / outAlpha;
                        g = (g * 255) / outAlpha;
                        r = (r * 255) / outAlpha;
                    }

                    // Ограничиваем диапазон 0..255
                    rasterPixels[i] = (byte)(b > 255 ? 255 : (b < 0 ? 0 : b));
                    rasterPixels[i + 1] = (byte)(g > 255 ? 255 : (g < 0 ? 0 : g));
                    rasterPixels[i + 2] = (byte)(r > 255 ? 255 : (r < 0 ? 0 : r));
                    rasterPixels[i + 3] = (byte)(outAlpha - 1); // обратно в 0..255
                }
            }
            isUnSaved = true;
            WasChanged = true;
        }

        private void ApplyEraser(byte[] rasterPixels, byte[] vectorPixels)
        {
            // Ластик: делаем пиксели прозрачными там, где векторный слой непрозрачен
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                // Порог 10 для игнорирования шума/антиалиасинга
                if (vectorPixels[i + 3] > 10)
                {
                    // Корректное "стирание" с учётом альфы ластика
                    byte eraserAlpha = vectorPixels[i + 3];
                    byte currentAlpha = rasterPixels[i + 3];

                    // Уменьшаем альфу пропорционально силе ластика
                    int newAlpha = currentAlpha - (currentAlpha * eraserAlpha / 255);
                    rasterPixels[i + 3] = (byte)(newAlpha < 0 ? 0 : newAlpha);

                    // Опционально: при полном стирании обнуляем цвета
                    if (newAlpha == 0)
                    {
                        rasterPixels[i] = 0;
                        rasterPixels[i + 1] = 0;
                        rasterPixels[i + 2] = 0;
                    }
                }
            }
            isUnSaved = true;
            WasChanged = true;
        }


        /// <summary>
        /// Изменяет размер документа (растягивает изображение)
        /// </summary>
        public void Resize(int newWidth, int newHeight)
        {
            Width = newWidth; Height = newHeight;
            for (int i = 0; i < _layers.Count; i++)
            {
                _layers[i].Resize(newWidth, newHeight);
            }
            isUnSaved = true;
            WasChanged = true;
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