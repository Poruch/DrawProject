using DrawProject.Models.Instruments;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using System;
using DrawProject.Controls;
using System.Windows.Controls;
using DrawProject.Services.Plugins;

class Eraser : Tool
{
    public Eraser()
    {
        Name = "Eraser";
        ToolTip = "Set pixels max alfa channel";
        CursorPath = "pack://application:,,,/Resources/Cursors/eraser.png";
        CommitOnMouseUp = true;
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

    public override void ApplyTool(InstrumentContext context)
    {
        if (context.LastPosition == default)
        {
            // Первая точка штриха
            AddPreview(context.Position, context);
        }
        else
        {
            // Интерполяция между LastPosition и Position
            int steps = Math.Max(1, context.Steps);
            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                double x = context.LastPosition.X + (context.Position.X - context.LastPosition.X) * t;
                double y = context.LastPosition.Y + (context.Position.Y - context.LastPosition.Y) * t;
                AddPreview(new Point(x, y), context);
            }
        }
    }
    private void AddPreview(Point pos, InstrumentContext context)
    {
        var preview = Brush.Shape.GetPreviewElement(
            pos,
            (int)(context.Brush.Size * context.Pressure),
            Color.FromArgb(255, 255, 255, 255), // для ластика цвет не важен
            Brush.Opacity);
        VectorOverlay.Children.Add(preview);
        _currentStroke.Add(preview);
    }
    public override void OnMouseUp(InstrumentContext context)
    {

    }


}