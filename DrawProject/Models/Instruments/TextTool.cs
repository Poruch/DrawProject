using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DrawProject.Models.Instruments
{
    public class TextTool : Tool
    {
        private TextBox _editingTextBox;
        private bool _isPlacingMode = false;
        private Point _placementPoint;
        private Border _editBorder;
        private InstrumentContext _instrumentContext;

        public TextTool()
        {
            Name = "Text";
            ToolTip = "Add text to canvas (click once to place, click again to finish)";
            CommitOnMouseUp = false;
        }

        public override void OnMouseDown(InstrumentContext context)
        {
            if (context?.Canvas == null) return;

            var mousePos = Mouse.GetPosition(context.Canvas);
            _instrumentContext = context;

            if (!_isPlacingMode)
            {
                // Первый клик - начинаем размещение текста
                StartTextPlacement(mousePos);
            }
            else
            {
                // Второй клик - завершаем размещение
                CompleteTextPlacement(mousePos);
            }
        }

        private void StartTextPlacement(Point position)
        {
            // Создаем TextBox для редактирования
            _editingTextBox = new TextBox
            {
                Text = "Text",
                Foreground = new SolidColorBrush(_instrumentContext.Brush.Color),
                FontSize = 16,
                FontFamily = new FontFamily("Arial"),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(2),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                MinWidth = 100,
                MinHeight = 30
            };

            // Сохраняем точку размещения
            _placementPoint = position;

            // Создаем Border для визуального выделения при редактировании
            _editBorder = new Border
            {
                Child = _editingTextBox,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(2)
            };

            // Позиционируем
            Canvas.SetLeft(_editBorder, position.X);
            Canvas.SetTop(_editBorder, position.Y);

            // Добавляем на векторный слой
            _instrumentContext.Canvas.GetVectorOverlay().Children.Add(_editBorder);

            // Переходим в режим размещения
            _isPlacingMode = true;

            // Фокусируем и выделяем текст
            _editingTextBox.Focus();
            _editingTextBox.SelectAll();

            // Подписываемся на события клавиатуры
            _editingTextBox.PreviewKeyDown += OnTextBoxKeyDown;
            _editingTextBox.LostFocus += OnTextBoxLostFocus;
            _editingTextBox.PreviewMouseDown += OnTextBoxPreviewMouseDown;
        }

        private void CompleteTextPlacement(Point position)
        {
            if (_editingTextBox == null || _editBorder == null) return;

            // Если текст пустой, удаляем
            if (string.IsNullOrWhiteSpace(_editingTextBox.Text))
            {
                CancelEditing();
                return;
            }

            // Создаем TextBlock для финального отображения
            var finalTextBlock = new TextBlock
            {
                Text = _editingTextBox.Text,
                Foreground = _editingTextBox.Foreground,
                FontSize = _editingTextBox.FontSize,
                FontFamily = _editingTextBox.FontFamily,
                Background = Brushes.Transparent,
                TextWrapping = TextWrapping.Wrap
            };

            // Получаем позицию
            var borderPos = new Point(
                Canvas.GetLeft(_editBorder),
                Canvas.GetTop(_editBorder));

            // Удаляем Border с канваса
            _instrumentContext.Canvas.GetVectorOverlay().Children.Remove(_editBorder);

            // Добавляем TextBlock без рамок
            Canvas.SetLeft(finalTextBlock, borderPos.X);
            Canvas.SetTop(finalTextBlock, borderPos.Y);
            _instrumentContext.Canvas.GetVectorOverlay().Children.Add(finalTextBlock);

            if (_instrumentContext.Canvas is Controls.HybridCanvas hybridCanvas)
            {
                hybridCanvas.CommitDrawing();
            }

            // Сбрасываем состояние
            Cleanup();
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (_editingTextBox == null) return;

            switch (e.Key)
            {
                case Key.Escape:
                    // Отмена - удаляем текст
                    CancelEditing();
                    e.Handled = true;
                    break;

                case Key.Enter:
                    // Ctrl+Enter - завершение
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        CompleteTextPlacement(_placementPoint);
                        e.Handled = true;
                    }
                    break;

                case Key.Tab:
                    // Tab - завершение редактирования
                    CompleteTextPlacement(_placementPoint);
                    e.Handled = true;
                    break;
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Небольшая задержка чтобы не терять фокус при переключении между элементами TextBox
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                if (_editingTextBox != null && !_editingTextBox.IsFocused && _editBorder?.Parent is Canvas)
                {
                    // Если фокус действительно потерян (не переключился на другой элемент)
                    if (Keyboard.FocusedElement != _editingTextBox)
                    {
                        CompleteTextPlacement(_placementPoint);
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnTextBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Предотвращаем клик по самому TextBox от завершения редактирования
            e.Handled = true;
        }

        private void CompleteCurrentEditing()
        {
            if (_editingTextBox != null && _editBorder?.Parent is Canvas)
            {
                CompleteTextPlacement(_placementPoint);
            }
        }

        private void Cleanup()
        {
            if (_editingTextBox != null)
            {
                _editingTextBox.PreviewKeyDown -= OnTextBoxKeyDown;
                _editingTextBox.LostFocus -= OnTextBoxLostFocus;
                _editingTextBox.PreviewMouseDown -= OnTextBoxPreviewMouseDown;
                _editingTextBox = null;
            }

            _editBorder = null;
            _isPlacingMode = false;
            _instrumentContext = null;
        }

        private void CancelEditing()
        {
            if (_editBorder?.Parent is Canvas canvas)
            {
                canvas.Children.Remove(_editBorder);
                Cleanup();
            }
        }

        public override void OnMouseMove(InstrumentContext context)
        {
            // При движении мыши в режиме размещения можно двигать текст
            if (_isPlacingMode && _editBorder != null && Mouse.LeftButton == MouseButtonState.Pressed)
            {
                var mousePos = Mouse.GetPosition(context.Canvas);
                Canvas.SetLeft(_editBorder, mousePos.X);
                Canvas.SetTop(_editBorder, mousePos.Y);
            }
        }

        public override void OnMouseUp(InstrumentContext context) { }

        public override void OnMouseLeave(InstrumentContext context)
        {
            // При выходе за пределы завершаем редактирование
            if (_isPlacingMode)
            {
                CompleteCurrentEditing();
            }
        }
    }
}