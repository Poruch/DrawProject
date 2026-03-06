using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Resources;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Threading;
using System.Windows.Media.Media3D;
using System.Drawing;
using System.Windows;
using System.Diagnostics;


namespace DrawProject.Services.Plugins
{
    public abstract class Filter
    {
        public string Name { get; set; }
        public string FilterTip { get; set; }
        public bool SupportsCancellation { get; set; } = false;

        public byte[] Apply(byte[] pixelBuffer, int stride, int width, int height,
                            IProgress<double> progress = null,
                            CancellationToken cancellationToken = default)
        {
            if (SupportsCancellation)
            {
                return ApplyWithCancellation(pixelBuffer, stride, width, height, progress, cancellationToken);
            }
            else
            {
                return ApplyWithoutCancellation(pixelBuffer, stride, width, height);
            }
        }

        private byte[] ApplyWithCancellation(byte[] pixelBuffer, int stride, int width, int height,
                                             IProgress<double> progress,
                                             CancellationToken cancellationToken)
        {
            int totalPixels = width * height;
            int lastReportPercent = -1;

            for (int i = 0; i < totalPixels; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return null;
                int x = i % width;
                int y = i / width;
                int index = y * stride + x * 4;

                ProcessPixel(pixelBuffer, index, x, y, width, height);

                int percent = (i * 100) / totalPixels;
                if (percent > lastReportPercent)
                {
                    progress?.Report((double)i / totalPixels);
                    lastReportPercent = percent;
                }
            }

            progress?.Report(1.0);
            return pixelBuffer;
        }


        protected abstract byte[] ApplyWithoutCancellation(byte[] pixelBuffer, int stride, int width, int height);

        protected abstract void ProcessPixel(byte[] pixelBuffer, int index, int x, int y, int width, int height);

        public abstract byte[] Undo(byte[] pixelBuffer, int stride, int width, int height);

    }
}
