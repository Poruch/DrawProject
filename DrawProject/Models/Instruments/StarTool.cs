using DrawProject.Models.Instruments;
using DrawProject.Attributes;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;

namespace DrawProject.Instruments
{
    public class StarTool : Tool
    {
        private Point _startPoint;
        private bool _isDrawing = false;
        private Path _previewStar;

        [Inspectable("Количество лучей")]
        public int PointsCount { get; set; } = 5;
        [Inspectable("Отношение внутреннего радиуса к внешнему")]
        public double InnerRadiusRatio { get; set; } = 0.5;

        [Inspectable("Закрашивать или нет")]
        public bool IsFill { get; set; } = false;

        public StarTool()
        {
            Name = "Звезда";
            ToolTip = "Строит звезду с настраиваемым количеством лучей";
            CursorPath = "pack://application:,,,/Resources/Cursors/star.png";
            PointsCount = 5;
            InnerRadiusRatio = 0.5;
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context.Canvas == null || context.Canvas.Brush == null) return;

            _isDrawing = true;
            _startPoint = context.Position;

            CreatePreview(context);
            Debug.WriteLine($"[Star] Drawing started at {_startPoint}");
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
            CreateFinalStar(context);
            RemovePreview(context);
            Debug.WriteLine($"[Star] Drawing completed at {context.Position}");
        }

        public override void OnMouseLeave(InstrumentContext context)
        {
            if (_isDrawing)
            {
                OnMouseUp(context);
            }
        }

        private void CreatePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;

            _previewStar = new Path
            {
                StrokeDashArray = new DoubleCollection(new double[] { 4, 2 }),
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(Color.FromArgb(50, brush.Color.R, brush.Color.G, brush.Color.B)) : Brushes.Transparent,
                Data = CreateStarGeometry(_startPoint, 1.0, PointsCount, InnerRadiusRatio)
            };

            canvas.Children.Add(_previewStar);
        }

        private void UpdatePreview(InstrumentContext context)
        {
            if (_previewStar == null) return;

            var currentPoint = context.Position;
            double outerRadius = Math.Max(
                Math.Abs(currentPoint.X - _startPoint.X),
                Math.Abs(currentPoint.Y - _startPoint.Y)
            );

            if (outerRadius < 5) outerRadius = 5;

            _previewStar.Data = CreateStarGeometry(_startPoint, outerRadius, PointsCount, InnerRadiusRatio);
        }

        private void CreateFinalStar(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            var brush = context.Brush;
            var currentPoint = context.Position;

            double outerRadius = Math.Max(
                Math.Abs(currentPoint.X - _startPoint.X),
                Math.Abs(currentPoint.Y - _startPoint.Y)
            );

            if (outerRadius < 5) outerRadius = 5;

            Path star = new Path
            {
                StrokeThickness = brush.Size,
                Stroke = new SolidColorBrush(brush.Color),
                Fill = IsFill ? new SolidColorBrush(brush.Color) : Brushes.Transparent,
                Data = CreateStarGeometry(_startPoint, outerRadius, PointsCount, InnerRadiusRatio)
            };

            canvas.Children.Add(star);
        }

        private void RemovePreview(InstrumentContext context)
        {
            var canvas = context.Canvas.GetVectorOverlay();
            if (_previewStar != null && canvas.Children.Contains(_previewStar))
            {
                canvas.Children.Remove(_previewStar);
                _previewStar = null;
            }
        }

        private Geometry CreateStarGeometry(Point center, double outerRadius, int points, double innerRadiusRatio)
        {
            if (points < 2) points = 2;
            if (innerRadiusRatio < 0.1) innerRadiusRatio = 0.1;
            if (innerRadiusRatio > 0.9) innerRadiusRatio = 0.9;

            double innerRadius = outerRadius * innerRadiusRatio;
            double angleStep = Math.PI / points;
            double startAngle = -Math.PI / 2;

            PathFigure figure = new PathFigure
            {
                IsClosed = true,
                StartPoint = new Point(
                    center.X + outerRadius * Math.Cos(startAngle),
                    center.Y + outerRadius * Math.Sin(startAngle)
                )
            };

            for (int i = 1; i < points * 2; i++)
            {
                double angle = startAngle + i * angleStep;
                double radius = (i % 2 == 0) ? outerRadius : innerRadius;

                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(
                        center.X + radius * Math.Cos(angle),
                        center.Y + radius * Math.Sin(angle)
                    )
                });
            }

            PathGeometry geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }
    }
}