using DrawProject.Controls;
using DrawProject.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
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
            Closing += MainWindow_Closing;


            MainViewModel model = DataContext as MainViewModel;
            var elements = model.GenerateToolMenuItems();

            if (elements.Count > 0)
                elements[0].Command.Execute(null);
            else
                Debug.WriteLine("Ошибка, инструментов должно быть больше 1");

            for (int i = 0; i < elements.Count; i++)
            {
                ToolBox.Items.Add(elements[i]);
            }
            this.MouseMove += OnMouseMove;
            //_drawingCanvas = FindName("drawingCanvas") as HybridCanvas;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.MousePosition = e.GetPosition(sender as IInputElement);
            }
        }

        private void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.DrawingCanvas = sender as HybridCanvas;
            }
        }


        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            var doc = (DataContext as MainViewModel).CurrentDoc;
            // Проверяем, есть ли несохраненные изменения
            if (doc != null && doc.IsUnSaved)
            {
                // Проигрываем звук предупреждения
                SystemSounds.Exclamation.Play();

                // Показываем диалог
                MessageBoxResult result = MessageBox.Show(
                    "У вас есть несохраненные изменения. Сохранить перед выходом?",
                    "Подтверждение выхода",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Сохранить и выйти
                        try
                        {
                            (DataContext as MainViewModel).SaveCommand.Execute(null);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            e.Cancel = true;
                        }
                        break;

                    case MessageBoxResult.No:
                        // Выйти без сохранения
                        break;

                    case MessageBoxResult.Cancel:
                        // Отменить закрытие
                        e.Cancel = true;
                        break;
                }
            }
        }
        private void MyColorWheel_ColorChanged(object sender, Color e)
        {

        }
    }
}