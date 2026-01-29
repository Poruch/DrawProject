using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DrawProject.Services
{
    /// <summary>
    /// Сервис для высокочастотного считывания позиции мыши
    /// </summary>
    public class MouseInputService : IDisposable
    {
        // === СОБЫТИЯ ===
        public event Action<MousePoint> MouseMoved;
        public event Action<MouseButtonEventArgs> MouseDown;
        public event Action<MouseButtonEventArgs> MouseUp;
        public event Action MouseEnter;
        public event Action MouseLeave;

        // === СТРУКТУРЫ ===
        public struct MousePoint
        {
            public Point Position;
            public DateTime Timestamp;
            public float Pressure;
            public bool IsPen;

            public override string ToString() =>
                $"[{Timestamp:HH:mm:ss.fff}] X:{Position.X:F1}, Y:{Position.Y:F1}, Pressure:{Pressure:F2}";
        }

        // === ПОЛЯ ===
        private readonly UIElement _targetElement;
        private readonly ConcurrentQueue<MousePoint> _pointQueue = new();

        // Потоки
        private Task _inputTask;
        private CancellationTokenSource _cts;
        private DispatcherTimer _dispatchTimer;

        // Состояние
        private bool _isCapturing = false;
        private MousePoint _lastRawPosition;
        private DateTime _lastInputTime;
        private const int INPUT_POLLING_RATE_HZ = 250; // 250 Гц
        private const int DISPATCH_RATE_HZ = 125; // 125 Гц

        // === СВОЙСТВА ===
        public bool IsRunning { get; private set; }
        public int QueueSize => _pointQueue.Count;
        public MousePoint LastPosition => _lastRawPosition;
        public bool IsMouseOver => _targetElement.IsMouseOver;

        // === КОНСТРУКТОР ===
        public MouseInputService(UIElement targetElement)
        {
            _targetElement = targetElement ?? throw new ArgumentNullException(nameof(targetElement));
            Initialize();
        }

        private void Initialize()
        {
            // Подписка на события мыши
            _targetElement.MouseLeftButtonDown += OnTargetMouseDown;
            _targetElement.MouseLeftButtonUp += OnTargetMouseUp;
            _targetElement.MouseEnter += OnTargetMouseEnter;
            _targetElement.MouseLeave += OnTargetMouseLeave;
            _targetElement.MouseMove += OnTargetMouseMove;

            // Таймер для диспатча событий
            _dispatchTimer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / DISPATCH_RATE_HZ)
            };
            _dispatchTimer.Tick += DispatchPoints;
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Запуск сервиса
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            _isCapturing = false;

            // Запускаем фоновый поток для опроса мыши
            _inputTask = Task.Run(PollMousePosition, _cts.Token);

            // Запускаем таймер диспатча
            _dispatchTimer.Start();

            IsRunning = true;
            Debug.WriteLine($"[MouseInputService] Started (Polling: {INPUT_POLLING_RATE_HZ}Hz, Dispatch: {DISPATCH_RATE_HZ}Hz)");
        }

        /// <summary>
        /// Остановка сервиса
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;

            _cts?.Cancel();
            _dispatchTimer.Stop();
            _pointQueue.Clear();

            try
            {
                _inputTask?.Wait(1000);
            }
            catch { }

            IsRunning = false;
            Debug.WriteLine("[MouseInputService] Stopped");
        }

        /// <summary>
        /// Начать захват точек (при нажатии кнопки мыши)
        /// </summary>
        public void StartCapture()
        {
            _isCapturing = true;
            _pointQueue.Clear();
            Debug.WriteLine("[MouseInputService] Capture started");
        }

        /// <summary>
        /// Остановить захват точек (при отпускании кнопки мыши)
        /// </summary>
        public void StopCapture()
        {
            _isCapturing = false;
            Debug.WriteLine("[MouseInputService] Capture stopped");
        }

        /// <summary>
        /// Получить все накопленные точки
        /// </summary>
        public MousePoint[] GetCapturedPoints()
        {
            return _pointQueue.ToArray();
        }

        /// <summary>
        /// Попытаться получить следующую точку
        /// </summary>
        public bool TryGetNextPoint(out MousePoint point)
        {
            return _pointQueue.TryDequeue(out point);
        }

        // === ФОНОВЫЙ ПОТОК ОПРОСА ===
        private async Task PollMousePosition()
        {
            var sleepTime = (int)(1000.0 / INPUT_POLLING_RATE_HZ);
            MousePoint? lastPoint = null;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // Проверяем что мышь над элементом
                    if (_targetElement.IsMouseOver && _isCapturing)
                    {
                        // Получаем позицию мыши
                        var currentPos = Mouse.GetPosition(_targetElement);

                        // Проверяем что позиция в пределах элемента
                        if (currentPos.X >= 0 && currentPos.Y >= 0 &&
                            currentPos.X <= _targetElement.RenderSize.Width &&
                            currentPos.Y <= _targetElement.RenderSize.Height)
                        {
                            _lastRawPosition = new MousePoint
                            {
                                Position = currentPos,
                                Timestamp = DateTime.Now,
                                Pressure = GetPressure(),
                                IsPen = IsPenActive()
                            };

                            _lastInputTime = DateTime.Now;

                            // Создаем точку
                            var point = new MousePoint
                            {
                                Position = currentPos,
                                Timestamp = DateTime.Now,
                                Pressure = GetPressure(),
                                IsPen = IsPenActive()
                            };

                            // Добавляем в очередь
                            _pointQueue.Enqueue(point);

                            // Интерполяция между точками
                            if (lastPoint.HasValue)
                            {
                                AddInterpolatedPoints(lastPoint.Value, point);
                            }

                            lastPoint = point;
                        }
                    }
                    else
                    {
                        lastPoint = null; // Сбрасываем при выходе за пределы
                    }

                    await Task.Delay(sleepTime, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MouseInputService] Polling error: {ex.Message}");
                    await Task.Delay(100); // Задержка при ошибке
                }
            }
        }

        // === ИНТЕРПОЛЯЦИЯ ===
        private void AddInterpolatedPoints(MousePoint from, MousePoint to)
        {
            double distance = Math.Sqrt(
                Math.Pow(to.Position.X - from.Position.X, 2) +
                Math.Pow(to.Position.Y - from.Position.Y, 2));

            // Интерполируем только если расстояние достаточно большое
            if (distance > 3.0)
            {
                // Количество промежуточных точек зависит от расстояния
                int steps = Math.Max(1, (int)(distance / 2.5));
                steps = Math.Min(steps, 10); // Ограничиваем

                double timeStep = (to.Timestamp - from.Timestamp).TotalMilliseconds / (steps + 1);
                float pressureStep = (to.Pressure - from.Pressure) / (steps + 1);

                for (int i = 1; i <= steps; i++)
                {
                    double t = i / (double)(steps + 1);

                    var interpolated = new MousePoint
                    {
                        Position = new Point(
                            from.Position.X + (to.Position.X - from.Position.X) * t,
                            from.Position.Y + (to.Position.Y - from.Position.Y) * t),
                        Timestamp = from.Timestamp.AddMilliseconds(timeStep * i),
                        Pressure = from.Pressure + pressureStep * i,
                        IsPen = from.IsPen && to.IsPen // Если оба точки от пера
                    };

                    _pointQueue.Enqueue(interpolated);
                }
            }
        }

        // === ДИСПАТЧ СОБЫТИЙ ===
        private void DispatchPoints(object sender, EventArgs e)
        {
            // Отправляем все накопленные точки
            while (_pointQueue.TryDequeue(out var point))
            {
                MouseMoved?.Invoke(point);
            }
        }

        // === ОБРАБОТЧИКИ СОБЫТИЙ ЭЛЕМЕНТА ===
        private void OnTargetMouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseDown?.Invoke(e);
            StartCapture();
        }

        private void OnTargetMouseUp(object sender, MouseButtonEventArgs e)
        {
            MouseUp?.Invoke(e);
            StopCapture();
        }

        private void OnTargetMouseEnter(object sender, MouseEventArgs e)
        {
            MouseEnter?.Invoke();
        }

        private void OnTargetMouseLeave(object sender, MouseEventArgs e)
        {
            MouseLeave?.Invoke();
        }

        private void OnTargetMouseMove(object sender, MouseEventArgs e)
        {
            // Сохраняем последнюю позицию для точности
            _lastRawPosition.Position = e.GetPosition(_targetElement);
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        private float GetPressure()
        {
            try
            {
                var stylus = Stylus.CurrentStylusDevice;
                if (stylus != null && stylus.Inverted)
                {
                    var points = stylus.GetStylusPoints(_targetElement);
                    if (points != null && points.Count > 0)
                    {
                        return points[0].PressureFactor;
                    }
                }
            }
            catch { }

            return 1.0f;
        }

        private bool IsPenActive()
        {
            try
            {
                return Stylus.CurrentStylusDevice?.Inverted ?? false;
            }
            catch
            {
                return false;
            }
        }

        // === ОЧИСТКА ===
        public void Dispose()
        {
            Stop();

            // Отписка от событий
            _targetElement.MouseLeftButtonDown -= OnTargetMouseDown;
            _targetElement.MouseLeftButtonUp -= OnTargetMouseUp;
            _targetElement.MouseEnter -= OnTargetMouseEnter;
            _targetElement.MouseLeave -= OnTargetMouseLeave;
            _targetElement.MouseMove -= OnTargetMouseMove;

            _dispatchTimer?.Stop();
            _cts?.Dispose();

            Debug.WriteLine("[MouseInputService] Disposed");
        }
    }
}