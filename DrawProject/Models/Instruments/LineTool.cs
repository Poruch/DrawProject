using DrawProject.Models.Instruments;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace DrawProject.Instruments
{
    public class LineTool : Tool
    {
        private Point _startPoint;
        private bool _isDrawing = false;
        private Line _previewLine; // Для линии используем класс Line

        public LineTool()
        {
            Name = "Линия";
            ToolTip = "Рисует линию";
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context.Canvas == null || context.Canvas.Brush == null) return;

            _isDrawing = true;
            _startPoint = context.Position;

            // Создаем предпросмотр линии
            CreatePreview(context);

            Debug.WriteLine($"[Line] Started at {_startPoint}");
        }

        public override void OnMouseMove(InstrumentContext context)
        {
            if (!_isDrawing || context.Canvas == null || context.Canvas.Brush == null) return;

            UpdatePreview(context);
        }

        public override void OnMouseUp(InstrumentContext context)
        {
            if (!_isDrawing || context.Canvas == null || context.Canvas.Brush == null) return;

            _isDrawing = false;

            // Создаем финальную линию
            CreateFinalLine(context);

            // Удаляем предпросмотр
            RemovePreview(context);

            Debug.WriteLine($"[Line] Completed at {context.Position}");
        }

        public override void OnMouseLeave(InstrumentContext context)
        {
            if (_isDrawing)
            {
                // Если вышли за пределы во время рисования - завершаем
                OnMouseUp(context);
            }
        }

        private void CreatePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;

            _previewLine = new Line
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = context.Position.X, // Начальная точка = конечная при создании
                Y2 = context.Position.Y,
                Stroke = new SolidColorBrush(brush.Color),
                StrokeThickness = brush.Size,
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }), // Пунктир для предпросмотра
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            canvas.Children.Add(_previewLine);
        }

        private void UpdatePreview(InstrumentContext context)
        {
            if (_previewLine != null)
            {
                // Обновляем конечную точку линии
                _previewLine.X2 = context.Position.X;
                _previewLine.Y2 = context.Position.Y;
            }
        }

        private void CreateFinalLine(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;
            var currentPoint = context.Position;

            Line finalLine = new Line
            {
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = currentPoint.X,
                Y2 = currentPoint.Y,
                Stroke = new SolidColorBrush(brush.Color),
                StrokeThickness = brush.Size,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                // Можно добавить эффекты:
                // Effect = new DropShadowEffect { BlurRadius = 2, Opacity = 0.5 }
            };

            canvas.Children.Add(finalLine);
        }

        private void RemovePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();

            if (_previewLine != null && canvas.Children.Contains(_previewLine))
            {
                canvas.Children.Remove(_previewLine);
                _previewLine = null;
            }
        }
    }
}