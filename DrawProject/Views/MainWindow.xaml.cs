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
using System.Windows.Threading;
using Xceed.Wpf.AvalonDock.Layout;
using System.IO;
using System.Xml;
using Xceed.Wpf.AvalonDock.Layout.Serialization;


namespace DrawProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        public ICommand ResetPanelSizesCommand { get; }
        int leftWifth = 150;
        int rightWifth = 150;
        MainViewModel model = null;
        public MainWindow()
        {
            InitializeComponent();
            Closing += MainWindow_Closing;
            ResetPanelSizesCommand = new RelayCommand(ResetPanelSizes);
            model = DataContext as MainViewModel;
            model.DrawingCanvas = drawingCanvas;
            var menuItems = model.GenerateToolRibbonControls();

            ToolsGroup.Items.Clear();

            // Добавление новых кнопок
            foreach (var button in menuItems)
            {
                ToolsGroup.Items.Add(button);
            }
            model.ApplyDefaultTool();
            this.MouseMove += OnMouseMove;
            this.Loaded += MainWindow_Loaded;


            SaveOriginalLayout();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void DrawingCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.DrawingCanvas = sender as HybridCanvas;
            }
        }


        private string _originalLayout;
        private void SaveOriginalLayout()
        {
            if (dockManager == null) return;
            var serializer = new XmlLayoutSerializer(dockManager);
            using (var stringWriter = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(stringWriter))
            {
                serializer.Serialize(xmlWriter);
                _originalLayout = stringWriter.ToString();
            }
        }

        private void ResetPanelSizes()
        {
            if (string.IsNullOrEmpty(_originalLayout))
            {
                SaveOriginalLayout();
                return;
            }

            try
            {
                var serializer = new XmlLayoutSerializer(dockManager);
                using (var stringReader = new StringReader(_originalLayout))
                using (var xmlReader = XmlReader.Create(stringReader))
                {
                    serializer.Deserialize(xmlReader);
                }
                model.UpdateImage();
            }
            catch (Exception ex)
            {
                // Логирование ошибки при необходимости
                Debug.WriteLine($"Ошибка сброса макета: {ex.Message}");
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                OpenProgrammInfo(sender, null);
            }
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


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Закрывает текущее окно (и приложение, если это главное окно)
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