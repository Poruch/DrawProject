using DrawProject.Models;
using DrawProject.Models.Instruments;
using DrawProject.Services;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawProject.Controls
{
    public partial class HybridCanvas : UserControl
    {
        // === DEPENDENCY PROPERTIES ===
        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register("Brush", typeof(Brush), typeof(HybridCanvas),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ToolProperty =
            DependencyProperty.Register("Tool", typeof(Tool), typeof(HybridCanvas),
                new PropertyMetadata(null));

        public static readonly DependencyProperty ImageDocumentProperty =
            DependencyProperty.Register("ImageDocument", typeof(ImageDocument),
                typeof(HybridCanvas), new PropertyMetadata(null, OnImageDocumentChanged));

        // === СВОЙСТВА ===
        public Brush Brush
        {
            get => (Brush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        public Tool Tool
        {
            get => (Tool)GetValue(ToolProperty);
            set => SetValue(ToolProperty, value);
        }

        private bool _blend = true;
        public bool Blend { get => _blend; set => _blend = value; }

        public ImageDocument ImageDocument
        {
            get => (ImageDocument)GetValue(ImageDocumentProperty);
            set => SetValue(ImageDocumentProperty, value);
        }

        // === ПОЛЯ ===
        private Image _rasterImage;
        private Canvas _vectorOverlay;
        private bool _isDrawing = false;
        private Point _lastPoint;

        // === КОНСТРУКТОР ===
        public HybridCanvas()
        {
            InitializeComponent();
            InitializeLayers();
            SetupMouseEvents();
        }

        private void InitializeLayers()
        {
            _rasterImage = new Image();
            _vectorOverlay = new Canvas { Background = Brushes.Transparent };

            rootCanvas.Children.Add(_rasterImage);
            rootCanvas.Children.Add(_vectorOverlay);
        }
        SimpleMouseService simpleMouseService;

        private void SetupMouseEvents()
        {
            simpleMouseService = new SimpleMouseService(this);
            simpleMouseService.MouseDown += OnMouseDown;
            simpleMouseService.MouseUp += OnMouseUp;
            simpleMouseService.MouseMoved += OnMouseMove;
            simpleMouseService.Start();
        }

        // === СОБЫТИЯ МЫШИ ===
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Brush == null || ImageDocument?.ActiveLayer == null || Tool == null)
                return;

            _isDrawing = true;
            _lastPoint = e.GetPosition(this);

            // Захватываем мышь
            //CaptureMouse();

            // Начинаем рисование
            var context = new InstrumentContext(this, e);
            Tool.OnMouseDown(context);

            Debug.WriteLine("[HybridCanvas] Drawing started");
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;

            // Завершаем рисование
            var context = new InstrumentContext(this, e);
            Tool.OnMouseUp(context);


            // Коммитим рисунок
            CommitDrawing();

            Debug.WriteLine("[HybridCanvas] Drawing ended");
        }

        private void OnMouseMove(Point currentPoint)
        {
            if (!_isDrawing || Tool == null) return;

            // Простая интерполяция между точками
            if (_lastPoint != default)
            {
                InterpolateAndDraw(_lastPoint, currentPoint);
            }
            else
            {
                // Рисуем текущую точку
                DrawPoint(currentPoint);
            }

            _lastPoint = currentPoint;
        }

        private void InterpolateAndDraw(Point from, Point to)
        {
            double distance = Math.Sqrt(
                Math.Pow(to.X - from.X, 2) +
                Math.Pow(to.Y - from.Y, 2));

            // Определяем нужно ли интерполировать
            if (distance > (Brush?.Size ?? 5))
            {
                // Рассчитываем количество промежуточных точек
                int steps = (int)(distance / ((Brush?.Size ?? 10) * 0.5));
                steps = Math.Clamp(steps, 1, 5); // Не больше 5 точек

                for (int i = 1; i <= steps; i++)
                {
                    double t = i / (double)(steps + 1);
                    Point interpolated = new Point(
                        from.X + (to.X - from.X) * t,
                        from.Y + (to.Y - from.Y) * t);

                    // Рисуем промежуточную точку
                    DrawPoint(interpolated);
                }
            }

            // Рисуем конечную точку
            DrawPoint(to);
        }

        private void DrawPoint(Point point)
        {
            if (Tool == null) return;

            try
            {
                // Создаем MouseEventArgs если их нет
                var args = new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount)
                {
                    RoutedEvent = Mouse.MouseMoveEvent,
                    Source = this
                };

                // Создаем контекст и вызываем инструмент
                var context = new InstrumentContext(this, args);
                Tool.OnMouseMove(context);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[HybridCanvas] Draw error: {ex.Message}");
            }
        }

        // === КОММИТ И ОЧИСТКА ===
        public void CommitDrawing()
        {
            if (_vectorOverlay.Children.Count == 0 ||
                Brush == null ||
                ImageDocument?.ActiveLayer == null)
                return;

            try
            {
                var renderTarget = new RenderTargetBitmap(
                    (int)_vectorOverlay.ActualWidth,
                    (int)_vectorOverlay.ActualHeight,
                    96, 96, PixelFormats.Pbgra32);

                renderTarget.Render(_vectorOverlay);
                ImageDocument.ApplyVectorLayer(renderTarget, Blend);
                _rasterImage.Source = ImageDocument.GetCompositeImage();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CommitDrawing error: {ex.Message}");
            }
            finally
            {
                _vectorOverlay.Children.Clear();
                Blend = true;
            }
        }

        // === ОСТАЛЬНЫЕ МЕТОДЫ ===
        private static void OnImageDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as HybridCanvas;

            if (e.NewValue is ImageDocument newDoc)
            {
                canvas._rasterImage.Source = newDoc.GetCompositeImage();
                canvas._rasterImage.Width = newDoc.Width;
                canvas._rasterImage.Height = newDoc.Height;

                canvas._vectorOverlay.Width = newDoc.Width;
                canvas._vectorOverlay.Height = newDoc.Height;

                canvas.Width = newDoc.Width;
                canvas.Height = newDoc.Height;

                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.Width = newDoc.Width;
                    canvas.rootCanvas.Height = newDoc.Height;
                }

                canvas._vectorOverlay.Children.Clear();
            }
            else if (e.NewValue == null)
            {
                canvas._rasterImage.Source = null;
                canvas._vectorOverlay.Children.Clear();
            }
        }

        public void RefreshRasterImage()
        {
            if (ImageDocument != null)
            {
                _rasterImage.Source = ImageDocument.GetCompositeImage();
            }
        }

        public Canvas GetVectorOverlay() => _vectorOverlay;
        public Image GetRasterImage() => _rasterImage;

        public void ClearCanvas()
        {
            _vectorOverlay.Children.Clear();
            ImageDocument?.Clear();
            _rasterImage.Source = ImageDocument?.GetCompositeImage();
        }
    }
}