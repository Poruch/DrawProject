using DrawProject.Controls;
using DrawProject.Models.Instruments;
using DrawProject.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace DrawProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ICommand ResetPanelSizesCommand { get; }
        private HybridCanvas _drawingCanvas;
        public MainWindow()
        {

            //DataContext = new MainViewModel();

            InitializeComponent();
            Closing += MainWindow_Closing;
            ResetPanelSizesCommand = new RelayCommand(ResetPanelSizes);

            var model = DataContext as MainViewModel;
            model.DrawingCanvas = _drawingCanvas;
            var menuItems = model.GenerateToolMenuItems();
            // Добавление в ToolBox (предполагается, что ToolBox — это ItemsControl)
            ToolBox.Items.Clear();
            foreach (var item in menuItems)
            {
                ToolBox.Items.Add(item);
            }
            model.ApplyDefaultTool();
            this.MouseMove += OnMouseMove;
            //_drawingCanvas = FindName("drawingCanvas") as HybridCanvas;
        }

        private void ResetPanelSizes()
        {
            // Сохраняем желаемые значения
            var leftWidth = new GridLength(150);
            var rightWidth = new GridLength(150);

            // Применяем их
            LeftColumn.Width = leftWidth;
            RightColumn.Width = rightWidth;

            // Ключевой момент: откладываем UpdateLayout до завершения рендеринга
            Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    MainContentGrid.UpdateLayout();
                }),
                System.Windows.Threading.DispatcherPriority.Render
            );
        }


        public void OpenProgrammInfo(object sender, RoutedEventArgs e)
        {
            aboutPopup.IsOpen = !aboutPopup.IsOpen;
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