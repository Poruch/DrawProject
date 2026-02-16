using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawProject.Attributes;
using DrawProject.Controls;
using DrawProject.Models;
using DrawProject.Models.Instruments;
using DrawProject.Services;

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
        public ICommand SelectLayerCommand { get; set; }

        public ICommand AddLayerCommand { get; }

        public ICommand RemoveLayerCommand { get; }

        public ICommand ResizeCommand { get; }


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
            get => _selectedLayerIndex; set
            {
                CurrentDoc.SelectedLayerIndex = value;
                _selectedLayerIndex = CurrentDoc.SelectedLayerIndex;
                OnPropertyChanged(nameof(SelectedLayerIndex));
            }
        }
        string currentPath = "";
        List<Tool> _tools = new List<Tool>();

        public void ApplyDefaultTool()
        {
            OnToolSelected(_tools.FirstOrDefault(x => x is BrushInstrument));
        }
        //Список инструментов
        public void UpdateImage()
        {
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
            _brush.Color = Colors.Black;
            _brush.Size = 5;
            _brush.Opacity = 1.0f;
            _brush.Hardness = 0.5f;
            _brush.Shape = new SquareBrushShape();
        }


        public List<MenuItem> GenerateToolMenuItems()
        {
            var menuItems = new List<MenuItem>();

            // Находим все классы-наследники Tool в текущей сборке
            var toolTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Tool)))
                .ToList();

            foreach (var toolType in toolTypes)
            {
                if (Activator.CreateInstance(toolType) is Tool tool)
                {
                    _tools.Add(tool);
                    var mainItem = new MenuItem { Header = tool.Name };
                    // Пункт настроек (если есть)
                    var inspectableProps = tool.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetCustomAttribute<InspectableAttribute>() != null && p.CanRead && p.CanWrite)
                        .ToList();
                    if (inspectableProps.Any())
                    {
                        // Пункт для выбора инструмента
                        var selectItem = new MenuItem
                        {
                            Header = $"Выбрать: {tool.Name}",
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => OnToolSelected(tool))
                        };

                        mainItem.Items.Add(selectItem);

                        var settingsSubmenu = new MenuItem { Header = "⚙ Настройки..." };
                        settingsSubmenu.Command = new RelayCommand(() => ShowSettingsWindow(tool, inspectableProps));
                        mainItem.Items.Add(settingsSubmenu);
                    }
                    else
                    {
                        mainItem.Command = new RelayCommand(() => OnToolSelected(tool));
                    }
                    menuItems.Add(mainItem);
                }
            }


            return menuItems;
        }




        public List<UIElement> GenerateToolRibbonControls()
        {
            var ribbonControls = new List<UIElement>();

            var toolTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Tool)))
                .ToList();

            foreach (var toolType in toolTypes)
            {
                if (Activator.CreateInstance(toolType) is Tool tool)
                {
                    _tools.Add(tool);

                    var icon = LoadIconFromResource(tool.CursorPath);

                    var inspectableProps = tool.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetCustomAttribute<InspectableAttribute>() != null && p.CanRead && p.CanWrite)
                        .ToList();

                    if (inspectableProps.Any())
                    {
                        var splitButton = new RibbonSplitButton
                        {
                            Label = tool.Name,
                            SmallImageSource = icon,
                            LargeImageSource = icon,
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => OnToolSelected(tool)),
                            IsCheckable = false
                        };

                        var settingsItem = new RibbonMenuItem
                        {
                            Header = "Настройки...",
                            Command = new RelayCommand(() => ShowSettingsWindow(tool, inspectableProps))
                        };
                        var settingsIcon = LoadIconFromResource("SettingsIcon.png");
                        if (settingsIcon != null)
                        {
                            settingsItem.ImageSource = settingsIcon;
                        }
                        splitButton.Items.Add(settingsItem);
                        ribbonControls.Add(splitButton);
                    }
                    else
                    {
                        var button = new RibbonButton
                        {
                            Label = tool.Name,
                            SmallImageSource = icon,
                            LargeImageSource = icon,
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => OnToolSelected(tool)),
                            Focusable = true,
                            IsHitTestVisible = true,
                        };

                        ribbonControls.Add(button);
                    }
                }
            }

            return ribbonControls;
        }

        // Вспомогательный метод для загрузки иконок
        private ImageSource LoadIconFromResource(string resourceName)
        {
            try
            {
                var uri = new Uri(resourceName, UriKind.RelativeOrAbsolute);
                return new BitmapImage(uri);
            }
            catch
            {
                return null;
            }
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

        private void OnToolSelected(Tool tool)
        {
            ActiveTool = tool;
            if (_drawingCanvas != null)
                _drawingCanvas.Cursor = CursorLoader.LoadCursor(tool.CursorPath);
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
                CreateDocument((int)width, (int)height);
            }
            UpdateImage();
        }
        private void CreateDocument(int width, int height)
        {
            CurrentDoc = new ImageDocument(width, height);
            UpdateImage();
        }
        private void OpenImage()
        {
            var source = FileService.OpenFileImage();
            if (source == null)
            {
                return;
            }
            CreateDocument((int)source.Width, (int)source.Height);
            CurrentDoc.CreateNewImage(source);
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


            if (currentPath == "")
            {
                currentPath = FileService.GetPath();
                if (currentPath != "")
                {
                    try
                    {
                        FileService.SaveBitmapToFile(CurrentDoc.GetCompositeImage(), currentPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }
            else
            {
                FileService.SaveBitmapToFile(CurrentDoc.GetCompositeImage(), currentPath);
            }
            CurrentDoc.IsUnSaved = false;

        }

        private void SaveAs()
        {
            if (CurrentDoc == null) return;

            FileService.SaveBitmapToPng(CurrentDoc.GetCompositeImage());
            CurrentDoc.IsUnSaved = false;
        }



        private void RemoveLayer()
        {
            if (SelectedLayerIndex == -1) return;
            int tmp = SelectedLayerIndex;
            Layers.RemoveAt(SelectedLayerIndex);
            SelectedLayerIndex = tmp - 1;
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
