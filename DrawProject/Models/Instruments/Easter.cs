using DrawProject.Models.Instruments;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using System;
using DrawProject.Controls;

class Easter : Tool
{
    public string Name => "Ластик";

    private List<UIElement> _currentStroke = new();

    public override void OnMouseDown(InstrumentContext context)
    {
        _currentStroke.Clear();
    }

    public override void OnMouseMove(InstrumentContext context)
    {
        if (context.Brush == null || context.Document == null) return;

        // Получаем WriteableBitmap ИЗ документа, не создаем новый!
        var activeLayer = context.Document.ActiveLayer; // Это WriteableBitmap
        if (activeLayer == null) return;

        // Создаем превью
        var previewColor = Color.FromArgb(128, 200, 200, 200);
        var preview = context.Brush.Shape.GetPreviewElement(
            context.Position,
            (int)(context.Brush.Size * context.Pressure),
            previewColor,
            0.3f);

        context.VectorOverlay.Children.Add(preview);
        _currentStroke.Add(preview);

        // Сразу делаем область прозрачной
        MakeAreaTransparent(context, activeLayer);

        // Интерполяция
        if (context.LastPosition != default)
        {
            InterpolateTransparentPoints(context, activeLayer);
        }
    }

    private void MakeAreaTransparent(InstrumentContext context, WriteableBitmap bitmap)
    {
        if (bitmap == null) return;

        int radius = (int)(context.Brush.Size * context.Pressure) / 2;
        int centerX = (int)context.Position.X;
        int centerY = (int)context.Position.Y;

        bitmap.Lock();

        try
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x >= 0 && x < bitmap.PixelWidth &&
                        y >= 0 && y < bitmap.PixelHeight)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(x - centerX, 2) +
                            Math.Pow(y - centerY, 2));

                        if (distance <= radius)
                        {
                            // Делаем пиксель прозрачным
                            byte[] transparentPixel = { 0, 0, 0, 0 };

                            bitmap.WritePixels(
                                new Int32Rect(x, y, 1, 1),
                                transparentPixel, 4, 0);
                        }
                    }
                }
            }
        }
        finally
        {
            bitmap.Unlock();
        }

        // ВАЖНО: Принудительно обновляем отображение
        ForceCanvasUpdate(context);
    }

    private void InterpolateTransparentPoints(InstrumentContext context, WriteableBitmap bitmap)
    {
        if (context.Brush == null || context.LastPosition == default || bitmap == null)
            return;

        float distance = (float)Math.Sqrt(
            Math.Pow(context.Position.X - context.LastPosition.X, 2) +
            Math.Pow(context.Position.Y - context.LastPosition.Y, 2));

        float spacing = context.Brush.Spacing * context.Brush.Size;
        if (distance <= spacing) return;

        int steps = (int)(distance / spacing);

        for (int i = 1; i < steps; i++)
        {
            float t = i / (float)steps;
            Point interpolated = new Point(
                context.LastPosition.X + (context.Position.X - context.LastPosition.X) * t,
                context.LastPosition.Y + (context.Position.Y - context.LastPosition.Y) * t);

            EraseSinglePoint(bitmap, interpolated, context);
        }
    }

    private void EraseSinglePoint(WriteableBitmap bitmap, Point point, InstrumentContext context)
    {
        int radius = (int)(context.Brush.Size * context.Pressure) / 2;
        int centerX = (int)point.X;
        int centerY = (int)point.Y;

        bitmap.Lock();

        try
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    if (x >= 0 && x < bitmap.PixelWidth &&
                        y >= 0 && y < bitmap.PixelHeight)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(x - centerX, 2) +
                            Math.Pow(y - centerY, 2));

                        if (distance <= radius)
                        {
                            byte[] transparentPixel = { 0, 0, 0, 0 };
                            bitmap.WritePixels(
                                new Int32Rect(x, y, 1, 1),
                                transparentPixel, 4, 0);
                        }
                    }
                }
            }
        }
        finally
        {
            bitmap.Unlock();
        }

        // Обновляем после каждого стирания
        ForceCanvasUpdate(context);
    }

    private void ForceCanvasUpdate(InstrumentContext context)
    {
        if (context.Canvas == null || context.Document == null) return;

        // 1. Прямое обновление Image
        if (context.Canvas is HybridCanvas canvas)
        {
            // Принудительно обновляем источник изображения
            var currentSource = canvas.GetRasterImage()?.Source;

            if (currentSource != null)
            {
                // Создаем новый BitmapSource из того же WriteableBitmap
                var updatedBitmap = context.Document.GetCompositeImage();

                // Важно: создаем новый экземпляр, чтобы WPF заметил изменение
                var newBitmap = new WriteableBitmap(updatedBitmap);
                canvas.GetRasterImage().Source = newBitmap;
            }
        }

        // 2. Вызов метода обновления в HybridCanvas
        context.Canvas.RefreshRasterImage();

        // 3. Принудительное обновление UI
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (context.Canvas is HybridCanvas canvas)
            {
                canvas.InvalidateVisual();
                canvas.UpdateLayout();
            }
        }, System.Windows.Threading.DispatcherPriority.Render);
    }

    public override void OnMouseUp(InstrumentContext context)
    {
        _currentStroke.Clear();

        if (context.VectorOverlay != null)
        {
            context.VectorOverlay.Children.Clear();
        }

        // Финальное обновление
        ForceCanvasUpdate(context);
    }

    public override void OnMouseLeave(InstrumentContext context)
    {
        OnMouseUp(context);
    }
}