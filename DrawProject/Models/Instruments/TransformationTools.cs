using DrawProject.Models.Instruments;
using DrawProject.Services.Plugins;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DrawProject.Instruments
{
    // === ИНСТРУМЕНТ ВЫДЕЛЕНИЯ (прямоугольная область) ===
    public class SelectionTool : Tool
    {
        private Point _startPoint;
        private bool _isSelecting = false;
        private Rectangle _selectionRect; // для предпросмотра

        public SelectionTool()
        {
            Name = "Selection";
            ToolTip = "Select rectangular area";
            CursorPath = "pack://application:,,,/Resources/Cursors/select.png";
            CommitOnMouseUp = false; // не вызываем CommitDrawing после выделения
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context.Canvas == null) return;

            _isSelecting = true;
            _startPoint = context.Position;

            _selectionRect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 255))
            };
            Canvas.SetLeft(_selectionRect, _startPoint.X);
            Canvas.SetTop(_selectionRect, _startPoint.Y);
            _selectionRect.Width = 0;
            _selectionRect.Height = 0;
            context.VectorOverlay.Children.Add(_selectionRect);
        }

        public override void OnMouseMove(InstrumentContext context)
        {
            if (!_isSelecting) return;

            var current = context.Position;
            double left = Math.Min(_startPoint.X, current.X);
            double top = Math.Min(_startPoint.Y, current.Y);
            double width = Math.Abs(current.X - _startPoint.X);
            double height = Math.Abs(current.Y - _startPoint.Y);

            _selectionRect.Width = width;
            _selectionRect.Height = height;
            Canvas.SetLeft(_selectionRect, left);
            Canvas.SetTop(_selectionRect, top);
        }

        public override void OnMouseUp(InstrumentContext context)
        {
            if (!_isSelecting) return;
            _isSelecting = false;

            // Завершаем выделение
            if (_selectionRect.Width > 5 && _selectionRect.Height > 5) // минимальный размер
            {
                var rect = new Rect(
                    Canvas.GetLeft(_selectionRect),
                    Canvas.GetTop(_selectionRect),
                    _selectionRect.Width,
                    _selectionRect.Height);
                context.Canvas.SetSelectionRect(rect);
            }
            else
            {
                context.Canvas.ClearSelection();
            }

            // Удаляем временный прямоугольник
            context.VectorOverlay.Children.Remove(_selectionRect);
        }

        public override void OnMouseLeave(InstrumentContext context)
        {
            if (_isSelecting) OnMouseUp(context);
        }
    }

    // === ИНСТРУМЕНТ ПЕРЕМЕЩЕНИЯ (перетаскивание выделенной области) ===
    public class MoveTool : Tool
    {
        private bool _isMoving = false;
        private Point _lastPosition;
        private Rect _originalRect;

        public MoveTool()
        {
            Name = "Move";
            ToolTip = "Move selected area";
            CursorPath = "pack://application:,,,/Resources/Cursors/move.png";
            CommitOnMouseUp = false;
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (!context.Canvas.SelectionBounds.HasValue) return;

            var selection = context.Canvas.SelectionBounds.Value;
            if (selection.Contains(context.Position))
            {
                _isMoving = true;
                _lastPosition = context.Position;
                _originalRect = selection;
            }
        }

        public override void OnMouseMove(InstrumentContext context)
        {
            if (!_isMoving) return;

            var current = context.Position;
            double deltaX = current.X - _lastPosition.X;
            double deltaY = current.Y - _lastPosition.Y;

            var newRect = new Rect(
                _originalRect.X + deltaX,
                _originalRect.Y + deltaY,
                _originalRect.Width,
                _originalRect.Height);
            context.Canvas.SetSelectionRect(newRect);
        }

        public override void OnMouseUp(InstrumentContext context)
        {
            _isMoving = false;
        }

        public override void OnMouseLeave(InstrumentContext context)
        {
            if (_isMoving) OnMouseUp(context);
        }
    }

    // === ИНСТРУМЕНТ ТРАНСФОРМАЦИИ (масштабирование по углам) ===
    public class TransformTool : Tool
    {
        private enum TransformMode
        {
            None,
            Move,
            ScaleTopLeft,
            ScaleTopRight,
            ScaleBottomLeft,
            ScaleBottomRight
        }

        private TransformMode _mode = TransformMode.None;
        private Point _startPoint;
        private Rect _originalRect;

        public TransformTool()
        {
            Name = "Transform";
            ToolTip = "Scale selected area";
            CursorPath = "pack://application:,,,/Resources/Cursors/transform.png";
            CommitOnMouseUp = false;
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (!context.Canvas.SelectionBounds.HasValue) return;

            var selection = context.Canvas.SelectionBounds.Value;
            Point pos = context.Position;
            double handleSize = 10;

            // Проверяем попадание в углы
            if (Math.Abs(pos.X - selection.Left) < handleSize && Math.Abs(pos.Y - selection.Top) < handleSize)
                _mode = TransformMode.ScaleTopLeft;
            else if (Math.Abs(pos.X - selection.Right) < handleSize && Math.Abs(pos.Y - selection.Top) < handleSize)
                _mode = TransformMode.ScaleTopRight;
            else if (Math.Abs(pos.X - selection.Left) < handleSize && Math.Abs(pos.Y - selection.Bottom) < handleSize)
                _mode = TransformMode.ScaleBottomLeft;
            else if (Math.Abs(pos.X - selection.Right) < handleSize && Math.Abs(pos.Y - selection.Bottom) < handleSize)
                _mode = TransformMode.ScaleBottomRight;
            else if (selection.Contains(pos))
                _mode = TransformMode.Move;
            else
                _mode = TransformMode.None;

            if (_mode != TransformMode.None)
            {
                _startPoint = pos;
                _originalRect = selection;
            }
        }

        public override void OnMouseMove(InstrumentContext context)
        {
            if (_mode == TransformMode.None) return;

            Point current = context.Position;
            double deltaX = current.X - _startPoint.X;
            double deltaY = current.Y - _startPoint.Y;

            Rect newRect = _originalRect;

            if (_mode == TransformMode.Move)
            {
                newRect = new Rect(
                    _originalRect.X + deltaX,
                    _originalRect.Y + deltaY,
                    _originalRect.Width,
                    _originalRect.Height);
            }
            else
            {
                double left = _originalRect.Left;
                double top = _originalRect.Top;
                double right = _originalRect.Right;
                double bottom = _originalRect.Bottom;

                switch (_mode)
                {
                    case TransformMode.ScaleTopLeft:
                        left += deltaX;
                        top += deltaY;
                        break;
                    case TransformMode.ScaleTopRight:
                        right += deltaX;
                        top += deltaY;
                        break;
                    case TransformMode.ScaleBottomLeft:
                        left += deltaX;
                        bottom += deltaY;
                        break;
                    case TransformMode.ScaleBottomRight:
                        right += deltaX;
                        bottom += deltaY;
                        break;
                }

                // Ограничиваем минимальный размер
                if (right - left < 5) right = left + 5;
                if (bottom - top < 5) bottom = top + 5;

                newRect = new Rect(left, top, right - left, bottom - top);
            }

            context.Canvas.SetSelectionRect(newRect);
        }

        public override void OnMouseUp(InstrumentContext context)
        {
            _mode = TransformMode.None;
        }

        public override void OnMouseLeave(InstrumentContext context)
        {
            if (_mode != TransformMode.None) OnMouseUp(context);
        }
    }
}