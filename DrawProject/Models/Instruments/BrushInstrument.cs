using DrawProject.Controls;
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

namespace DrawProject.Models.Instruments
{
    class BrushInstrument : Tool
    {

        public BrushInstrument()
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

        public override void OnMouseMove(InstrumentContext context)
        {
            var preview = Brush.Shape.GetPreviewElement(context.Position,
                (int)(context.Brush.Size * context.Pressure), Brush.Color, Brush.Opacity);
            VectorOverlay.Children.Add(preview);
        }

        public override void OnMouseUp(InstrumentContext context)
        {

        }
    }
}
