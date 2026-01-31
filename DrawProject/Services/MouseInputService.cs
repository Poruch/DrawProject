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


        // === ДИСПАТЧ СОБЫТИЙ ===
        private void DispatchPoints(object sender, EventArgs e)
        {
            var position = Mouse.GetPosition(_targetElement);
            MousePoint mousePoint = new MousePoint()
            {
                Position = position,
                Pressure = GetPressure()
            };
            MouseMoved?.Invoke(mousePoint);
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