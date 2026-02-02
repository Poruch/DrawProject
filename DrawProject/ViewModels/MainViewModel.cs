using System.Collections.ObjectModel;
using System.Reflection.Emit;
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
        public ICommand SelectBrushCommand { get; }
        public ICommand SelectEraserCommand { get; }

        public ICommand SelectRectangleCommand { get; }
        public ICommand ColorWheelChanged { get; }

        public ICommand SaveCommand { get; }


        public ICommand OpenCommand { get; }


        public ICommand MoveLayerDownCommand { get; set; }
        public ICommand SelectPipetteCommand { get; }
        public ICommand MoveLayerUpCommand { get; set; }
        public ICommand SelectLayerCommand { get; set; }

        public ICommand AddLayerCommand { get; }

        public ICommand RemoveLayerCommand { get; }

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
            CurrentDoc.AddNewLayer();
            _selectedLayerIndex = CurrentDoc.SelectedLayerIndex;
            OnPropertyChanged(nameof(SelectedLayerIndex));
        }





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
        BrushInstrument brushInstrument;
        Easter easter;
        RectangleInstrument rectangleInstrument;
        PipetteTool pipetteTool;
        // === КОНСТРУКТОР ===
        public MainViewModel()
        {

            //CurrentDoc = new ImageDocument(1, 1);

            brushInstrument = new BrushInstrument();
            easter = new Easter();
            rectangleInstrument = new RectangleInstrument();
            pipetteTool = new PipetteTool();
            _activeTool = brushInstrument;


            ClearCommand = new RelayCommand(ClearCanvas);
            ChangeColorCommand = new RelayCommand<Color>(ChangeColor);
            ColorWheelChanged = new RelayCommand<Color>(OnColorChanged);


            SelectBrushCommand = new RelayCommand(BrushTool);
            SelectEraserCommand = new RelayCommand(EasterTool);
            SelectRectangleCommand = new RelayCommand(RectangleTool);
            SelectPipetteCommand = new RelayCommand(() => { ActiveTool = pipetteTool; });

            SaveCommand = new RelayCommand(SaveImage);
            OpenCommand = new RelayCommand(OpenImage);

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

        // === МЕТОДЫ ===
        private void ClearCanvas()
        {
            CurrentDoc.ClearActiveLayer();
        }

        private void ChangeColor(Color color)
        {
            BrushColor = color;
        }

        private void EasterTool()
        {
            ActiveTool = easter;
        }
        private void BrushTool()
        {
            ActiveTool = brushInstrument;
        }

        private void RectangleTool()
        {
            ActiveTool = rectangleInstrument;
        }


        private void OnColorChanged(Color color)
        {
            BrushColor = color;
        }
        private void CreateDocument(int width, int height)
        {
            CurrentDoc = new ImageDocument(width, height);
        }
        private void OpenImage()
        {
            var source = SaveLoadService.OpenFileImage();
            if (source == null)
            {
                return;
            }
            CreateDocument((int)source.Width, (int)source.Height);
            CurrentDoc.CreateNewImage(source);
            _drawingCanvas.CommitDrawing();
            //_drawingCanvas.AddNewLayer(source);
        }



        private void SaveImage()
        {
            SaveLoadService.SaveBitmapToPng(CurrentDoc.GetCompositeImage());
        }
    }
}
