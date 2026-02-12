using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace DrawProject.Services
{
    internal static class CursorLoader
    {
        public static Cursor LoadCursor(string pngResourcePath, int size = 32, Point hotSpot = default)
        {
            try
            {
                // 1. Загрузить PNG как BitmapSource
                var uri = new Uri(pngResourcePath, UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // 2. Масштабировать до нужного размера
                var scaled = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawImage(bitmap, new Rect(0, 0, size, size));
                }
                scaled.Render(dv);
                scaled.Freeze();

                // 3. Конвертировать в .cur в памяти
                var cursorData = ConvertToCursor(scaled, (int)hotSpot.X, (int)hotSpot.Y);

                // 4. Создать курсор
                return new Cursor(cursorData);
            }
            catch
            {
                return Cursors.Arrow;
            }
        }
        private static MemoryStream ConvertToCursor(BitmapSource bitmap, int hotspotX, int hotspotY)
        {
            var stream = new MemoryStream();

            // ICO/CUR header
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                // ICONDIR
                writer.Write((ushort)0); // reserved
                writer.Write((ushort)2); // 2 = cursor
                writer.Write((ushort)1); // count

                // ICONDIRENTRY
                writer.Write((byte)bitmap.PixelWidth);   // width
                writer.Write((byte)bitmap.PixelHeight);  // height
                writer.Write((byte)0);                   // color count (0 = 256+)
                writer.Write((byte)0);                   // reserved
                writer.Write((ushort)hotspotX);          // hotspot x
                writer.Write((ushort)hotspotY);          // hotspot y
                writer.Write((uint)0);                   // bytes in image (will update later)
                writer.Write((uint)22);                  // offset to image data

                long imageDataStart = stream.Position;

                // BITMAPINFOHEADER
                writer.Write((uint)40);                  // size
                writer.Write((int)bitmap.PixelWidth);    // width
                writer.Write((int)(bitmap.PixelHeight * 2)); // height * 2 (AND + XOR)
                writer.Write((ushort)1);                 // planes
                writer.Write((ushort)32);                // bit count
                writer.Write((uint)0);                   // compression
                writer.Write((uint)0);                   // image size
                writer.Write((int)0);                    // x pixels per meter
                writer.Write((int)0);                    // y pixels per meter
                writer.Write((uint)0);                   // colors used
                writer.Write((uint)0);                   // colors important

                // Pixels (BGRA)
                int stride = bitmap.PixelWidth * 4;
                byte[] pixelData = new byte[stride * bitmap.PixelHeight];
                bitmap.CopyPixels(pixelData, stride, 0);

                // 🔁 Flip vertically for ICO/CUR format
                byte[] transformedPixelData = new byte[pixelData.Length];
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        // Исходный пиксель
                        int srcIndex = y * stride + x * 4;

                        // Целевой пиксель: (зеркальный X, перевёрнутый Y)
                        int dstX = bitmap.PixelWidth - 1 - x;
                        int dstY = bitmap.PixelHeight - 1 - y;
                        int dstIndex = dstY * stride + dstX * 4;

                        // Копируем 4 байта (BGRA)
                        transformedPixelData[dstIndex + 0] = pixelData[srcIndex + 0]; // B
                        transformedPixelData[dstIndex + 1] = pixelData[srcIndex + 1]; // G
                        transformedPixelData[dstIndex + 2] = pixelData[srcIndex + 2]; // R
                        transformedPixelData[dstIndex + 3] = pixelData[srcIndex + 3]; // A
                    }
                }

                // Write XOR mask (image)
                writer.Write(transformedPixelData);

                // Write AND mask (transparent)
                int andMaskSize = ((bitmap.PixelWidth + 31) / 32) * 4 * bitmap.PixelHeight; // correct size for 1bpp
                byte[] andMask = new byte[andMaskSize];
                Array.Fill(andMask, (byte)0xFF); // fully transparent
                writer.Write(andMask);

                // Update image size in ICONDIRENTRY
                long imageDataEnd = stream.Position;
                long imageSize = imageDataEnd - imageDataStart;
                stream.Position = 14; // position of "bytes in image"
                writer.Write((uint)imageSize);

                stream.Position = 0;
                return stream;
            }
        }
    }
}
