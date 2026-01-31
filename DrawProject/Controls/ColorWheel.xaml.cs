// В ColorWheelControl.xaml.cs добавьте:
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DrawProject.Controls
{
    public partial class ColorWheelControl : UserControl
    {
        // Свойство для выбранного цвета
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorWheelControl),
                new PropertyMetadata(Colors.Red, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        // Свойство для команды (если нужно)
        public static readonly DependencyProperty ColorChangedCommandProperty =
            DependencyProperty.Register("ColorChangedCommand", typeof(ICommand), typeof(ColorWheelControl));

        public ICommand ColorChangedCommand
        {
            get { return (ICommand)GetValue(ColorChangedCommandProperty); }
            set { SetValue(ColorChangedCommandProperty, value); }
        }


        public static readonly DependencyProperty ColorChangedCommandParameterProperty =
            DependencyProperty.Register("ColorChangedCommandParameter", typeof(object),
                typeof(ColorWheelControl), new PropertyMetadata(null));

        public object ColorChangedCommandParameter
        {
            get => GetValue(ColorChangedCommandParameterProperty);
            set => SetValue(ColorChangedCommandParameterProperty, value);
        }

        public event EventHandler<Color> ColorChanged;
        public ColorWheelControl()
        {
            InitializeComponent();

            // Убедитесь, что контрол видим
            this.Visibility = Visibility.Visible;
            this.IsEnabled = true;

            Loaded += ColorWheelControl_Loaded;
        }

        private void ColorWheelControl_Loaded(object sender, RoutedEventArgs e)
        {
            // Проверка загрузки
            System.Diagnostics.Debug.WriteLine("ColorWheelControl loaded!");
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ColorWheelControl)d;
            // Обновляем цвет селектора
            if (control.Selector != null)
                control.Selector.Fill = new SolidColorBrush((Color)e.NewValue);

        }

        // Обработчики событий мыши
        private void ColorWheel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Selector.Visibility = Visibility.Visible;
            UpdateSelectorPosition(e.GetPosition(ColorWheelEllipse));
        }

        private void ColorWheel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                UpdateSelectorPosition(e.GetPosition(ColorWheelEllipse));
        }

        private void ColorWheel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // Можно добавить логику
        }

        private void UpdateSelectorPosition(Point position)
        {
            // Простая логика позиционирования
            double radius = ColorWheelEllipse.ActualWidth / 2;
            double centerX = radius;
            double centerY = radius;

            double dx = position.X - centerX;
            double dy = position.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance > radius)
            {
                dx = dx * radius / distance;
                dy = dy * radius / distance;
                distance = radius;
            }

            Canvas.SetLeft(Selector, centerX + dx - 6);
            Canvas.SetTop(Selector, centerY + dy - 6);

            // Обновляем цвет
            UpdateColorFromPosition(position);
        }

        private void UpdateColorFromPosition(Point position)
        {
            // Простая логика определения цвета по позиции
            double centerX = ColorWheelEllipse.ActualWidth / 2;
            double centerY = ColorWheelEllipse.ActualHeight / 2;
            double radius = Math.Min(centerX, centerY);

            double angle = Math.Atan2(position.Y - centerY, position.X - centerX) * 180 / Math.PI;
            if (angle < 0) angle += 360;

            // Простое присвоение цветов по углу
            Color newColor = angle switch
            {
                < 60 => Colors.Red,
                < 120 => Colors.Yellow,
                < 180 => Colors.Green,
                < 240 => Colors.Cyan,
                < 300 => Colors.Blue,
                _ => Colors.Magenta
            };

            SelectedColor = newColor;

            // Вызываем команду, если она задана
            if (ColorChangedCommand != null && ColorChangedCommand.CanExecute(newColor))
                ColorChangedCommand.Execute(newColor);
            //ColorChanged.Invoke(this, newColor);
        }
    }
}