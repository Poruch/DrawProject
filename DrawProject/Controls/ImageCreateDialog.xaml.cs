// SimpleImageSizeDialog.xaml.cs
using System.Windows;

namespace DrawProject.Controls
{
    public partial class CreateImageDialog : Window
    {
        public double ImageWidth { get; private set; }
        public double ImageHeight { get; private set; }
        public double Resolution { get; private set; }

        public CreateImageDialog()
        {
            InitializeComponent();
        }

        public CreateImageDialog(double initialWidth, double initialHeight, double initialResolution = 96)
            : this()
        {
            WidthTextBox.Text = initialWidth.ToString();
            HeightTextBox.Text = initialHeight.ToString();
            ResolutionTextBox.Text = initialResolution.ToString();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(WidthTextBox.Text, out double width) &&
                double.TryParse(HeightTextBox.Text, out double height) &&
                double.TryParse(ResolutionTextBox.Text, out double resolution))
            {
                ImageWidth = width;
                ImageHeight = height;
                Resolution = resolution;
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