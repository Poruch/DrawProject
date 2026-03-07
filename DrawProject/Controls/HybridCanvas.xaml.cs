using DrawProject.Models;
using DrawProject.Models.Instruments;
using DrawProject.Services;
using DrawProject.Services.Plugins;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static DrawProject.Services.MouseInputService;

namespace DrawProject.Controls
{
    public partial class HybridCanvas : UserControl
    {
        // === DEPENDENCY PROPERTIES ===
        public static readonly DependencyProperty BrushProperty =
        DependencyProperty.Register("Brush", typeof(Brush), typeof(HybridCanvas),
        new PropertyMetadata(null));

        public static readonly DependencyProperty ToolProperty =
        DependencyProperty.Register("Tool", typeof(Tool), typeof(HybridCanvas),
        new PropertyMetadata(null));

        public static readonly DependencyProperty ImageDocumentProperty =
        DependencyProperty.Register("ImageDocument", typeof(ImageDocument),
        typeof(HybridCanvas), new PropertyMetadata(null, OnImageDocumentChanged));

        // === СВОЙСТВА ===
        public Brush Brush
        {
            get => (Brush)GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }


        public Tool Tool
        {
            get => (Tool)GetValue(ToolProperty);
            set => SetValue(ToolProperty, value);
        }

        private bool _useBlend = true;
        public bool UseBlend { get => _useBlend; set => _useBlend = value; }

        public ImageDocument ImageDocument
        {
            get => (ImageDocument)GetValue(ImageDocumentProperty);
            set
            {
                SetValue(ImageDocumentProperty, value);
            }
        }


        // === ПОЛЯ ===
        private bool _isDrawing = false;
        private bool isInitialize = false;
        private int BrushSize = 1;



        public Rect? SelectionBounds { get; private set; }


        // === КОНСТРУКТОР ===
        public HybridCanvas()
        {
            InitializeComponent();
            InitializeLayers();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void InitializeLayers()
        {

            // Получаем элементы из XAML
            var mainGrid = this.Content as Grid;
            if (mainGrid == null) return;

            // Подписка на события мыши на самом UserControl
            this.MouseLeftButtonDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseUp;

            this.Loaded += HybridCanvas_Loaded;
            this.Unloaded += HybridCanvas_Unloaded;


        }


        private void HybridCanvas_Loaded(object sender, RoutedEventArgs e)
        {

        }
        private void HybridCanvas_Unloaded(object sender, RoutedEventArgs e)
        {

        }


        InstrumentContext _lastContext = null;
        Queue<InstrumentContext> _mousePoints = new Queue<InstrumentContext>();
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!_isDrawing && _mousePoints.Count == 0) return;

            // Обрабатываем все накопившиеся контексты
            while (_mousePoints.Count > 0)
            {
                var ctx = _mousePoints.Dequeue();
                Tool?.ApplyTool(ctx);
            }
        }

        private void ProcessingPoints(InstrumentContext context)
        {

        }
        // === ВВОД МЫШИ (UI поток) ===
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing) return;

            var context = new InstrumentContext(this, e, _lastContext == null ? null : _lastContext.Position);
            _lastContext = context;
            _mousePoints.Enqueue(context);
        }
        // === УПРАВЛЕНИЕ СОСТОЯНИЕМ ===
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Brush == null || ImageDocument?.ActiveSource == null) return;

            _isDrawing = true;

            var point = e.GetPosition(this);
            var context = new InstrumentContext(this, e, null);
            Tool?.OnMouseDown(context);
            _mousePoints.Enqueue(context);
            _lastContext = context;
            BrushSize = Brush.Size;
            CaptureMouse();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            _lastContext = null;
            if (!_isDrawing) return;

            _isDrawing = false;
            ReleaseMouseCapture();

            var context = new InstrumentContext(this, e, _lastContext == null ? null : _lastContext.Position);
            Tool?.OnMouseUp(context);


            if (Tool.CommitOnMouseUp)
                CommitDrawing();
        }

        private void OnCanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true; // Запрещаем прокрутку
        }

        public void AddNewLayer(BitmapSource source)
        {
            ImageDocument.CreateNewImage(source);
            CommitDrawing();
        }

        // === КОММИТ И ОЧИСТКА ===
        public void CommitDrawing()
        {
            if (_vectorOverlay.Children.Count == 0 && !ImageDocument.WasChanged)
            {
                ClearOverlay();
                return;
            }

            // Подготовка
            _vectorOverlay.UpdateLayout();
            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);

            // Рендеринг
            var bitmap = new RenderTargetBitmap(
            (int)Math.Max(1, _vectorOverlay.ActualWidth),
            (int)Math.Max(1, _vectorOverlay.ActualHeight),
            96, 96, PixelFormats.Pbgra32);

            bitmap.Render(_vectorOverlay);

            // Применение
            ImageDocument.ApplyVectorLayer(bitmap, Brush.Color.A, !UseBlend, SelectionBounds);
            _rasterImage.Source = ImageDocument.GetCompositeImage();
            ImageDocument.WasChanged = false;
            bitmap.Freeze();
            // Очистка
            ClearOverlay();
        }

        private void ClearOverlay()
        {
            Dispatcher.BeginInvoke(() =>
                          {
                              _vectorOverlay.Children.Clear();
                              UseBlend = true;
                          });
        }


        public void SetSelectionRect(Rect rect)
        {
            SelectionBounds = rect;
            DrawSelectionOverlay();
        }

        public void ClearSelection()
        {
            SelectionBounds = null;
            _selectionOverlay.Children.Clear();
        }

        private void DrawSelectionOverlay()
        {
            _selectionOverlay.Children.Clear();
            if (!SelectionBounds.HasValue) return;

            var rect = SelectionBounds.Value;
            var visualRect = new Rectangle
            {
                Stroke = Brushes.Blue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 255))
            };
            Canvas.SetLeft(visualRect, rect.X);
            Canvas.SetTop(visualRect, rect.Y);
            visualRect.Width = rect.Width;
            visualRect.Height = rect.Height;
            _selectionOverlay.Children.Add(visualRect);
        }


        // === ОСТАЛЬНЫЕ МЕТОДЫ ===
        private static void OnImageDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as HybridCanvas;

            if (e.NewValue is ImageDocument newDoc)
            {
                newDoc.DocumentWasChanged += canvas.CommitDrawing;
                canvas._vectorOverlay.Children.Clear();
            }
            else if (e.NewValue == null)
            {
                canvas._rasterImage.Source = null;
                canvas._vectorOverlay.Children.Clear();
            }
        }


        public Canvas GetVectorOverlay() => _vectorOverlay;
        public Image GetRasterImage() => _rasterImage;

        public void ClearCanvas()
        {
            _vectorOverlay.Children.Clear();
            ImageDocument?.ClearActiveLayer();
            _rasterImage.Source = ImageDocument?.GetCompositeImage();
        }
    }
}