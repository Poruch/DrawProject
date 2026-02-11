using DrawProject.Models.Instruments;
using System.Diagnostics;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using DrawProject.Attributes;

namespace DrawProject.Instruments
{
    public class EllipseTool : Tool
    {
        private Point _startPoint;
        private bool _isDrawing = false;
        private Ellipse _previewEllipse;
        [Inspectable("Закрашивать или нет")]
        public bool IsFill { get; set; } = false;
        public EllipseTool()
        {
            Name = "Эллипс";
            ToolTip = "Строит эллипс";
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context.Canvas == null || context.Canvas.Brush == null) return;

            _isDrawing = true;
            _startPoint = context.Position;

            // Создаем предпросмотр
            CreatePreview(context);

            Debug.WriteLine($"[Ellipse] Started at {_startPoint}");
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

            // Создаем финальный эллипс
            CreateFinalEllipse(context);

            // Удаляем предпросмотр
            RemovePreview(context);

            Debug.WriteLine($"[Ellipse] Completed at {context.Position}");
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

            _previewEllipse = new Ellipse
            {
                Width = Math.Abs(context.Position.X - _startPoint.X),
                Height = Math.Abs(context.Position.Y - _startPoint.Y),
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(brush.Color) : Brushes.Transparent // Прозрачная заливка для предпросмотра
            };

            // Позиционирование на Canvas
            Canvas.SetLeft(_previewEllipse, _startPoint.X);
            Canvas.SetTop(_previewEllipse, _startPoint.Y);
            canvas.Children.Add(_previewEllipse);
        }

        private void UpdatePreview(InstrumentContext context)
        {
            var currentPoint = context.Position;

            // Вычисляем параметры эллипса
            double left = Math.Min(_startPoint.X, currentPoint.X);
            double top = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            if (_previewEllipse != null)
            {
                _previewEllipse.Width = width;
                _previewEllipse.Height = height;
                Canvas.SetLeft(_previewEllipse, left);
                Canvas.SetTop(_previewEllipse, top);
            }
        }

        private void CreateFinalEllipse(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;
            var currentPoint = context.Position;

            // Создаем финальный эллипс
            Ellipse ellipse = new Ellipse
            {
                Width = Math.Abs(context.Position.X - _startPoint.X),
                Height = Math.Abs(context.Position.Y - _startPoint.Y),
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(brush.Color) : Brushes.Transparent // Или можно сделать заливку
                // Fill = new SolidColorBrush(Color.FromArgb(50, brush.Color.R, brush.Color.G, brush.Color.B))
            };

            // Позиционирование на Canvas
            double left = Math.Min(_startPoint.X, currentPoint.X);
            double top = Math.Min(_startPoint.Y, currentPoint.Y);

            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);
            canvas.Children.Add(ellipse);
        }

        private void RemovePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();

            // Удаляем предпросмотр эллипса
            if (_previewEllipse != null && canvas.Children.Contains(_previewEllipse))
            {
                canvas.Children.Remove(_previewEllipse);
                _previewEllipse = null;
            }
        }
    }
}