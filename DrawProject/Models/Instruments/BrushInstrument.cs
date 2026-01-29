using DrawProject.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawProject.Models.Instruments
{
    class BrushInstrument : Tool
    {
        public BrushInstrument()
        {

        }
        public string Name => throw new NotImplementedException();
        Brush Brush { get; set; }
        public Canvas VectorOverlay { get; set; }
        public ImageSource Icon => throw new NotImplementedException();

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
            // Создаем превью элемента кисти
            var preview = Brush.Shape.GetPreviewElement(context.Position,
                (int)(context.Brush.Size * context.Pressure), Brush.Color, Brush.Opacity);
            VectorOverlay.Children.Add(preview);
            // Интерполяция для плавного рисования
            if (context.LastPosition != default)
            {
                //InterpolateBrushPoints(context.LastPosition, context.Position);
            }
        }

        public override void OnMouseUp(InstrumentContext context)
        {

        }

        private void InterpolateBrushPoints(Point from, Point to)
        {
            if (Brush == null) return;

            float distance = (float)Math.Sqrt(
                Math.Pow(to.X - from.X, 2) +
                Math.Pow(to.Y - from.Y, 2));

            float spacing = Brush.Spacing * Brush.Size;
            if (distance <= spacing) return;

            int steps = (int)(distance / spacing);

            for (int i = 1; i < steps; i++)
            {
                float t = i / (float)steps;
                Point interpolated = new Point(
                    from.X + (to.X - from.X) * t,
                    from.Y + (to.Y - from.Y) * t);

                // Добавляем промежуточные превью
                var preview = Brush.Shape.GetPreviewElement(interpolated,
                    Brush.Size, Brush.Color, Brush.Opacity);

                VectorOverlay.Children.Add(preview);
            }
        }
    }
}
