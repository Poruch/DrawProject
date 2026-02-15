using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для ColorSlider.xaml
    /// </summary>
    public partial class ColorSlider : UserControl
    {
        // Флаг для предотвращения циклических обновлений
        private bool _isUpdatingFromSelectedColor = false;

        // Dependency Properties
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorSlider),
                new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        public static readonly DependencyProperty RedProperty =
            DependencyProperty.Register("Red", typeof(int), typeof(ColorSlider),
                new PropertyMetadata(0, OnRgbChanged)); // Исправлено: 255 вместо 0

        public static readonly DependencyProperty GreenProperty =
            DependencyProperty.Register("Green", typeof(int), typeof(ColorSlider),
                new PropertyMetadata(0, OnRgbChanged));

        public static readonly DependencyProperty BlueProperty =
            DependencyProperty.Register("Blue", typeof(int), typeof(ColorSlider),
                new PropertyMetadata(0, OnRgbChanged));

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register("Alpha", typeof(int), typeof(ColorSlider),
                new PropertyMetadata(255, OnRgbChanged));

        public static readonly DependencyProperty PreviewColorProperty =
            DependencyProperty.Register("PreviewColor", typeof(SolidColorBrush), typeof(ColorSlider),
                new PropertyMetadata(new SolidColorBrush(Colors.Red)));

        public static readonly DependencyProperty ColorChangedCommandProperty =
            DependencyProperty.Register("ColorChangedCommand", typeof(ICommand), typeof(ColorSlider));

        // Properties
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public int Red
        {
            get => (int)GetValue(RedProperty);
            set => SetValue(RedProperty, Math.Clamp(value, 0, 255));
        }

        public int Green
        {
            get => (int)GetValue(GreenProperty);
            set => SetValue(GreenProperty, Math.Clamp(value, 0, 255));
        }

        public int Blue
        {
            get => (int)GetValue(BlueProperty);
            set => SetValue(BlueProperty, Math.Clamp(value, 0, 255));
        }

        public int Alpha
        {
            get => (int)GetValue(AlphaProperty);
            set => SetValue(AlphaProperty, Math.Clamp(value, 0, 255));
        }

        public SolidColorBrush PreviewColor
        {
            get => (SolidColorBrush)GetValue(PreviewColorProperty);
            set => SetValue(PreviewColorProperty, value);
        }

        public ICommand ColorChangedCommand
        {
            get => (ICommand)GetValue(ColorChangedCommandProperty);
            set => SetValue(ColorChangedCommandProperty, value);
        }

        public ColorSlider()
        {
            InitializeComponent();
            UpdatePreviewColor();
            Loaded += ColorSlider_Loaded;
        }

        private void ColorSlider_Loaded(object sender, RoutedEventArgs e)
        {
            SyncSlidersFromColor();
        }

        private void SyncSlidersFromColor()
        {
            if (RedSlider != null) RedSlider.Value = Red;
            if (GreenSlider != null) GreenSlider.Value = Green;
            if (BlueSlider != null) BlueSlider.Value = Blue;
            if (AlphaSlider != null) AlphaSlider.Value = Alpha;
            UpdatePreviewColor();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorSlider)d;
            var color = (Color)e.NewValue;

            control._isUpdatingFromSelectedColor = true;
            try
            {
                control.Red = color.R;
                control.Green = color.G;
                control.Blue = color.B;
                control.Alpha = color.A;
            }
            finally
            {
                control._isUpdatingFromSelectedColor = false;
            }

            control.UpdatePreviewColor();
            control.ColorChangedCommand?.Execute(color);
        }

        private static void OnRgbChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorSlider)d;

            if (control._isUpdatingFromSelectedColor)
                return;

            control.SelectedColor = Color.FromArgb(
                (byte)control.Alpha,
                (byte)control.Red,
                (byte)control.Green,
                (byte)control.Blue);
        }

        private void UpdatePreviewColor()
        {
            PreviewColor = new SolidColorBrush(SelectedColor);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (slider == null) return;

            switch (slider.Name)
            {
                case "RedSlider":
                    Red = (int)slider.Value;
                    break;
                case "GreenSlider":
                    Green = (int)slider.Value;
                    break;
                case "BlueSlider":
                    Blue = (int)slider.Value;
                    break;
                case "AlphaSlider":
                    Alpha = (int)slider.Value;
                    break;
            }
        }

        /// <summary>
        /// Устанавливает цвет и синхронизирует слайдеры.
        /// Аналогично установке SelectedColor, но с явным вызовом.
        /// </summary>
        public void SetColor(Color color)
        {
            SelectedColor = color;
        }

        /// <summary>
        /// Возвращает текущий цвет (обёртка над SelectedColor для удобства).
        /// </summary>
        public Color CurrentColor => SelectedColor;
    }
}