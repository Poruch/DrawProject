using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DrawProject.Controls;
using DrawProject.Instruments;
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

        // === НАСТРОЙКИ КИСТИ ===
        public Color BrushColor
        {
            get => _brush.Color;
            set
            {
                if (_brush.Color != value)
                {
                    _brush.Color = value;
                    OnPropertyChanged(nameof(BrushColor));
                }
            }
        }

        public int BrushSize
        {
            get => _brush.Size;
            set
            {
                if (_brush.Size != value)
                {
                    _brush.Size = value;
                    OnPropertyChanged(nameof(BrushSize));
                }
            }
        }

        public float BrushOpacity
        {
            get => _brush.Opacity;
            set
            {
                if (_brush.Opacity != value)
                {
                    _brush.Opacity = value;
                    OnPropertyChanged(nameof(BrushOpacity));
                }
            }
        }

        public float BrushHardness
        {
            get => _brush.Hardness;
            set
            {
                if (_brush.Hardness != value)
                {
                    _brush.Hardness = value;
                    OnPropertyChanged(nameof(BrushHardness));
                }
            }
        }

        public BrushShape BrushShape
        {
            get => _brush.Shape;
            set
            {
                if (_brush.Shape != value)
                {
                    _brush.Shape = value;
                    OnPropertyChanged(nameof(BrushShape));
                }
            }
        }

        public Brush CurrentBrush => _brush;

        // === КОМАНДЫ ===
        public ICommand ClearCommand { get; }
        public ICommand ChangeColorCommand { get; }
        public ICommand ColorWheelChanged { get; }

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
        //Список инструментов

        // === КОНСТРУКТОР ===
        public MainViewModel()
        {
            ClearCommand = new RelayCommand(ClearCanvas);
            ChangeColorCommand = new RelayCommand<Color>(ChangeColor);
            ColorWheelChanged = new RelayCommand<Color>(OnColorChanged);


            SaveCommand = new RelayCommand(SaveImage);
            OpenCommand = new RelayCommand(OpenImage);
            SaveAsCommand = new RelayCommand(SaveAs);
            CreateCommand = new RelayCommand(OpenCreateContext);

            AddLayerCommand = new RelayCommand(AddLayer);
            RemoveLayerCommand = new RelayCommand(RemoveLayer);
            MoveLayerUpCommand = new RelayCommand<Layer>(MoveLayerUp);
            MoveLayerDownCommand = new RelayCommand<Layer>(MoveLayerDown);


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
                // Создаем экземпляр инструмента
                if (Activator.CreateInstance(toolType) is Tool tool)
                {
                    // Создаем MenuItem

                    var menuItem = new MenuItem
                    {
                        Header = tool.Name,
                        ToolTip = tool.ToolTip,
                        Tag = tool // Сохраняем инструмент в Tag
                    };

                    menuItem.Command = new RelayCommand(() => { OnToolselected(menuItem.Tag as Tool); });
                    menuItems.Add(menuItem);
                }
            }

            return menuItems;
        }

        private void OnToolselected(Tool tool)
        {
            ActiveTool = tool;
        }
        // === МЕТОДЫ ===
        private void ClearCanvas()
        {
            CurrentDoc.ClearActiveLayer();
        }

        private void ChangeColor(Color color)
        {
            BrushColor = color;
        }

        private void OnColorChanged(Color color)
        {
            BrushColor = color;
        }

        private void OpenCreateContext()
        {
            CreateDocument(200, 200);
        }
        private void CreateDocument(int width, int height)
        {
            CurrentDoc = new ImageDocument(width, height);
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
            _drawingCanvas.CommitDrawing();
            CurrentDoc.WasChanged = true;
            DrawingCanvas.CommitDrawing();
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
            CurrentDoc.WasChanged = true;
            DrawingCanvas.CommitDrawing();
        }
        private void MoveLayerUp(Layer layer)
        {
            if (layer == null || Layers == null || Layers.Count <= 1) return;

            int currentIndex = Layers.IndexOf(layer);
            if (currentIndex > 0) // Не первый ли слой?
            {
                // Сохраняем выбранный индекс
                int selectedIndex = SelectedLayerIndex;

                // Меняем местами с предыдущим слоем
                Layers.Move(currentIndex, currentIndex - 1);

                // Корректируем выбранный индекс
                if (selectedIndex == currentIndex)
                {
                    SelectedLayerIndex = currentIndex - 1; // Выбранный слой переместился вверх
                }
                else if (selectedIndex == currentIndex - 1)
                {
                    SelectedLayerIndex = currentIndex; // Соседний слой переместился вниз
                }
                CurrentDoc.WasChanged = true;
                DrawingCanvas.CommitDrawing();
            }
        }

        private void MoveLayerDown(Layer layer)
        {
            if (layer == null || Layers == null || Layers.Count <= 1) return;

            int currentIndex = Layers.IndexOf(layer);
            if (currentIndex < Layers.Count - 1) // Не последний ли слой?
            {
                // Сохраняем выбранный индекс
                int selectedIndex = SelectedLayerIndex;

                // Меняем местами со следующим слоем
                Layers.Move(currentIndex, currentIndex + 1);

                // Корректируем выбранный индекс
                if (selectedIndex == currentIndex)
                {
                    SelectedLayerIndex = currentIndex + 1; // Выбранный слой переместился вниз
                }
                else if (selectedIndex == currentIndex + 1)
                {
                    SelectedLayerIndex = currentIndex; // Соседний слой переместился вверх
                }
                CurrentDoc.WasChanged = true;
                DrawingCanvas.CommitDrawing();
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
