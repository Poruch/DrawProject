using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrawProject.Services.Plugins;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DrawProject.Models.Filers
{
    internal class BlackBorder : Filter
    {
        /// <summary>
        /// Толщина рамки в пикселях.
        /// </summary>
        public int BorderSize { get; set; } = 1;

        public BlackBorder() { Name = "черный"; }
        /// <summary>
        /// Применяет эффект чёрной рамки к изображению.
        /// Изменяет исходный массив пикселей (формат BGRA).
        /// </summary>
        /// <param name="_pixelBuffer">Массив байтов изображения.</param>
        /// <param name="_stride">Ширина строки в байтах (обычно width * 4).</param>
        /// <param name="_width">Ширина изображения в пикселях.</param>
        /// <param name="_height">Высота изображения в пикселях.</param>
        /// <returns>Тот же массив с наложенной рамкой.</returns>
        public override byte[] Apply(byte[] _pixelBuffer, int _stride, int _width, int _height, IProgress<double> progress = null)
        {
            if (BorderSize <= 0) return _pixelBuffer;

            int border = Math.Min(BorderSize, Math.Min(_width, _height) / 2); // защита от слишком большой рамки

            // Верхняя граница
            for (int y = 0; y < border; y++)
            {
                int rowStart = y * _stride;
                for (int x = 0; x < _width; x++)
                {
                    int index = rowStart + x * 4;
                    SetBlackPixel(_pixelBuffer, index);
                }
                progress?.Report((y + 1) * 100.0 / _height);
            }

            // Нижняя граница
            for (int y = _height - border; y < _height; y++)
            {
                int rowStart = y * _stride;
                for (int x = 0; x < _width; x++)
                {
                    int index = rowStart + x * 4;
                    SetBlackPixel(_pixelBuffer, index);
                }
                progress?.Report((y + 1) * 100.0 / _height);
            }

            // Левая и правая границы (исключая уже закрашенные углы)
            for (int y = border; y < _height - border; y++)
            {
                int rowStart = y * _stride;

                // Левая граница
                for (int x = 0; x < border; x++)
                {
                    int index = rowStart + x * 4;
                    SetBlackPixel(_pixelBuffer, index);
                }

                // Правая граница
                for (int x = _width - border; x < _width; x++)
                {
                    int index = rowStart + x * 4;
                    SetBlackPixel(_pixelBuffer, index);
                }
                progress?.Report((y + 1) * 100.0 / _height);
            }

            return _pixelBuffer;
        }

        private void SetBlackPixel(byte[] buffer, int index)
        {
            // Устанавливаем чёрный цвет (BGR = 0,0,0), альфу не трогаем
            buffer[index] = 0;       // B
            buffer[index + 1] = 0;   // G
            buffer[index + 2] = 0;   // R
            // buffer[index + 3] остаётся без изменений
        }

        public override byte[] Undo(byte[] _pixelBuffer, int _stride, int _width, int _height)
        {
            throw new NotSupportedException("BlackBorder не поддерживает отмену напрямую. Используйте команды с сохранением состояния.");
        }
    }
}
