using System;
using System.Windows;
using System.Windows.Threading;

namespace DrawProject.Extensions
{
    public static class UIElementExtensions
    {
        /// <summary>
        /// Запускает функцию с заданной частотой
        /// </summary>
        /// <param name="element">UI элемент</param>
        /// <param name="intervalMs">Интервал в миллисекундах</param>
        /// <param name="action">Функция для выполнения</param>
        /// <returns>Таймер (для остановки)</returns>
        public static DispatcherTimer StartAtRate(this UIElement element,
            int intervalMs, Action action)
        {
            var timer = new DispatcherTimer(DispatcherPriority.Input, element.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };

            timer.Tick += (s, e) => action();
            timer.Start();

            return timer;
        }

        /// <summary>
        /// Запускает функцию с заданной частотой (Гц)
        /// </summary>
        public static DispatcherTimer StartAtFrequency(this UIElement element,
            int frequencyHz, Action action)
        {
            return StartAtRate(element, 1000 / frequencyHz, action);
        }
    }
}