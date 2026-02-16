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
        public event Action DocumentWasChanged;


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
        public bool WasChanged
        {
            get => _wasChanged;
            set
            {
                _wasChanged = value;
                if (_wasChanged && DocumentWasChanged != null)
                    DocumentWasChanged.Invoke();
            }

        }
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set
            {
                _selectedLayerIndex = (int)Math.Clamp(value, -1, _layers.Count - 1);
                WasChanged = true;
            }
        }
        public bool IsUnSaved { get => isUnSaved; set => isUnSaved = value; }

        private readonly int _stride;
        private readonly int _bufferSize;

        private readonly byte[] _vectorBuffer;
        private readonly byte[] _rasterBuffer;

        // === КОНСТРУКТОР ===
        public ImageDocument(int width, int height)
        {
            Width = width;
            Height = height;
            _stride = width * 4;
            _bufferSize = _stride * height;

            _vectorBuffer = new byte[_bufferSize];
            _rasterBuffer = new byte[_bufferSize];

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
            if (Bitmap.PixelWidth != source.PixelWidth || Bitmap.PixelHeight != source.PixelHeight)
            {
                var scaledSource = new TransformedBitmap(
                    source,
                    new ScaleTransform(
                        (double)Bitmap.PixelWidth / source.PixelWidth,
                        (double)Bitmap.PixelHeight / source.PixelHeight
                    )
                );

                Bitmap.WritePixels(new Int32Rect(0, 0, Bitmap.PixelWidth, Bitmap.PixelHeight),
                    GetPixelsFromBitmapSource(scaledSource)
                    ,
                    Bitmap.BackBufferStride,
                    0
                );
            }
            else
            {
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
            if (SelectedLayerIndex == -1) return;
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

            int width = _layers[0].Source.PixelWidth;
            int height = _layers[0].Source.PixelHeight;

            WriteableBitmap result = new WriteableBitmap(
                width,
                height,
                96, 96,
                PixelFormats.Pbgra32,
                null
            );

            int stride = width * 4;
            byte[] resultPixels = new byte[height * stride];

            foreach (var layer in _layers)
            {
                byte[] layerPixels = new byte[height * stride];
                layer.Source.CopyPixels(layerPixels, stride, 0);

                BlendPixels(resultPixels, layerPixels);
            }

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
                float alpha = layer[i + 3] / 255f;

                for (int channel = 0; channel < 3; channel++)
                {
                    result[i + channel] = (byte)(
                        (layer[i + channel] * alpha) +
                        (result[i + channel] * (1 - alpha))
                    );
                }

                result[i + 3] = Math.Max(result[i + 3], layer[i + 3]);
            }
            isUnSaved = true;
        }

        // === ПЕРЕНОС ВЕКТОРА В РАСТР ===
        public void ApplyVectorLayer(BitmapSource vectorLayer, byte alpha = 255, bool isEraser = false)
        {
            if (vectorLayer == null || SelectedLayerIndex == -1) return;
            vectorLayer.CopyPixels(_vectorBuffer, _stride, 0);

            _layers[SelectedLayerIndex].Source.CopyPixels(_rasterBuffer, _stride, 0);

            if (isEraser)
            {
                ApplyEraser(_rasterBuffer, _vectorBuffer, alpha);
            }
            else
            {
                ApplyBrush(_rasterBuffer, _vectorBuffer, alpha);
            }

            // Записываем обратно
            _layers[SelectedLayerIndex].UpdatePixels(new Int32Rect(0, 0, Width, Height),
                _rasterBuffer, _stride);
            isUnSaved = true;
        }

        private void ApplyBrush(byte[] rasterPixels, byte[] vectorPixels, byte alpha = 255)
        {
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                byte vectorAlpha = vectorPixels[i + 3] == 0 ? vectorPixels[i + 3] : alpha;
                if (vectorAlpha == 0) continue;

                float srcAlpha = vectorAlpha / 255f;

                float dstR = rasterPixels[i] / 255f;
                float dstG = rasterPixels[i + 1] / 255f;
                float dstB = rasterPixels[i + 2] / 255f;
                float dstA = rasterPixels[i + 3] / 255f;

                float srcR = vectorPixels[i] / 255f;
                float srcG = vectorPixels[i + 1] / 255f;
                float srcB = vectorPixels[i + 2] / 255f;

                float outR = srcR * srcAlpha + dstR * (1f - srcAlpha);
                float outG = srcG * srcAlpha + dstG * (1f - srcAlpha);
                float outB = srcB * srcAlpha + dstB * (1f - srcAlpha);

                float outA = dstA + srcAlpha * (1f - dstA);

                rasterPixels[i] = (byte)(outR * 255);
                rasterPixels[i + 1] = (byte)(outG * 255);
                rasterPixels[i + 2] = (byte)(outB * 255);
                rasterPixels[i + 3] = (byte)(outA * 255);
            }
        }

        private void ApplyEraser(byte[] rasterPixels, byte[] vectorPixels, byte alpha = 255)
        {
            for (int i = 0; i < rasterPixels.Length; i += 4)
            {
                byte eraserAlpha = vectorPixels[i + 3] == 0 ? vectorPixels[i + 3] : alpha;
                if (eraserAlpha == 0) continue;

                float strength = eraserAlpha / 255f;
                float currentAlpha = rasterPixels[i + 3] / 255f;
                float newAlpha = currentAlpha * (1 - strength);

                rasterPixels[i + 3] = (byte)(newAlpha * 255);

                // Если пиксель стал полностью прозрачным – обнулим цвет (необязательно)
                if (newAlpha == 0)
                {
                    rasterPixels[i] = 0;
                    rasterPixels[i + 1] = 0;
                    rasterPixels[i + 2] = 0;
                }
            }
            isUnSaved = true;
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