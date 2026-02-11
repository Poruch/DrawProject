using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DrawProject.Attributes;
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

                    // Пункт для выбора инструмента
                    var selectItem = new MenuItem
                    {
                        Header = $"Выбрать: {tool.Name}",
                        ToolTip = tool.ToolTip,
                        Command = new RelayCommand(() => OnToolSelected(tool))
                    };

                    // Пункт настроек (если есть)
                    var inspectableProps = tool.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetCustomAttribute<InspectableAttribute>() != null && p.CanRead && p.CanWrite)
                        .ToList();

                    mainItem.Items.Add(selectItem);

                    if (inspectableProps.Any())
                    {
                        var settingsSubmenu = new MenuItem { Header = "⚙ Настройки..." };
                        settingsSubmenu.Command = new RelayCommand(() => ShowSettingsWindow(tool, inspectableProps));
                        mainItem.Items.Add(settingsSubmenu);
                    }

                    menuItems.Add(mainItem);
                }
            }


            return menuItems;
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
                _drawingCanvas.Cursor = LoadCursor(tool.CursorPath);
        }
        public static Cursor LoadCursor(string pngResourcePath, int size = 32, Point hotSpot = default)
        {
            try
            {
                // 1. Загрузить PNG как BitmapSource
                var uri = new Uri(pngResourcePath, UriKind.Absolute);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // 2. Масштабировать до нужного размера
                var scaled = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawImage(bitmap, new Rect(0, 0, size, size));
                }
                scaled.Render(dv);
                scaled.Freeze();

                // 3. Конвертировать в .cur в памяти
                var cursorData = ConvertToCursor(scaled, (int)hotSpot.X, (int)hotSpot.Y);

                // 4. Создать курсор
                return new Cursor(cursorData);
            }
            catch
            {
                return Cursors.Arrow;
            }
        }
        private static MemoryStream ConvertToCursor(BitmapSource bitmap, int hotspotX, int hotspotY)
        {
            var stream = new MemoryStream();

            // ICO/CUR header
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                // ICONDIR
                writer.Write((ushort)0); // reserved
                writer.Write((ushort)2); // 2 = cursor
                writer.Write((ushort)1); // count

                // ICONDIRENTRY
                writer.Write((byte)bitmap.PixelWidth);   // width
                writer.Write((byte)bitmap.PixelHeight);  // height
                writer.Write((byte)0);                   // color count (0 = 256+)
                writer.Write((byte)0);                   // reserved
                writer.Write((ushort)hotspotX);          // hotspot x
                writer.Write((ushort)hotspotY);          // hotspot y
                writer.Write((uint)0);                   // bytes in image (will update later)
                writer.Write((uint)22);                  // offset to image data

                long imageDataStart = stream.Position;

                // BITMAPINFOHEADER
                writer.Write((uint)40);                  // size
                writer.Write((int)bitmap.PixelWidth);    // width
                writer.Write((int)(bitmap.PixelHeight * 2)); // height * 2 (AND + XOR)
                writer.Write((ushort)1);                 // planes
                writer.Write((ushort)32);                // bit count
                writer.Write((uint)0);                   // compression
                writer.Write((uint)0);                   // image size
                writer.Write((int)0);                    // x pixels per meter
                writer.Write((int)0);                    // y pixels per meter
                writer.Write((uint)0);                   // colors used
                writer.Write((uint)0);                   // colors important

                // Pixels (BGRA)
                int stride = bitmap.PixelWidth * 4;
                byte[] pixelData = new byte[stride * bitmap.PixelHeight];
                bitmap.CopyPixels(pixelData, stride, 0);

                // 🔁 Flip vertically for ICO/CUR format
                byte[] transformedPixelData = new byte[pixelData.Length];
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        // Исходный пиксель
                        int srcIndex = y * stride + x * 4;

                        // Целевой пиксель: (зеркальный X, перевёрнутый Y)
                        int dstX = bitmap.PixelWidth - 1 - x;
                        int dstY = bitmap.PixelHeight - 1 - y;
                        int dstIndex = dstY * stride + dstX * 4;

                        // Копируем 4 байта (BGRA)
                        transformedPixelData[dstIndex + 0] = pixelData[srcIndex + 0]; // B
                        transformedPixelData[dstIndex + 1] = pixelData[srcIndex + 1]; // G
                        transformedPixelData[dstIndex + 2] = pixelData[srcIndex + 2]; // R
                        transformedPixelData[dstIndex + 3] = pixelData[srcIndex + 3]; // A
                    }
                }

                // Write XOR mask (image)
                writer.Write(transformedPixelData);

                // Write AND mask (transparent)
                int andMaskSize = ((bitmap.PixelWidth + 31) / 32) * 4 * bitmap.PixelHeight; // correct size for 1bpp
                byte[] andMask = new byte[andMaskSize];
                Array.Fill(andMask, (byte)0xFF); // fully transparent
                writer.Write(andMask);

                // Update image size in ICONDIRENTRY
                long imageDataEnd = stream.Position;
                long imageSize = imageDataEnd - imageDataStart;
                stream.Position = 14; // position of "bytes in image"
                writer.Write((uint)imageSize);

                stream.Position = 0;
                return stream;
            }
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
                DrawingCanvas.CommitDrawing();
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
            CurrentDoc.WasChanged = true;
            DrawingCanvas.CommitDrawing();
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
                CurrentDoc.WasChanged = true;
                DrawingCanvas.CommitDrawing();
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
