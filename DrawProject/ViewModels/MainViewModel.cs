using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using DrawProject.Attributes;
using DrawProject.Controls;
using DrawProject.Models;
using DrawProject.Models.Instruments;
using DrawProject.Services;
using DrawProject.Services.Plugins;
using static DrawProject.Models.BrushShape;

namespace DrawProject.ViewModels
{
    class MainViewModel : ObservableObject
    {
        private Brush _brush = new Brush();
        private ImageDocument _currentDoc;
        private HybridCanvas _drawingCanvas;
        private Tool _activeTool;

        private Point _mousePoint = new Point();
        public Point MousePosition
        {
            get => _mousePoint;
            set
            {
                SetProperty(ref _mousePoint, value);
            }
        }
        public Tool ActiveTool
        {
            get => _activeTool;
            set => SetProperty(ref _activeTool, value);

        }

        public ImageDocument CurrentDoc
        {
            get => _currentDoc;
            set
            {
                SetProperty(ref _currentDoc, value);
                Layers = CurrentDoc.GetLayers;
            }
        }

        public Brush CurrentBrush => _brush;

        // === КОМАНДЫ ===
        public ICommand ClearCommand { get; }
        public ICommand ChangeColorCommand { get; }

        public ICommand SaveCommand { get; }


        public ICommand OpenCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand SaveAsCommand { get; }


        public ICommand MoveLayerDownCommand { get; set; }
        public ICommand SelectPipetteCommand { get; }
        public ICommand MoveLayerUpCommand { get; set; }

        public ICommand AddLayerCommand { get; }

        public ICommand RemoveLayerCommand { get; }

        public ICommand ResizeCommand { get; }


        private CancellationTokenSource _currentFilterCts;

        public ICommand CancelFilterCommand { get; }


        public HybridCanvas DrawingCanvas { get => _drawingCanvas; set => _drawingCanvas = value; }

        private ObservableCollection<Layer> _layers = new();

        public ObservableCollection<Layer> Layers
        {
            get => _layers;
            set
            {
                _layers = value;
                OnPropertyChanged(nameof(Layers));
            }
        }
        private int _selectedLayerIndex = 0;
        public int SelectedLayerIndex
        {
            get => _selectedLayerIndex;
            set
            {
                CurrentDoc.SelectedLayerIndex = value;
                _selectedLayerIndex = CurrentDoc.SelectedLayerIndex;
                OnPropertyChanged(nameof(SelectedLayerIndex));
            }
        }

        public List<PluginController> Plugins { get => _plugins; set => _plugins = value; }

        string _currentPath = "";
        List<PluginController> _plugins = new List<PluginController>();
        public void AddPlugin(PluginController plugin)
        {
            Plugins.Add(plugin);
            var couple = UIGeneratorService.GenerateToolRibbonControls(OnToolSelected, ShowSettingsWindow, plugin.InstrumentTypes);


            plugin.UIInstruments.AddRange(couple.Item1);
            plugin.Tools.AddRange(couple.Item2);

            var coupleFilters = UIGeneratorService.GenerateFiltersRibbonControls((f) => { OnFilterSelected(f); }, ShowSettingsWindow, plugin.FilterTypes);

            plugin.Filters = coupleFilters.Item2;
            plugin.UIFilters = coupleFilters.Item1;

            plugin.IsEnabledChanged += Plugin_IsEnabledChanged;
            plugin.IsUploadChanged += Plugin_IsUploadChanged;
        }

        private void Plugin_IsEnabledChanged(object? sender, EventArgs e)
        {

        }
        private void Plugin_IsUploadChanged(object? sender, EventArgs e)
        {
            var plugin = (PluginController)sender;
            if (!plugin.IsUpload)
            {
                Plugins.Remove(plugin);
            }
        }


