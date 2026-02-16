// Models/Brush.cs
using DrawProject.Models;
using DrawProject.ViewModels;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;

public class Brush : ObservableObject
{
    // === СВОЙСТВА ===

    private Color _color = Colors.Black;
    public Color Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                OnPropertyChanged();
            }
        }
    }
    public int Size { get; set; } = 5;
    public float Opacity { get; set; } = 1.0f;
    float hardness = 0.5f;
    public float Hardness
    {
        get { return hardness; }
        set
        {
            hardness = value;
            Shape.Hardness = hardness;
        }
    }
    public float Spacing { get; set; } = 0.25f; // Интервал между отпечатками

    // === ФОРМА КИСТИ ===
    public BrushShape Shape { get; set; } = new CircleBrushShape();

    // === ДЛЯ РИСОВАНИЯ ===
    private float[,] _cachedMask; // Кэшированная маска
    private bool _maskDirty = true;

    // Получить маску кисти (с кэшированием)
    public float[,] GetMask()
    {
        if (_maskDirty || _cachedMask == null)
        {

            _cachedMask = Shape.GetMask(Size);
            _maskDirty = false;
        }
        return _cachedMask;
    }

    // Применить кисть в точке
    public void ApplyAt(WriteableBitmap bitmap, Point center)
    {
        var mask = GetMask();
        int halfSize = Size / 2;

        for (int y = 0; y < Size; y++)
        {
            int bitmapY = (int)center.Y + y - halfSize;
            if (bitmapY < 0 || bitmapY >= bitmap.PixelHeight) continue;

            for (int x = 0; x < Size; x++)
            {
                int bitmapX = (int)center.X + x - halfSize;
                if (bitmapX < 0 || bitmapX >= bitmap.PixelWidth) continue;

                float maskValue = mask[x, y];
                if (maskValue <= 0) continue;

                // Рисуем пиксель с учетом маски и прозрачности
                DrawPixel(bitmap, bitmapX, bitmapY, Color, maskValue * Opacity);
            }
        }
    }

    // Отрисовать превью кисти (для векторного слоя)
    public UIElement CreatePreview(Point center)
    {
        return Shape.GetPreviewElement(center, Size, Color, Opacity);
    }

    private void DrawPixel(WriteableBitmap bitmap, int x, int y,
        Color color, float alpha)
    {
        // Твоя реализация рисования пикселя
    }
}