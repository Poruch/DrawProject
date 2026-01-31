using DrawProject.Controls;
using DrawProject.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HybridCanvas _drawingCanvas;
        public MainWindow()
        {

            //DataContext = new MainViewModel();

            InitializeComponent();

            _drawingCanvas = FindName("drawingCanvas") as HybridCanvas;
        }

        private void MyColorWheel_ColorChanged(object sender, Color e)
        {

        }
    }
}