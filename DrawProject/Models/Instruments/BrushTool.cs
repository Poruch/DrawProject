using DrawProject.Controls;
using DrawProject.Services.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DrawProject.Models.Instruments
{
    class BrushTool : Tool
    {

        public BrushTool()
        {
            Name = "Brush";
            ToolTip = "Drawing point on cursor";
            CursorPath = "pack://application:,,,/Resources/Cursors/pen.png";
        }
        Brush Brush { get; set; }
        public Canvas VectorOverlay { get; set; }

        public override void OnMouseDown(InstrumentContext context)
        {
            Brush = context.Brush;
            VectorOverlay = context.VectorOverlay;
        }

        public override void OnMouseLeave(InstrumentContext context)
        {

        }

        public override void ApplyTool(InstrumentContext context)
        {
            int steps = Math.Max(1, context.Steps);

            for (int i = 0; i <= steps; i++)
            {
                double t = (double)i / steps;
                double x = context.LastPosition.X + (context.Position.X - context.LastPosition.X) * t;
                double y = context.LastPosition.Y + (context.Position.Y - context.LastPosition.Y) * t;
                Point interpolatedPos = new Point(x, y);

                // (Опционально) интерполяция давления, если оно менялось
                // Для этого нужно сохранять предыдущее давление в контексте,
                // например, добавив LastPressure.
                // float pressure = context.LastPressure + (context.Pressure - context.LastPressure) * t;
                // int size = (int)(context.Brush.Size * pressure);

                int currentSize = (int)(context.Brush.Size * context.Pressure); // или интерполированное давление

                var preview = Brush.Shape.GetPreviewElement(
                    interpolatedPos,
                    currentSize,
                    Color.FromArgb(255, Brush.Color.R, Brush.Color.G, Brush.Color.B),
                    Brush.Opacity
                );

                RenderOptions.SetEdgeMode(preview, EdgeMode.Aliased);
                VectorOverlay.Children.Add(preview);
            }
        }

        public override void OnMouseUp(InstrumentContext context)
        {

        }
    }
}
