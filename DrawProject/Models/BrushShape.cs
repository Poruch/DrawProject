using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Windows.Controls;

namespace DrawProject.Models
{
    public abstract class BrushShape
    {
        public abstract string Name { get; }

        public float Hardness
        {
            get => _hardness;
            set
            {
                _hardness = Math.Clamp(value, 0f, 1f);
                ClearMaskCache();
            }
        }

        protected float _hardness = 0.5f;

        // === КЭШ МАСОК ===
        private Dictionary<(int, float), float[,]> _maskCache = new();
        private const int MAX_MASK_CACHE = 10;

        // === КЭШ ПРЕВЬЮ ===
        private Dictionary<(int, Color, float), UIElement> _previewCache = new();
        private const int MAX_PREVIEW_CACHE = 20;

        // === ОСНОВНЫЕ МЕТОДЫ ===
        public float[,] GetMask(int size)
        {
            var key = (size, Hardness);

            if (_maskCache.TryGetValue(key, out var cached))
                return cached;

            var mask = CalculateMask(size);
            CacheMask(key, mask);
            return mask;
        }

        public UIElement GetPreviewElement(Point center, int size, Color color, float opacity)
        {
            var key = (size, color, opacity);

            if (_previewCache.TryGetValue(key, out var template))
                return CloneAndPosition(template, center, size);

            var element = CreatePreviewTemplate(size, color, opacity);
            CachePreview(key, element);
            return CloneAndPosition(element, center, size);
        }

        // === АБСТРАКТНЫЕ МЕТОДЫ ===
        protected abstract float[,] CalculateMask(int size);
        protected abstract UIElement CreatePreviewTemplate(int size, Color color, float opacity);
        public abstract bool IsPointInShape(Point point, Point center, int size);

        // === КЭШИРОВАНИЕ ===
        private void CacheMask((int, float) key, float[,] mask)
        {
            if (_maskCache.Count >= MAX_MASK_CACHE)
                _maskCache.Clear();

            _maskCache[key] = mask;
        }

        private void CachePreview((int, Color, float) key, UIElement element)
        {
            if (_previewCache.Count >= MAX_PREVIEW_CACHE)
                _previewCache.Clear();

            _previewCache[key] = element;
        }

        private UIElement CloneAndPosition(UIElement template, Point center, int size)
        {
            var clone = CloneElement(template);
            PositionElement(clone, center, size);
            return clone;
        }

        // === ВСПОМОГАТЕЛЬНЫЕ ===
        protected virtual UIElement CloneElement(UIElement template)
        {
            if (template is Ellipse ellipse)
                return new Ellipse
                {
                    Width = ellipse.Width,
                    Height = ellipse.Height,
                    Fill = ellipse.Fill,
                    Opacity = ellipse.Opacity
                };

            if (template is Rectangle rect)
                return new Rectangle
                {
                    Width = rect.Width,
                    Height = rect.Height,
                    Fill = rect.Fill,
                    Opacity = rect.Opacity,
                    Stroke = rect.Stroke,
                    StrokeThickness = rect.StrokeThickness
                };

            throw new NotImplementedException();
        }

        protected virtual void PositionElement(UIElement element, Point center, int size)
        {
            if (element is FrameworkElement fe)
            {
                fe.SetValue(Canvas.LeftProperty, center.X - size / 2);
                fe.SetValue(Canvas.TopProperty, center.Y - size / 2);
            }
        }

        public void ClearMaskCache() => _maskCache.Clear();
        public void ClearPreviewCache() => _previewCache.Clear();
        public void ClearAllCache()
        {
            ClearMaskCache();
            ClearPreviewCache();
        }
    }

    // === РЕАЛИЗАЦИИ ===

    public class CircleBrushShape : BrushShape
    {
        public override string Name => "Circle";

        protected override float[,] CalculateMask(int size)
        {
            var mask = new float[size, size];
            int radius = size / 2;
            float hardRadius = radius * Hardness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    if (distance <= hardRadius)
                        mask[x, y] = 1.0f;
                    else if (distance <= radius)
                        mask[x, y] = 1.0f - (distance - hardRadius) / (radius - hardRadius);
                }
            }

            return mask;
        }

        protected override UIElement CreatePreviewTemplate(int size, Color color, float opacity)
        {
            return new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Opacity = opacity * 0.7f
            };
        }

        public override bool IsPointInShape(Point point, Point center, int size)
        {
            float dx = (float)(point.X - center.X);
            float dy = (float)(point.Y - center.Y);
            return Math.Sqrt(dx * dx + dy * dy) <= size / 2;
        }
    }

    public class SquareBrushShape : BrushShape
    {
        public override string Name => "Square";

        protected override float[,] CalculateMask(int size)
        {
            var mask = new float[size, size];
            int hardBorder = Math.Max(1, (int)(size * (1 - Hardness) / 2));

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Math.Min(
                        Math.Min(x, size - 1 - x),
                        Math.Min(y, size - 1 - y));

                    mask[x, y] = Math.Min(1.0f, dist / hardBorder);
                }
            }

            return mask;
        }

        protected override UIElement CreatePreviewTemplate(int size, Color color, float opacity)
        {
            return new Rectangle
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(color),
                Opacity = opacity * 0.7f,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1
            };
        }

        public override bool IsPointInShape(Point point, Point center, int size)
        {
            return Math.Abs(point.X - center.X) <= size / 2 &&
                   Math.Abs(point.Y - center.Y) <= size / 2;
        }
    }
}