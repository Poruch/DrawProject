using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace DrawProject.Services
{
    public class RateLimitedExecutor : IDisposable
    {
        private readonly UIElement _element;
        private readonly DispatcherPriority _priority;
        private DispatcherTimer _timer;
        private Action _currentAction;
        private int _currentRateHz;

        public RateLimitedExecutor(UIElement element,
            DispatcherPriority priority = DispatcherPriority.Input)
        {
            _element = element;
            _priority = priority;
        }

        /// <summary>
        /// Запускает выполнение функции с заданной частотой
        /// </summary>
        public void Start(int frequencyHz, Action action)
        {
            Stop();

            _currentAction = action;
            _currentRateHz = frequencyHz;

            _timer = new DispatcherTimer(_priority, _element.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / frequencyHz)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Debug.WriteLine($"[RateLimitedExecutor] Started at {frequencyHz}Hz");
        }

        /// <summary>
        /// Изменяет частоту выполнения
        /// </summary>
        public void ChangeRate(int newFrequencyHz)
        {
            if (_timer != null && _timer.IsEnabled)
            {
                _currentRateHz = newFrequencyHz;
                _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / newFrequencyHz);

                Debug.WriteLine($"[RateLimitedExecutor] Rate changed to {newFrequencyHz}Hz");
            }
        }

        /// <summary>
        /// Останавливает выполнение
        /// </summary>
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer = null;
                _currentAction = null;

                Debug.WriteLine("[RateLimitedExecutor] Stopped");
            }
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            try
            {
                _currentAction?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[RateLimitedExecutor] Error: {ex.Message}");
            }
        }

        public bool IsRunning => _timer?.IsEnabled ?? false;
        public int CurrentRateHz => _currentRateHz;

        public void Dispose()
        {
            Stop();
        }
    }
}