        public void ApplyDefaultTool()
        {
            OnToolSelected(_plugins[0].Tools.FirstOrDefault(x => x is BrushTool));
        }
        //Список инструментов
        public void UpdateImage()
        {
            if (CurrentDoc != null)
                CurrentDoc.WasChanged = true;
        }
        // === КОНСТРУКТОР ===
        public MainViewModel()
        {
            ClearCommand = new RelayCommand(ClearCanvas);
            ChangeColorCommand = new RelayCommand<Color>(ChangeColor);


            SaveCommand = new RelayCommand(SaveImage);
            OpenCommand = new RelayCommand(OpenImage);
            SaveAsCommand = new RelayCommand(SaveAs);
            CreateCommand = new RelayCommand(OpenCreateContext);

            AddLayerCommand = new RelayCommand(AddLayer);
            RemoveLayerCommand = new RelayCommand(RemoveLayer);
            MoveLayerUpCommand = new RelayCommand<Layer>(MoveLayerUp);
            MoveLayerDownCommand = new RelayCommand<Layer>(MoveLayerDown);


            ResizeCommand = new RelayCommand(ResizeImage);
            CancelFilterCommand = new RelayCommand(() =>
            {
                _currentFilterCts.Cancel();
            });

            _brush.Color = Colors.Black;
            _brush.Size = 5;
            _brush.Opacity = 1.0f;
            _brush.Hardness = 0.5f;
            _brush.Shape = new CircleBrushShape();
        }
        public (List<UIElement> Tools, List<UIElement> Filters) UpdateToolRibbonControls()
        {
            List<UIElement> tools = new();
            List<UIElement> filters = new();

            for (var i = 0; i < Plugins.Count; i++)
            {
                var plugin = Plugins[i];
                if (!plugin.IsUpload)
                {
                    Plugins.RemoveAt(i);
                    continue;
                }
                else if (plugin.IsEnabled)
                {
                    if (plugin.UIInstruments != null)
                        tools.AddRange(plugin.UIInstruments);
                    if (plugin.UIFilters != null)
                        filters.AddRange(plugin.UIFilters);
                }
            }

            return (tools, filters);
        }
        private bool _isFiltering;
        private bool _canCancel = true;
        public bool CanCancelFiltering
        {
            get => _canCancel;
            set { _canCancel = value; OnPropertyChanged(); OnPropertyChanged("CanCancelFilteringVisibility"); }
        }
        public Visibility CanCancelFilteringVisibility => _canCancel ? Visibility.Visible : Visibility.Collapsed;
        public bool IsFiltering
        {
            get => _isFiltering;
            set { _isFiltering = value; OnPropertyChanged(); OnPropertyChanged("FilterOverlayVisibility"); }
        }
        public Visibility FilterOverlayVisibility => IsFiltering ? Visibility.Visible : Visibility.Collapsed;
        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }
        private void ShowSettingsWindow(Tool tool, List<PropertyInfo> properties)
        {
            var window = new Window
            {
                Title = $"Настройки: {tool.Name}",
                Width = 300,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Content = new PropertyEditorControl(properties) { DataContext = tool }
            };
            window.ShowDialog();
        }
        private void ShowSettingsWindow(Filter tool, List<PropertyInfo> properties)
        {
            var window = new Window
            {
                Title = $"Настройки: {tool.Name}",
                Width = 300,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Content = new PropertyEditorControl(properties) { DataContext = tool }
            };
            window.ShowDialog();
        }
        private void OnToolSelected(Tool tool)
        {
            ActiveTool = tool;
            if (_drawingCanvas != null)
                _drawingCanvas.Cursor = CursorLoader.LoadCursor(tool.CursorPath);
        }

