using DrawProject.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
namespace DrawProject.Models.Instruments
{
    public class InstrumentContext
    {
        public Point Position { get; }
        public Point LastPosition { get; }
        public MouseButtonEventArgs MouseButtonArgs { get; }
        public MouseEventArgs MouseArgs { get; }

        public Brush Brush { get; }
        // === Контекст приложения ===
        public HybridCanvas Canvas { get; }
        public ImageDocument Document => Canvas?.ImageDocument;
        public Canvas VectorOverlay { get; }

        // === Давление пера ===
        public float Pressure { get; private set; } = 1.0f;

        // === Вспомогательные свойства ===
        public bool IsLeftButtonPressed => Mouse.LeftButton == MouseButtonState.Pressed;
        public bool IsPenActive { get; private set; }

        // === Конструкторы ===

        public InstrumentContext(HybridCanvas canvas, MouseButtonEventArgs e, Point? lastPosition = null)
        {
            Canvas = canvas;
            MouseButtonArgs = e;
            MouseArgs = e;
            Position = e.GetPosition(canvas);
            Brush = canvas.Brush;
            VectorOverlay = canvas.GetVectorOverlay();
            UpdatePressure();
            LastPosition = lastPosition != null ? lastPosition.Value : Position;
        }

        public InstrumentContext(HybridCanvas canvas, MouseEventArgs e, Point? lastPosition = null)
        {
            Canvas = canvas;
            MouseArgs = e;
            Position = e.GetPosition(canvas);
            VectorOverlay = canvas.GetVectorOverlay();
            Brush = canvas.Brush;
            UpdatePressure();
            LastPosition = lastPosition != null ? lastPosition.Value : Position;

        }

        public InstrumentContext(HybridCanvas canvas, Point position, Point? lastPosition = null)
        {
            Canvas = canvas;
            Position = position;
            VectorOverlay = canvas.GetVectorOverlay();
            Brush = canvas.Brush;
            UpdatePressure();
            LastPosition = lastPosition != null ? lastPosition.Value : Position;
        }

        /// <summary>
        /// Обновляет значение давления пера
        /// </summary>
        public void UpdatePressure()
        {
            try
            {
                var stylus = Stylus.CurrentStylusDevice;
                if (stylus != null)
                {
                    IsPenActive = stylus.Inverted;
                    var points = stylus.GetStylusPoints(Canvas);

                    if (points != null && points.Count > 0)
                    {
                        Pressure = points[0].PressureFactor;
                        return;
                    }
                }
            }
            catch
            {
                // В случае ошибки используем значение по умолчанию
            }

            // Если перо не активно или произошла ошибка
            IsPenActive = false;
            Pressure = 1.0f;
        }

        /// <summary>
        /// Получает размер кисти с учетом давления
        /// </summary>
        public int GetPressureAdjustedSize(int baseSize)
        {
            return (int)(baseSize * Pressure);
        }
    }
}
