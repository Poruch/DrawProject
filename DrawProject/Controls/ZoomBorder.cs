using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows;

namespace DrawProject.Controls
{
    public class ZoomBorder : Canvas
    {
        private UIElement child = null;
        private Point origin;
        private Point start;

        public ZoomBorder()
        {
            this.Background = Brushes.Transparent;
            this.ClipToBounds = true;

            // Подписываемся на события
            this.MouseWheel += child_MouseWheel;
            this.MouseLeftButtonDown += child_MouseLeftButtonDown;
            this.MouseLeftButtonUp += child_MouseLeftButtonUp;
            this.MouseMove += child_MouseMove;
            this.PreviewMouseRightButtonDown += child_PreviewMouseRightButtonDown;
        }

        // Автоматически инициализируем при загрузке
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            InitializeFirstChild();
        }

        // Или когда добавляются визуальные дочерние элементы
        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            base.OnVisualChildrenChanged(visualAdded, visualRemoved);

            if (child == null && this.Children.Count > 0)
            {
                InitializeFirstChild();
            }
        }

        private void InitializeFirstChild()
        {
            if (this.Children.Count == 0) return;

            // Берем первый дочерний элемент
            child = this.Children[0] as UIElement;

            if (child != null)
            {
                Canvas.SetLeft(child, 0);
                Canvas.SetTop(child, 0);

                // Создаем трансформации если их нет
                if (child.RenderTransform is not TransformGroup)
                {
                    TransformGroup group = new TransformGroup();
                    group.Children.Add(new ScaleTransform());
                    group.Children.Add(new TranslateTransform());
                    child.RenderTransform = group;
                    child.RenderTransformOrigin = new Point(0.0, 0.0);
                }
            }
        }

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            if (element?.RenderTransform is TransformGroup group)
            {
                return group.Children.OfType<TranslateTransform>().FirstOrDefault();
            }
            return null;
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            if (element?.RenderTransform is TransformGroup group)
            {
                return group.Children.OfType<ScaleTransform>().FirstOrDefault();
            }
            return null;
        }

        public void Reset()
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                if (st != null)
                {
                    st.ScaleX = 1.0;
                    st.ScaleY = 1.0;
                }

                var tt = GetTranslateTransform(child);
                if (tt != null)
                {
                    tt.X = 0.0;
                    tt.Y = 0.0;
                }
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                const double ZOOM_FACTOR = 1.2; // 20% увеличение
                const double MIN_SCALE = 0.01;  // 1%
                const double MAX_SCALE = 50.0;  // 5000%

                double zoom = e.Delta > 0 ? ZOOM_FACTOR : 1.0 / ZOOM_FACTOR;
                double newScaleX = st.ScaleX * zoom;
                double newScaleY = st.ScaleY * zoom;

                // Ограничиваем масштаб
                newScaleX = Math.Max(MIN_SCALE, Math.Min(MAX_SCALE, newScaleX));
                newScaleY = Math.Max(MIN_SCALE, Math.Min(MAX_SCALE, newScaleY));

                if (Math.Abs(newScaleX - st.ScaleX) < 0.001 &&
                    Math.Abs(newScaleY - st.ScaleY) < 0.001)
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX = relative.X * st.ScaleX + tt.X;
                double absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX = newScaleX;
                st.ScaleY = newScaleY;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //if (child != null)
            //{
            //    var tt = GetTranslateTransform(child);
            //    start = e.GetPosition(this);
            //    origin = new Point(tt.X, tt.Y);
            //    this.Cursor = Cursors.Hand;
            //    child.CaptureMouse();
            //}
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //if (child != null)
            //{
            //    child.ReleaseMouseCapture();
            //    this.Cursor = Cursors.Arrow;
            //}
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //this.Reset();
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            //if (child != null)
            //{
            //    if (child.IsMouseCaptured)
            //    {
            //        var tt = GetTranslateTransform(child);
            //        Vector v = start - e.GetPosition(this);
            //        tt.X = origin.X - v.X;
            //        tt.Y = origin.Y - v.Y;
            //    }
            //}
        }

        #endregion
    }
}