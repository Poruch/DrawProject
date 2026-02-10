using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace DrawProject.Services
{
    public class SimpleMouseService : IDisposable
    {
        private readonly UIElement _targetElement;
        private readonly DispatcherTimer _timer;
        private bool _isCapturing = false;

        public event Action<Point> MouseMoved;
        public event Action<MouseButtonEventArgs> MouseDown;
        public event Action<MouseButtonEventArgs> MouseUp;

        public SimpleMouseService(UIElement targetElement)
        {
            _targetElement = targetElement;

            Debug.WriteLine($"[SimpleMouseService] Created for element: {targetElement}");

            // ПОДПИСКА НА РЕАЛЬНЫЕ СОБЫТИЯ
            _targetElement.MouseLeftButtonDown += OnTargetMouseDown;
            _targetElement.MouseLeftButtonUp += OnTargetMouseUp;
            _targetElement.MouseMove += OnTargetMouseMove;

            // Таймер для опроса мыши (125 Гц)
            _timer = new DispatcherTimer(DispatcherPriority.Input)
            {
                Interval = TimeSpan.FromMilliseconds(8)
            };
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            if (_timer.IsEnabled) return;

            _timer.Start();
            Debug.WriteLine("[SimpleMouseService] Started");
        }

        public void Stop()
        {
            _timer.Stop();
            Debug.WriteLine("[SimpleMouseService] Stopped");
        }

        // === ОБРАБОТЧИКИ РЕАЛЬНЫХ СОБЫТИЙ ===
        private void OnTargetMouseDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[SimpleMouseService] Real mouse down at {e.GetPosition(_targetElement)}");
            _isCapturing = true;
            MouseDown?.Invoke(e);
        }

        private void OnTargetMouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"[SimpleMouseService] Real mouse up at {e.GetPosition(_targetElement)}");
            _isCapturing = false;
            MouseUp?.Invoke(e);
        }

        private void OnTargetMouseMove(object sender, MouseEventArgs e)
        {
            if (_isCapturing)
            {
                Debug.WriteLine($"[SimpleMouseService] Real mouse move at {e.GetPosition(_targetElement)}");
                MouseMoved?.Invoke(e.GetPosition(_targetElement));
            }
        }

        // === ТАЙМЕР ДЛЯ ОПРОСА ===
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (_isCapturing && _targetElement.IsMouseOver)
            {
                var pos = Mouse.GetPosition(_targetElement);
                // Debug.WriteLine($"[SimpleMouseService] Timer mouse move at {pos}");
                MouseMoved?.Invoke(pos);
            }
        }

        public void Dispose()
        {
            Stop();
            _targetElement.MouseLeftButtonDown -= OnTargetMouseDown;
            _targetElement.MouseLeftButtonUp -= OnTargetMouseUp;
            _targetElement.MouseMove -= OnTargetMouseMove;
        }
    }
}