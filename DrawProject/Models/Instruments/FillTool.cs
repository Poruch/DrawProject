using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Input;


namespace DrawProject.Models.Instruments
{
    internal class FillTool : Tool
    {
        private byte[] _pixelBuffer;
        private int _stride;
        private int _width;
        private int _height;

        public FillTool()
        {
            Name = "Заливка";
            ToolTip = "Быстрая заливка";
            CursorPath = "pack://application:,,,/Resources/Cursors/format-color-fill.cur";
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context?.Canvas is not Controls.HybridCanvas canvas ||
                context.Brush == null)
                return;

            var pos = Mouse.GetPosition(canvas);
            var source = canvas.ImageDocument?.ActiveSource as BitmapSource;

            if (source != null)
            {
                // Загружаем все пиксели в буфер один раз
                LoadPixelBuffer(source);

                // Получаем начальный цвет
                int startX = (int)pos.X;
                int startY = (int)pos.Y;
                Color targetColor = GetPixelFromBuffer(startX, startY);
                Color fillColor = context.Brush.Color;

                // Выполняем заливку в буфере
                FloodFillBuffer(startX, startY, targetColor, fillColor);

                // Создаем новое изображение из буфера
                var newBitmap = CreateBitmapFromBuffer(source);

                // Обновляем
                canvas.ImageDocument.SelectedLayer.UpdatePixels(newBitmap);
                canvas.ImageDocument.WasChanged = true;
                canvas.CommitDrawing();
            }
        }

        private void LoadPixelBuffer(BitmapSource source)
        {
            _width = source.PixelWidth;
            _height = source.PixelHeight;
            _stride = (_width * source.Format.BitsPerPixel + 7) / 8;
            _pixelBuffer = new byte[_stride * _height];

            source.CopyPixels(_pixelBuffer, _stride, 0);
        }

        private Color GetPixelFromBuffer(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return Colors.Transparent;

            int index = y * _stride + x * 4;

            // Для формата Bgra32
            byte b = _pixelBuffer[index];
            byte g = _pixelBuffer[index + 1];
            byte r = _pixelBuffer[index + 2];
            byte a = _pixelBuffer[index + 3];

            return Color.FromArgb(a, r, g, b);
        }

        private void SetPixelInBuffer(int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return;

            int index = y * _stride + x * 4;

            _pixelBuffer[index] = color.B;     // B
            _pixelBuffer[index + 1] = color.G; // G
            _pixelBuffer[index + 2] = color.R; // R
            _pixelBuffer[index + 3] = color.A; // A
        }

        private void FloodFillBuffer(int startX, int startY, Color targetColor, Color fillColor)
        {
            if (ColorsEqual(targetColor, fillColor))
                return;

            var queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));

            bool[,] visited = new bool[_width, _height];

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                int x = (int)point.X;
                int y = (int)point.Y;

                if (x < 0 || y < 0 || x >= _width || y >= _height || visited[x, y])
                    continue;

                Color currentColor = GetPixelFromBuffer(x, y);

                if (!ColorsEqual(currentColor, targetColor, 10))
                    continue;

                SetPixelInBuffer(x, y, fillColor);
                visited[x, y] = true;

                // Добавляем 4-связных соседей
                queue.Enqueue(new Point(x + 1, y));
                queue.Enqueue(new Point(x - 1, y));
                queue.Enqueue(new Point(x, y + 1));
                queue.Enqueue(new Point(x, y - 1));
            }
        }

        private BitmapSource CreateBitmapFromBuffer(BitmapSource original)
        {
            return BitmapSource.Create(
                _width, _height,
                original.DpiX, original.DpiY,
                PixelFormats.Bgra32, // или original.Format
                null, _pixelBuffer, _stride);
        }

        private bool ColorsEqual(Color c1, Color c2, int tolerance = 10)
        {
            return Math.Abs(c1.A - c2.A) <= tolerance &&
                   Math.Abs(c1.R - c2.R) <= tolerance &&
                   Math.Abs(c1.G - c2.G) <= tolerance &&
                   Math.Abs(c1.B - c2.B) <= tolerance;
        }

        public override void OnMouseMove(InstrumentContext context)
        {

        }

        public override void OnMouseUp(InstrumentContext context)
        {

        }

        public override void OnMouseLeave(InstrumentContext context)
        {

        }
    }
}
