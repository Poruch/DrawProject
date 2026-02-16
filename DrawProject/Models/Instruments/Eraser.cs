using DrawProject.Models.Instruments;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using System;
using DrawProject.Controls;
using System.Windows.Controls;

class Eraser : Tool
{
    public Eraser()
    {
        Name = "Eraser";
        ToolTip = "Set pixels max alfa channel";
        CursorPath = "pack://application:,,,/Resources/Cursors/eraser.png";
    }
    Brush Brush { get; set; }
    public Canvas VectorOverlay { get; set; }

    private List<UIElement> _currentStroke = new();
    public override void OnMouseDown(InstrumentContext context)
    {
        Brush = context.Brush;
        VectorOverlay = context.VectorOverlay;
        context.Canvas.UseBlend = false;
    }



    public override void OnMouseLeave(InstrumentContext context)
    {

    }

    public override void OnMouseMove(InstrumentContext context)
    {
        // Создаем превью элемента кисти
        var preview = Brush.Shape.GetPreviewElement(context.Position,
            (int)(context.Brush.Size * context.Pressure), Color.FromArgb(255, 255, 255, 255), Brush.Opacity);
        VectorOverlay.Children.Add(preview);
        _currentStroke.Add(preview);
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
                Brush.Size, Color.FromArgb(255, 255, 255, 255), Brush.Opacity);

            VectorOverlay.Children.Add(preview);
            _currentStroke.Add(preview);
        }
    }
}