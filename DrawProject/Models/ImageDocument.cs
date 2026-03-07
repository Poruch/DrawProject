using DrawProject.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using DrawProject.Services.Plugins;
using System.Diagnostics;
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
        public void ApplyVectorLayer(BitmapSource vectorLayer, byte alpha = 255, bool isEraser = false, Rect? selectionRect = null)
        {
            if (vectorLayer == null || SelectedLayerIndex == -1) return;
            vectorLayer.CopyPixels(_vectorBuffer, _stride, 0);

            _layers[SelectedLayerIndex].Source.CopyPixels(_rasterBuffer, _stride, 0);

            int minX = 0, minY = 0, maxX = Width, maxY = Height;
            if (selectionRect.HasValue)
            {
                var r = selectionRect.Value;
                minX = (int)Math.Max(0, r.X);
                minY = (int)Math.Max(0, r.Y);
                maxX = (int)Math.Min(Width, r.X + r.Width);
                maxY = (int)Math.Min(Height, r.Y + r.Height);
            }

            for (int y = minY; y < maxY; y++)
            {
                int rowStart = y * _stride;
                for (int x = minX; x < maxX; x++)
                {
                    int i = rowStart + x * 4;
                    if (isEraser)
                        ApplyEraserAt(i, alpha);   // реализуйте этот метод, используя _rasterBuffer и _vectorBuffer
                    else
                        ApplyBrushAt(i, alpha);    // аналогично
                }
            }

            // Записываем обратно
            _layers[SelectedLayerIndex].UpdatePixels(new Int32Rect(0, 0, Width, Height),
                _rasterBuffer, _stride);
            isUnSaved = true;
        }

        private void ApplyBrushAt(int i, byte alpha = 255)
        {
            byte vectorAlpha = _vectorBuffer[i + 3] == 0 ? _vectorBuffer[i + 3] : alpha;
            if (vectorAlpha == 0) return;

            float srcAlpha = vectorAlpha / 255f;

            float dstR = _rasterBuffer[i] / 255f;
            float dstG = _rasterBuffer[i + 1] / 255f;
            float dstB = _rasterBuffer[i + 2] / 255f;
            float dstA = _rasterBuffer[i + 3] / 255f;

            float srcR = _vectorBuffer[i] / 255f;
            float srcG = _vectorBuffer[i + 1] / 255f;
            float srcB = _vectorBuffer[i + 2] / 255f;

            float outR = srcR * srcAlpha + dstR * (1f - srcAlpha);
            float outG = srcG * srcAlpha + dstG * (1f - srcAlpha);
            float outB = srcB * srcAlpha + dstB * (1f - srcAlpha);

            float outA = dstA + srcAlpha * (1f - dstA);

            _rasterBuffer[i] = (byte)(outR * 255);
            _rasterBuffer[i + 1] = (byte)(outG * 255);
            _rasterBuffer[i + 2] = (byte)(outB * 255);
            _rasterBuffer[i + 3] = (byte)(outA * 255);
        }

        private void ApplyEraserAt(int i, byte alpha = 255)
        {
            byte eraserMask = _vectorBuffer[i + 3];
            if (eraserMask == 0) return;

            float strength = alpha / 255f;

            float currentAlpha = _rasterBuffer[i + 3] / 255f;
            float newAlpha = currentAlpha * (1 - strength);

            _rasterBuffer[i + 3] = (byte)(newAlpha * 255);

            if (newAlpha == 0)
            {
                _rasterBuffer[i] = 0;
                _rasterBuffer[i + 1] = 0;
                _rasterBuffer[i + 2] = 0;
            }
        }


        public async Task ApplyFilterAsync(Filter filter, IProgress<double> progress = null, CancellationToken cancellationToken = default)
        {

            if (SelectedLayer == null) return;

            //Захватываем нужные данные ДО фоновой работы
            Layer targetLayer = SelectedLayer;
            int targetWidth = Width;
            int targetHeight = Height;
            int stride = targetWidth * 4;
            byte[] originalPixels = new byte[_bufferSize];
            targetLayer.Source.CopyPixels(originalPixels, stride, 0);


            Debug.WriteLine($"Фильтр начат в {DateTime.Now:HH:mm:ss.fff}");
            byte[] resultPixels = null;
            try
            {
                resultPixels = await Task.Run(() => filter.Apply(originalPixels, stride, targetWidth, targetHeight, progress, cancellationToken), cancellationToken);
                if (resultPixels != null)
                    Debug.WriteLine($"Фильтр завершён в {DateTime.Now:HH:mm:ss.fff}");
                else
                    Debug.WriteLine($"Фильтрация отменена");
            }
            catch (Exception ex)
            {

            }

            if (resultPixels != null)
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (targetLayer == null ||
                        Width != targetWidth ||
                        Height != targetHeight)
                    {
                        return;
                    }

                    targetLayer.UpdatePixels(new Int32Rect(0, 0, targetWidth, targetHeight),
                                             resultPixels, stride);
                    WasChanged = true;
                    IsUnSaved = true;
                });
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