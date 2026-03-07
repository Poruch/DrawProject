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
using DrawProject.Services.Plugins;
using DrawProject.Models;


namespace DrawProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Fluent.RibbonWindow
    {
        public ICommand LoadPluginCommand { get; }
        public ICommand ResetPanelSizesCommand { get; }
        public ICommand OpenPluginDialogCommand { get; }
        private int leftWifth = 150;
        private int rightWifth = 150;
        private MainViewModel model = null;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                Loaded += MainWindow_Loaded;
                Closing += MainWindow_Closing;

                ResetPanelSizesCommand = new RelayCommand(ResetPanelSizes);
                OpenPluginDialogCommand = new RelayCommand(OpenPluginsWindow);
                LoadPluginCommand = new RelayCommand(LoadPlugin);

                model = DataContext as MainViewModel;
                model.DrawingCanvas = drawingCanvas;

                model.AddPlugin(new PluginController(new MainPlugin()));

                UpdateInterface();
                model.ApplyDefaultTool();
                this.MouseMove += OnMouseMove;


                SaveOriginalLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Что то пошло вообще не так \n" + ex.Message);
                Application.Current.Shutdown();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in App.Config.Plugins)
            {
                try
                {
                    var newPlugins = PluginService.GetPluginsFromFile(item.Path);
                    if (newPlugins == null || newPlugins.Count == 0) continue;
                    for (int i = 0; i < newPlugins.Count; i++)
                    {
                        model.AddPlugin(newPlugins[i]);
                        item.Name = newPlugins[i].Name;
                    }
                    UpdateInterface();
                }
                catch
                {
                    MessageBox.Show("Проблема с файлом конфигурации, удалите его");
                }
            }
        }

        private void UpdateInterface()
        {
            var menuItems = model.UpdateToolRibbonControls();
            SetToolsMenu(menuItems.Tools);
            SetFiltersMenu(menuItems.Filters);
        }
        private void OpenPluginsWindow()
        {
            var dialog = new PluginManagerDialog(model.Plugins);
            if (dialog.ShowDialog() == true)
            {
                UpdateInterface();
            }
            for (int i = 0; i < model.Plugins.Count; i++)
            {
                var plugin = model.Plugins[i];
                if (plugin.PluginPath == "")
                    continue;
                var existingConfig = App.Config.Plugins.FirstOrDefault(cfg => cfg.Name == plugin.Name);

                if (existingConfig == null)
                {
                    var newConfig = new PluginConfig
                    {
                        Name = plugin.Name,
                        IsEnabled = plugin.IsEnabled,
                        Path = plugin.PluginPath
                    };
                    App.Config.Plugins.Add(newConfig);
                }
                else
                {
                    existingConfig.IsEnabled = plugin.IsEnabled;
                    existingConfig.Path = plugin.PluginPath;
                }
            }
            for (int i = 0; i < App.Config.Plugins.Count; i++)
            {
                if (model.Plugins.FirstOrDefault(x => x.Name == App.Config.Plugins[i].Name) == null)
                {
                    App.Config.Plugins.RemoveAt(i);
                }
            }
        }
        private void SetToolsMenu(List<UIElement> toolItems)
        {
            ToolsGroup.Items.Clear();
            foreach (var button in toolItems)
            {
                ToolsGroup.Items.Add(button);
            }
        }
        private void SetFiltersMenu(List<UIElement> filtersItems)
        {
            Filters.Items.Clear();
            foreach (var button in filtersItems)
            {
                Filters.Items.Add(button);
            }
        }

        private void LoadPlugin()
        {
            var newPlugins = PluginService.GetPluginsFromFile();
            if (newPlugins == null) return;
            for (int i = 0; i < newPlugins.Count; i++)
            {
                model.AddPlugin(newPlugins[i]);
            }
            UpdateInterface();
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