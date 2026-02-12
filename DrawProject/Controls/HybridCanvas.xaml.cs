using DrawProject.Models;
using DrawProject.Models.Instruments;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
            set
            {
                SetValue(ImageDocumentProperty, value);
            }
        }


        // === ПОЛЯ ===
        private bool _isDrawing = false;
        private bool isInitialize = false;
        private int BrushSize = 1;
        // === МНОГОПОТОЧНЫЕ ОЧЕРЕДИ ===
        private ConcurrentQueue<MousePoint> _inputQueue;  // Для ввода мыши
        private ConcurrentQueue<ProcessedPoint> _outputQueue; // Для рисования в UI
        private Task _processingTask;
        private CancellationTokenSource _cts;
        private DispatcherTimer _uiTimer;

        private MousePoint? lastPoint = null;


        // Структуры данных
        private struct MousePoint
        {
            public Point Position;
            public DateTime Timestamp;
        }

        private struct ProcessedPoint
        {
            public Point Position;
            public bool IsInterpolated;
        }

        // === КОНСТРУКТОР ===
        public HybridCanvas()
        {
            InitializeComponent();
            InitializeLayers();
            InitializeThreadingSystem();
        }

        private void InitializeLayers()
        {

            // Получаем элементы из XAML
            var mainGrid = this.Content as Grid;
            if (mainGrid == null) return;

            // Подписка на события мыши на самом UserControl
            this.MouseLeftButtonDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseUp;
        }

        private void InitializeThreadingSystem()
        {
            // 1. Инициализация очередей
            _inputQueue = new ConcurrentQueue<MousePoint>();
            _outputQueue = new ConcurrentQueue<ProcessedPoint>();
            _cts = new CancellationTokenSource();

            // 2. Запуск фоновой задачи обработки
            _processingTask = Task.Run(() => ProcessPointsBackground(_cts.Token));

            // 3. Таймер для UI (60 Гц)
            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _uiTimer.Tick += ProcessPointsInUI;
            _uiTimer.Start();

            // 4. Очистка при уничтожении
            this.Unloaded += (s, e) => CleanupThreading();
        }

        // === ВВОД МЫШИ (UI поток) ===
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;

            // Просто добавляем точку в очередь ввода - ОЧЕНЬ БЫСТРО!
            _inputQueue.Enqueue(new MousePoint
            {
                Position = e.GetPosition(this),
                Timestamp = DateTime.Now
            });
        }

        // === ФОНОВАЯ ОБРАБОТКА (отдельный поток!) ===
        private async Task ProcessPointsBackground(CancellationToken ct)
        {


            while (!ct.IsCancellationRequested)
            {
                try
                {

                    // Обрабатываем все точки из входной очереди
                    while (_inputQueue.TryDequeue(out var currentPoint))
                    {
                        if (lastPoint.HasValue)
                        {
                            // Интерполируем между точками в фоновом потоке!
                            InterpolateBetweenPoints(lastPoint.Value, currentPoint);
                        }

                        // Добавляем оригинальную точку
                        _outputQueue.Enqueue(new ProcessedPoint
                        {
                            Position = currentPoint.Position,
                            IsInterpolated = false
                        });
                        lastPoint = currentPoint;
                    }
                    // Короткая пауза чтобы не грузить CPU
                    //await Task.Delay(1, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Background processing error: {ex.Message}");
                }
            }
        }

        private void InterpolateBetweenPoints(MousePoint from, MousePoint to)
        {
            // ВСЕ ВЫЧИСЛЕНИЯ в фоновом потоке!
            double distance = Math.Sqrt(
            Math.Pow(to.Position.X - from.Position.X, 2) +
            Math.Pow(to.Position.Y - from.Position.Y, 2));

            double timeDiff = (to.Timestamp - from.Timestamp).TotalMilliseconds;
            double speed = distance / Math.Max(1, timeDiff);

            // Определяем нужно ли интерполировать
            //if (distance > 2.0 && speed > 2.0)
            {
                int steps = CalculateSteps(distance, speed, BrushSize);

                for (int i = 1; i < steps; i++)
                {
                    double t = i / (double)steps;
                    Point interpolated = new Point(
                    from.Position.X + (to.Position.X - from.Position.X) * t,
                    from.Position.Y + (to.Position.Y - from.Position.Y) * t);

                    _outputQueue.Enqueue(new ProcessedPoint
                    {
                        Position = interpolated,
                        IsInterpolated = true
                    });
                }
            }
        }

        private int CalculateSteps(double distance, double speed, int size)
        {
            int steps = (int)(distance / Math.Max(1, size));
            steps = (int)(steps * 1.5);

            return steps;
        }





        // === ОБРАБОТКА В UI ПОТОКЕ ===
        private void ProcessPointsInUI(object sender, EventArgs e)
        {
            if (!_isDrawing || Tool == null) return;

            // Обрабатываем до 20 точек за кадр
            int pointsProcessed = 0;
            const int maxPointsPerFrame = 40;

            while (pointsProcessed < maxPointsPerFrame &&
              _outputQueue.TryDequeue(out var point))
            {
                // ВСЕ UI операции здесь!
                var context = new InstrumentContext(this, point.Position, default);
                Tool.OnMouseMove(context);

                pointsProcessed++;
            }

        }


        // === УПРАВЛЕНИЕ СОСТОЯНИЕМ ===
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Brush == null || ImageDocument?.ActiveSource == null) return;

            _isDrawing = true;

            // Очищаем очереди
            ClearQueues();

            // Первая точка - рисуем сразу в UI
            var point = e.GetPosition(this);
            var context = new InstrumentContext(this, e, default);
            Tool?.OnMouseDown(context);

            // Также добавляем в очередь для обработки
            _inputQueue.Enqueue(new MousePoint
            {
                Position = point,
                Timestamp = DateTime.Now
            });
            BrushSize = Brush.Size;
            CaptureMouse();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing) return;

            _isDrawing = false;
            ReleaseMouseCapture();

            // Обрабатываем все оставшиеся точки
            ProcessAllRemainingPoints();

            var context = new InstrumentContext(this, e, default);
            Tool?.OnMouseUp(context);

            lastPoint = null;
            if (Tool.CommitOnMouseUp)
                CommitDrawing();
        }

        private void ProcessAllRemainingPoints()
        {
            // Ждем пока фоновый поток обработает все
            int attempts = 0;
            while ((_inputQueue.Count > 0 || _outputQueue.Count > 0) && attempts < 100)
            {
                //Thread.Sleep(1);
                ProcessPointsInUI(null, EventArgs.Empty);
                attempts++;
            }
        }

        private void ClearQueues()
        {
            // Очищаем очереди
            while (_inputQueue.TryDequeue(out _)) { }
            while (_outputQueue.TryDequeue(out _)) { }
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true; // Запрещаем прокрутку
        }










        public void AddNewLayer(BitmapSource source)
        {
            ImageDocument.CreateNewImage(source);
            CommitDrawing();
        }







        // === КОММИТ И ОЧИСТКА ===
        public void CommitDrawing()
        {
            ClearQueues();
            if (_vectorOverlay.Children.Count == 0 && !ImageDocument.WasChanged)
            {
                ClearOverlay();
                return;
            }

            // Подготовка
            _vectorOverlay.UpdateLayout();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            // Рендеринг
            var bitmap = new RenderTargetBitmap(
            (int)Math.Max(1, _vectorOverlay.ActualWidth),
            (int)Math.Max(1, _vectorOverlay.ActualHeight),
            96, 96, PixelFormats.Pbgra32);

            bitmap.Render(_vectorOverlay);

            // Применение
            ImageDocument.ApplyVectorLayer(bitmap, Blend);
            _rasterImage.Source = ImageDocument.GetCompositeImage();
            ImageDocument.WasChanged = false;
            bitmap.Freeze();
            // Очистка
            ClearOverlay();
        }

        private void ClearOverlay()
        {
            Dispatcher.BeginInvoke(() =>
                          {
                              _vectorOverlay.Children.Clear();
                              Blend = true;
                          });
        }

        private void CleanupThreading()
        {
            _cts?.Cancel();
            _uiTimer?.Stop();

            try
            {
                _processingTask?.Wait(1000);
            }
            catch { }

            _cts?.Dispose();
        }

        // === ОСТАЛЬНЫЕ МЕТОДЫ ===
        private static void OnImageDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as HybridCanvas;

            if (e.NewValue is ImageDocument newDoc)
            {
                //Сбрасывает Binding в xaml
                //canvas._rasterImage.Source = newDoc.GetCompositeImage();
                //canvas._rasterImage.Width = newDoc.Width;
                //canvas._rasterImage.Height = newDoc.Height;

                //canvas._vectorOverlay.Width = newDoc.Width;
                //canvas._vectorOverlay.Height = newDoc.Height;

                //canvas.Width = newDoc.Width;
                //canvas.Height = newDoc.Height;

                newDoc.DocumenWasChanged += canvas.CommitDrawing;
                canvas._vectorOverlay.Children.Clear();
            }
            else if (e.NewValue == null)
            {
                canvas._rasterImage.Source = null;
                canvas._vectorOverlay.Children.Clear();
            }
        }
        public Canvas GetVectorOverlay() => _vectorOverlay;
        public Image GetRasterImage() => _rasterImage;

        public void ClearCanvas()
        {
            ClearQueues();
            _vectorOverlay.Children.Clear();
            ImageDocument?.ClearActiveLayer();
            _rasterImage.Source = ImageDocument?.GetCompositeImage();
        }
    }
}