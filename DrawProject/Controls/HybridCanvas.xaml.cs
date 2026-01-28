using DrawProject.Models;
using DrawProject.Models.Instruments;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DrawProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для HybridCanvas.xaml
    /// </summary>
    public partial class HybridCanvas : UserControl
    {
        // === DEPENDENCY PROPERTIES ===
        public static readonly DependencyProperty BrushProperty =
            DependencyProperty.Register("Brush", typeof(Brush), typeof(HybridCanvas),
                new PropertyMetadata(null, OnBrushChanged));

        public static readonly DependencyProperty ToolProperty =
            DependencyProperty.Register("Tool", typeof(Tool), typeof(HybridCanvas),
                new PropertyMetadata(null, OnInstrumentChanged));

        public static readonly DependencyProperty ImageDocumentProperty =
            DependencyProperty.Register("ImageDocument", typeof(ImageDocument),
                typeof(HybridCanvas),
                new PropertyMetadata(null, OnImageDocumentChanged));

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

        public ImageDocument ImageDocument
        {
            get => (ImageDocument)GetValue(ImageDocumentProperty);
            set => SetValue(ImageDocumentProperty, value);
        }
        // === ПОЛЯ ===
        private Image _rasterImage;
        private Canvas _vectorOverlay;
        private bool _isDrawing = false;
        private Point _lastPoint;

        // === КОНСТРУКТОР ===
        public HybridCanvas()
        {
            InitializeComponent();
            InitializeLayers();
        }

        private void InitializeLayers()
        {
            _rasterImage = new Image();
            _vectorOverlay = new Canvas { Background = Brushes.Transparent };

            rootCanvas.Children.Add(_rasterImage);
            rootCanvas.Children.Add(_vectorOverlay);

            this.MouseLeftButtonDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseLeftButtonUp += OnMouseUp;
        }
        public void RefreshRasterImage()
        {
            if (ImageDocument != null)
            {
                // Создаем новый WriteableBitmap из текущего, чтобы WPF заметил изменение
                var current = ImageDocument.GetCompositeImage();
                var newBitmap = new WriteableBitmap(current);
                _rasterImage.Source = newBitmap;

                // Принудительное обновление
                _rasterImage.InvalidateVisual();
                this.InvalidateVisual();
            }
        }

        public Canvas GetVectorOverlay()
        {
            return _vectorOverlay;
        }
        public Image GetRasterImage()
        {
            return _rasterImage;
        }
        private static void OnBrushChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            // Можно обработать изменение кисти
        }
        private static void OnInstrumentChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as HybridCanvas;
            Debug.WriteLine($"Инструмент изменен: {e.NewValue?.GetType().Name}");

            // Можно обработать изменение инструмента
        }
        private static void OnImageDocumentChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var canvas = d as HybridCanvas;

            if (e.NewValue is ImageDocument newDoc)
            {
                // 1. Обновляем растровый слой
                canvas._rasterImage.Source = newDoc.GetCompositeImage();
                canvas._rasterImage.Width = newDoc.Width;
                canvas._rasterImage.Height = newDoc.Height;

                // 2. Обновляем векторный слой
                canvas._vectorOverlay.Width = newDoc.Width;
                canvas._vectorOverlay.Height = newDoc.Height;

                // 3. Обновляем размер всего Canvas
                canvas.Width = newDoc.Width;
                canvas.Height = newDoc.Height;

                // 4. Обновляем rootCanvas
                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.Width = newDoc.Width;
                    canvas.rootCanvas.Height = newDoc.Height;
                }

                // 5. Очищаем старый векторный слой
                canvas._vectorOverlay.Children.Clear();

                Debug.WriteLine($"Document changed: {newDoc.Width}x{newDoc.Height}");
            }
            else if (e.NewValue == null)
            {
                // Если документ удален - очищаем всё
                canvas._rasterImage.Source = null;
                canvas._vectorOverlay.Children.Clear();
            }
        }

        // === МЕТОДЫ РИСОВАНИЯ ===
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Brush == null || ImageDocument?.ActiveLayer == null) return;

            _isDrawing = true;

            InstrumentContext context = new InstrumentContext(this, e, _lastPoint);
            Tool.OnMouseDown(context);

            _lastPoint = e.GetPosition(this);

            CaptureMouse();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDrawing || Brush == null) return;

            InstrumentContext context = new InstrumentContext(this, e, _lastPoint);

            Tool.OnMouseMove(context);

            _lastPoint = context.Position;
        }



        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDrawing || Brush == null) return;

            _isDrawing = false;
            ReleaseMouseCapture();

            InstrumentContext context = new InstrumentContext(this, e, _lastPoint);

            Tool.OnMouseUp(context);
            // Переносим на растровый слой
            CommitDrawing();
        }

        public void CommitDrawing()
        {
            if (_vectorOverlay.Children.Count == 0 ||
                Brush == null ||
                ImageDocument?.ActiveLayer == null)
                return;

            // Рендерим векторный слой в битмап
            var renderTarget = new RenderTargetBitmap(
                (int)_vectorOverlay.ActualWidth,
                (int)_vectorOverlay.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            renderTarget.Render(_vectorOverlay);

            // Применяем к активному слою
            //ImageDocument.ActiveLayer.ApplyVectorLayer(renderTarget, Brush);
            ImageDocument.ApplyVectorLayer(renderTarget);
            // Обновляем отображение
            _rasterImage.Source = ImageDocument.GetCompositeImage();

            // Очищаем векторный слой
            _vectorOverlay.Children.Clear();
        }

        // Метод для очистки холста
        public void ClearCanvas()
        {
            _vectorOverlay.Children.Clear();
            ImageDocument?.Clear();
            _rasterImage.Source = ImageDocument?.GetCompositeImage();
        }
    }
}

