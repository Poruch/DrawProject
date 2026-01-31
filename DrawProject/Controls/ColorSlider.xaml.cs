using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для ColorSlendet.xaml
    /// </summary>
    public partial class ColorSlider : UserControl
    {
        // Dependency Properties
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorSlider),
                new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        public static readonly DependencyProperty RedProperty =
            DependencyProperty.Register("Red", typeof(int), typeof(ColorSlider),
                new PropertyMetadata(255, OnRgbChanged));

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

        // Properties
        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set
            {
                SetValue(SelectedColorProperty, value);
            }
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


        // Свойство для команды (если нужно)
        public static readonly DependencyProperty ColorChangedCommandProperty =
            DependencyProperty.Register("ColorChangedCommand", typeof(ICommand), typeof(ColorSlider));

        public ICommand ColorChangedCommand
        {
            get { return (ICommand)GetValue(ColorChangedCommandProperty); }
            set { SetValue(ColorChangedCommandProperty, value); }
        }

        public ColorSlider()
        {
            InitializeComponent();
            UpdatePreviewColor();

        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorSlider)d;
            var color = (Color)e.NewValue;
            control.Red = color.R;
            control.Green = color.G;
            control.Blue = color.B;
            control.Alpha = color.A;
            control.UpdatePreviewColor();
            control.ColorChangedCommand.Execute(color);
        }

        private static void OnRgbChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorSlider)d;
            control.SelectedColor = Color.FromArgb(
                (byte)control.Alpha,
                (byte)control.Red,
                (byte)control.Green,
                (byte)control.Blue);
            control.UpdatePreviewColor();
        }

        private void UpdatePreviewColor()
        {
            PreviewColor = new SolidColorBrush(SelectedColor);
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            if (slider == null) return;


            // Обновляем соответствующие свойства
            if (slider.Name == "RedSlider")
                Red = (byte)slider.Value;
            else if (slider.Name == "GreenSlider")
                Green = (byte)slider.Value;
            else if (slider.Name == "BlueSlider")
                Blue = (byte)slider.Value;
            else if (slider.Name == "AlphaSlider")
                Alpha = (byte)slider.Value;

            SelectedColor = Color.FromArgb((byte)Alpha, (byte)Red, (byte)Green, (byte)Blue);
        }

    }
}
