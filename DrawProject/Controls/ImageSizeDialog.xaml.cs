// SimpleImageSizeDialog.xaml.cs
using System.Windows;

namespace DrawProject.Controls
{
    public partial class ImageSizeDialog : Window
    {
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }
        public double Resolution { get; private set; }

        public ImageSizeDialog()
        {
            InitializeComponent();
        }

        public ImageSizeDialog(double initialWidth, double initialHeight)
            : this()
        {
            WidthTextBox.Text = initialWidth.ToString();
            HeightTextBox.Text = initialHeight.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WidthTextBox.Text, out double width) &&
                double.TryParse(HeightTextBox.Text, out double height))
            {
                ImageWidth = width;
                ImageHeight = height;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректные числовые значения", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}