        private async void OnFilterSelected(Filter filter)
        {
            if (CurrentDoc == null) return;

            _currentFilterCts?.Cancel();
            _currentFilterCts = new CancellationTokenSource();

            IsFiltering = true;
            CanCancelFiltering = filter.SupportsCancellation;
            ProgressValue = 0;

            var progress = new Progress<double>(p => ProgressValue = p);

            try
            {
                await CurrentDoc.ApplyFilterAsync(filter, progress, _currentFilterCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Фильтр отменен - можно ничего не делать или показать сообщение
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтра: {ex.Message}");
            }
            finally
            {
                IsFiltering = false;
                ProgressValue = 0;
                _currentFilterCts?.Dispose();
                _currentFilterCts = null;
            }
        }
        // === МЕТОДЫ ===
        private void ClearCanvas()
        {
            if (CurrentDoc == null) return;

            CurrentDoc.ClearActiveLayer();
        }

        private void ChangeColor(Color color)
        {
            _brush.Color = color;
        }

        private void OnColorChanged(Color color)
        {
            _brush.Color = color;
        }

        private void OpenCreateContext()
        {
            var dialog = new CreateImageDialog();
            if (dialog.ShowDialog() == true)
            {
                // Используем размеры
                var width = dialog.ImageWidth;
                var height = dialog.ImageHeight;
                var resolution = dialog.Resolution;

                // Создаем документ
                if (!CreateDocument((int)width, (int)height))
                {
                    MessageBox.Show("Ошибка, не удалось создать новый документ");
                    return;
                }
            }
            UpdateImage();
        }
        private bool CreateDocument(int width, int height)
        {
            if (CurrentDoc != null)
            {
                MessageBoxResult result = MessageBox.Show(
                "У вас есть несохраненные изменения. Сохранить перед открытием нового документа?",
                "Подтверждение выхода",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Сохранить и выйти
                        try
                        {
                            SaveCommand.Execute(null);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                        break;

                    case MessageBoxResult.No:
                        break;

                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            CurrentDoc = new ImageDocument(width, height);
            UpdateImage();
            return true;
        }
        private void OpenImage()
        {

            var source = FileService.OpenFileImage();
            if (source.Item1 == null)
            {
                return;
            }


            _currentPath = source.Item2;
            if (!CreateDocument((int)source.Item1.Width, (int)source.Item1.Height))
            {
                MessageBox.Show("Ошибка, не удалось создать новый документ");
                return;
            }
            CurrentDoc.CreateNewImage(source.Item1);
        }

        private void ResizeImage()
        {
            var dialog = new ImageSizeDialog(CurrentDoc.Width, CurrentDoc.Height);
            if (dialog.ShowDialog() == true)
            {
                // Используем размеры
                var width = dialog.ImageWidth;
                var height = dialog.ImageHeight;
                var resolution = dialog.Resolution;

                // Создаем документ
                CurrentDoc.Resize((int)width, (int)height);
            }
        }


        private void SaveImage()
        {
            if (CurrentDoc == null) return;


            if (_currentPath == "")
            {
                _currentPath = FileService.GetPath();
                if (_currentPath != "")
                {
                    try
                    {
                        FileService.SaveBitmapToFile(CurrentDoc.GetCompositeImage(), _currentPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                FileService.SaveBitmapToFile(CurrentDoc.GetCompositeImage(), _currentPath);
            }
            CurrentDoc.IsUnSaved = false;

        }

        private void SaveAs()
        {
            if (CurrentDoc == null) return;

            var result = FileService.SaveBitmapToPng(CurrentDoc.GetCompositeImage());
            if (result.Item1)
            {
                _currentPath = result.Path;
            }
            CurrentDoc.IsUnSaved = false;
        }



        private void RemoveLayer()
        {
            if (SelectedLayerIndex == -1) return;
            int tmp = SelectedLayerIndex;
            Layers.RemoveAt(SelectedLayerIndex);
            SelectedLayerIndex = Math.Clamp(tmp - 1, 0, _layers.Count - 1);
        }
        private void MoveLayerUp(Layer layer)
        {
            if (layer == null || Layers == null || Layers.Count <= 1) return;

            int currentIndex = Layers.IndexOf(layer);
            if (currentIndex > 0)
            {
                int selectedIndex = SelectedLayerIndex;

                Layers.Move(currentIndex, currentIndex - 1);

                if (selectedIndex == currentIndex)
                {
                    SelectedLayerIndex = currentIndex - 1;
                }
                else if (selectedIndex == currentIndex - 1)
                {
                    SelectedLayerIndex = currentIndex;
                }
            }
        }

        private void MoveLayerDown(Layer layer)
        {
            if (layer == null || Layers == null || Layers.Count <= 1) return;

            int currentIndex = Layers.IndexOf(layer);
            if (currentIndex < Layers.Count - 1)
            {
                int selectedIndex = SelectedLayerIndex;

                Layers.Move(currentIndex, currentIndex + 1);

                if (selectedIndex == currentIndex)
                {
                    SelectedLayerIndex = currentIndex + 1;
                }
                else if (selectedIndex == currentIndex + 1)
                {
                    SelectedLayerIndex = currentIndex;
                }
            }
        }


        private void AddLayer()
        {
            // Создаем новый слой
            if (CurrentDoc == null) return;
            CurrentDoc.AddNewLayer();
            _selectedLayerIndex = CurrentDoc.SelectedLayerIndex;
            OnPropertyChanged(nameof(SelectedLayerIndex));
        }





    }
}
