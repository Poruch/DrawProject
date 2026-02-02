using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using System.Windows;

public class Layer : INotifyPropertyChanged
{
    static int count = 0;

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    private WriteableBitmap _source;
    public WriteableBitmap Source
    {
        get => _source;
        set
        {
            _source = value;
            OnPropertyChanged();
        }
    }

    public Layer() { count++; }

    public Layer(string name, WriteableBitmap source) : this()
    {
        Name = name;
        Source = source;
    }

    public Layer(WriteableBitmap source) : this()
    {
        Name = $"Layer {count}";
        Source = source;
    }

    // === МЕТОДЫ ДЛЯ ОБНОВЛЕНИЯ ПИКСЕЛЕЙ ===

    // Обновить слой из BitmapSource
    public void UpdatePixels(BitmapSource source)
    {
        if (Source == null || source == null) return;

        int stride = (Source.PixelWidth * 4);
        byte[] pixels = new byte[Source.PixelHeight * stride];
        source.CopyPixels(pixels, stride, 0);

        UpdatePixels(pixels, stride);
    }

    // Обновить слой из массива пикселей
    public void UpdatePixels(byte[] pixels, int stride)
    {
        if (Source == null || pixels == null) return;

        var rect = new Int32Rect(0, 0, Source.PixelWidth, Source.PixelHeight);
        Source.WritePixels(rect, pixels, stride, 0);

        // Уведомляем об изменении
        OnPropertyChanged(nameof(Source));
    }

    // Очистить слой
    public void ClearLayer(byte[] clearPixels, int stride)
    {
        if (Source == null) return;

        var rect = new Int32Rect(0, 0, Source.PixelWidth, Source.PixelHeight);
        Source.WritePixels(rect, clearPixels, stride, 0);

        OnPropertyChanged(nameof(Source));
    }

    // Обновить область с пикселями
    public void UpdatePixels(Int32Rect rect, byte[] pixels, int stride)
    {
        if (Source == null || pixels == null) return;

        Source.WritePixels(rect, pixels, stride, 0);
        OnPropertyChanged(nameof(Source));
    }

    // Обновить с AddDirtyRect
    public void UpdateWithDirtyRect(Int32Rect dirtyRect)
    {
        if (Source == null) return;

        Source.AddDirtyRect(dirtyRect);
        OnPropertyChanged(nameof(Source));
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}