using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
            set => SetProperty(ref _currentDoc, value);
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
        public ICommand AddLayerCommand { get; }
        public ICommand ResetLayerCommand { get; }
        public ICommand UpLayerCommand { get; }
        public ICommand SelectPipetteCommand { get; }
        public HybridCanvas DrawingCanvas { get => _drawingCanvas; set => _drawingCanvas = value; }



        //Список инструментов 
        BrushInstrument brushInstrument;
        Easter easter;
        RectangleInstrument rectangleInstrument;
        PipetteTool pipetteTool;
        // === КОНСТРУКТОР ===
        public MainViewModel()
        {
            CurrentDoc = new ImageDocument(2000, 1500);

            ClearCommand = new RelayCommand(ClearCanvas);
            ChangeColorCommand = new RelayCommand<Color>(ChangeColor);
            ColorWheelChanged = new RelayCommand<Color>(OnColorChanged);


            SelectBrushCommand = new RelayCommand(BrushTool);
            SelectEraserCommand = new RelayCommand(EasterTool);
            SelectRectangleCommand = new RelayCommand(RectangleTool);
            SelectPipetteCommand = new RelayCommand(() => { ActiveTool = pipetteTool; });

            SaveCommand = new RelayCommand(SaveImage);
            OpenCommand = new RelayCommand(OpenImage);

            AddLayerCommand = new RelayCommand(() => { CurrentDoc.AddNewLayer(); });
            ResetLayerCommand = new RelayCommand(() => { CurrentDoc.SelectedLayerIndex = 0; });
            UpLayerCommand = new RelayCommand(() => { CurrentDoc.SelectedLayerIndex++; });

            brushInstrument = new BrushInstrument();
            easter = new Easter();
            rectangleInstrument = new RectangleInstrument();
            pipetteTool = new PipetteTool();
            _activeTool = brushInstrument;

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

        private void OpenImage()
        {
            var source = SaveLoadService.OpenFileImage();
            if (source == null)
            {
                return;
            }
            CurrentDoc = new ImageDocument((int)source.Width, (int)source.Height);
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
