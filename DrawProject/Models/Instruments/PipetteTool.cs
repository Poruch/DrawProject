using DrawProject.Models.Instruments;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Xml.Linq;
using DrawProject.Controls;

public class SimplePipetteTool : Tool
{
    public SimplePipetteTool()
    {
        Name = "Eyedropper";
        ToolTip = "Pick color";
        CursorPath = "pack://application:,,,/Resources/Cursors/eyedropper.png";
    }

    public override void OnMouseDown(InstrumentContext context)
    {
        if (context?.Canvas == null || context.Brush == null) return;

        var mousePos = Mouse.GetPosition(context.Canvas);

        // Пробуем получить цвет с растрового слоя
        if (context.Canvas is HybridCanvas hybridCanvas &&
            hybridCanvas.ImageDocument?.ActiveSource is BitmapSource bitmap)
        {
            var color = GetColorFromBitmap(bitmap, mousePos);
            if (color.HasValue)
            {
                context.Brush.Color = color.Value;
                System.Media.SystemSounds.Beep.Play();
            }
        }
    }

    private Color? GetColorFromBitmap(BitmapSource bitmap, Point position)
    {
        try
        {
            if (position.X >= 0 && position.Y >= 0 &&
                position.X < bitmap.PixelWidth && position.Y < bitmap.PixelHeight)
            {
                var croppedBitmap = new CroppedBitmap(
                    bitmap,
                    new Int32Rect((int)position.X, (int)position.Y, 1, 1));

                var pixels = new byte[4];
                croppedBitmap.CopyPixels(pixels, 4, 0);

                return Color.FromArgb(pixels[3], pixels[2], pixels[1], pixels[0]);
            }
        }
        catch
        {
            // Игнорируем ошибки
        }

        return null;
    }

    public override void OnMouseMove(InstrumentContext context) { }
    public override void OnMouseUp(InstrumentContext context) { }
    public override void OnMouseLeave(InstrumentContext context) { }
}