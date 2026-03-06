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
using System.Diagnostics;
using System.Threading;

namespace DrawProject.Models.Filers
{
    internal class TestFilter : Filter
    {
        public TestFilter()
        {
            Name = "Тест";
            FilterTip = "тест фильтр для отмены операции";
            SupportsCancellation = true;
        }

        /// <summary>
        /// Применяет эффект чёрной рамки к изображению.
        /// Изменяет исходный массив пикселей (формат BGRA).
        /// </summary>
        /// <param name="_pixelBuffer">Массив байтов изображения.</param>
        /// <param name="_stride">Ширина строки в байтах (обычно width * 4).</param>
        /// <param name="_width">Ширина изображения в пикселях.</param>
        /// <param name="_height">Высота изображения в пикселях.</param>
        /// <returns>Тот же массив с наложенной рамкой.</returns>

        public override byte[] Undo(byte[] _pixelBuffer, int _stride, int _width, int _height)
        {
            throw new NotSupportedException("BlackBorder не поддерживает отмену напрямую. Используйте команды с сохранением состояния.");
        }

        protected override byte[] ApplyWithoutCancellation(byte[] pixelBuffer, int stride, int width, int height)
        {
            throw new NotImplementedException();
        }

        protected override void ProcessPixel(byte[] pixelBuffer, int index, int x, int y, int width, int height)
        {
            int totalIterations = 10000;
            for (int i = 0; i < totalIterations; i++)
            {
                double ratio = (double)i / totalIterations;
            }
        }
    }
}
