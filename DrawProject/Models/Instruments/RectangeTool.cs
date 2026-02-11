using DrawProject.Models.Instruments;
using DrawProject.Attributes;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;


namespace DrawProject.Instruments
{
    public class RectangleTool : Tool
    {
        private Point _startPoint;
        private bool _isDrawing = false;
        private Rectangle _previewRectangle;

        [Inspectable("Закрашивать или нет")]
        public bool IsFill { get; set; } = false;
        public RectangleTool()
        {
            Name = "Прямоугольник";
            ToolTip = "Строит прямоугольник";
        }
        public override void OnMouseDown(InstrumentContext context)
        {
            if (context.Canvas == null || context.Canvas.Brush == null) return;

            _isDrawing = true;
            _startPoint = context.Position;

            // Создаем предпросмотр
            CreatePreview(context);

            Debug.WriteLine($"[Rectangle] Started at {_startPoint}");
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

            // Создаем финальный прямоугольник
            CreateFinalRectangle(context);

            // Удаляем предпросмотр
            RemovePreview(context);

            Debug.WriteLine($"[Rectangle] Completed at {context.Position}");
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

            // Вариант 1: Прямоугольник как один элемент
            _previewRectangle = new Rectangle
            {
                Width = Math.Abs(context.Position.X - _startPoint.X),
                Height = Math.Abs(context.Position.Y - _startPoint.Y),
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(brush.Color) : Brushes.Transparent,
            };

            // Позиционирование на Canvas

            Canvas.SetLeft(_previewRectangle, _startPoint.X);
            Canvas.SetTop(_previewRectangle, _startPoint.Y);
            canvas.Children.Add(_previewRectangle);

        }


        private void UpdatePreview(InstrumentContext context)
        {
            var currentPoint = context.Position;

            // Вычисляем прямоугольник
            double left = Math.Min(_startPoint.X, currentPoint.X);
            double top = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            if (_previewRectangle != null)
            {
                _previewRectangle.Width = width;
                _previewRectangle.Height = height;
                Canvas.SetLeft(_previewRectangle, left);
                Canvas.SetTop(_previewRectangle, top);
            }
        }

        private void CreateFinalRectangle(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;
            var currentPoint = context.Position;

            // Вариант 1: Прямоугольник как один элемент
            Rectangle rectangle = new Rectangle
            {
                Width = Math.Abs(context.Position.X - _startPoint.X),
                Height = Math.Abs(context.Position.Y - _startPoint.Y),
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(brush.Color) : Brushes.Transparent,
            };

            // Позиционирование на Canvas
            double left = Math.Min(_startPoint.X, currentPoint.X);
            double top = Math.Min(_startPoint.Y, currentPoint.Y);

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);
            canvas.Children.Add(rectangle);

        }


        private void RemovePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();

            // Удаляем предпросмотр прямоугольника
            if (_previewRectangle != null && canvas.Children.Contains(_previewRectangle))
            {
                canvas.Children.Remove(_previewRectangle);
                _previewRectangle = null;
            }
        }


    }
}