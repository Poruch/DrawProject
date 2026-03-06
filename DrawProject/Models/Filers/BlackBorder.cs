using DrawProject.Services.Plugins;
using System.Xml.Linq;

using System;
using System.Threading;
using DrawProject.Services.Plugins;

namespace DrawProject.Models.Filters
{
    internal class BlackBorder : Filter
    {
        /// <summary>
        /// Толщина рамки в пикселях.
        /// </summary>
        public int BorderSize { get; set; } = 1;

        public BlackBorder()
        {
            Name = "Черная граница";
            FilterTip = "Создает черную рамку у изображения";
            SupportsCancellation = true; // включаем поддержку отмены и прогресса
        }

        /// <summary>
        /// Обрабатывает один пиксель. Если он находится в области рамки, устанавливает чёрный цвет.
        /// </summary>
        protected override void ProcessPixel(byte[] buffer, int index, int x, int y, int width, int height)
        {
            if (BorderSize <= 0) return;

            int border = Math.Min(BorderSize, Math.Min(width, height) / 2);

            bool isBorder = (y < border) || (y >= height - border) || (x < border) || (x >= width - border);

            if (isBorder)
            {
                buffer[index] = 0;       // B
                buffer[index + 1] = 0;   // G
                buffer[index + 2] = 0;   // R
                buffer[index + 3] = 255;
            }
        }

        public override byte[] Undo(byte[] pixelBuffer, int stride, int width, int height)
        {
            throw new NotSupportedException("BlackBorder не поддерживает отмену напрямую. Используйте команды с сохранением состояния.");
        }

        protected override byte[] ApplyWithoutCancellation(byte[] pixelBuffer, int stride, int width, int height)
        {
            throw new NotImplementedException();
        }
    }
}