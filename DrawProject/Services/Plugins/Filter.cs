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
        public abstract byte[] Apply(byte[] _pixelBuffer, int _stride, int _width, int _height, IProgress<double> progress = null);
        public abstract byte[] Undo(byte[] _pixelBuffer, int _stride, int _width, int _height);
    }
}
