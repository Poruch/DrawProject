using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DrawProject.Services
{
    public static class DebugService
    {

        public static bool IsPixelArrayEmpty(int[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return true;

            // Проверяем только альфа-канал (старший байт)
            for (int i = 0; i < pixels.Length; i++)
            {
                // Альфа = (pixels[i] >> 24) & 0xFF
                if ((pixels[i] & 0xFF000000) != 0)
                    return false;
            }
            return true;
        }

        public static bool IsPixelByteArrayEmpty(byte[] pixels)
        {
            if (pixels == null || pixels.Length == 0)
                return true;

            // Альфа-канал = каждый 4-й байт (индексы 3, 7, 11, ...)
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] != 0)
                    return false;
            }
            return true;
        }
    }
}
