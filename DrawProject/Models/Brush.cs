// Models/Brush.cs
using DrawProject.Models;
using DrawProject.ViewModels;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using static DrawProject.Models.BrushShape;

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

}