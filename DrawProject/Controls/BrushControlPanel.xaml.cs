using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DrawProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для BrushControlPanel.xaml
    /// </summary>
    public partial class BrushControlPanel : UserControl
    {
        public BrushControlPanel()
        {
            InitializeComponent();
        }
        #region Dependency Properties

        /// <summary>
        /// Команда изменения цвета
        /// </summary>
        /// 

        // DependencyProperty объявляется как обычно
        public static readonly DependencyProperty BrushColorProperty =
            DependencyProperty.Register(
                "BrushColor",
                typeof(Color),
                typeof(BrushControlPanel),
                new FrameworkPropertyMetadata(
                    Colors.Black,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Color BrushColor
        {
            get => (Color)GetValue(BrushColorProperty);
            set => SetValue(BrushColorProperty, value);
        }



        /// <summary>
        /// Размер кисти
        /// </summary>
        public double BrushSize
        {
            get { return (double)GetValue(BrushSizeProperty); }
            set { SetValue(BrushSizeProperty, value); }
        }
        public static readonly DependencyProperty BrushSizeProperty =
            DependencyProperty.Register(nameof(BrushSize), typeof(double), typeof(BrushControlPanel), new PropertyMetadata(5.0));

        /// <summary>
        /// Активный инструмент
        /// </summary>
        public object ActiveTool
        {
            get { return GetValue(ActiveToolProperty); }
            set { SetValue(ActiveToolProperty, value); }
        }
        public static readonly DependencyProperty ActiveToolProperty =
            DependencyProperty.Register(nameof(ActiveTool), typeof(object), typeof(BrushControlPanel), new PropertyMetadata(null));

        /// <summary>
        /// Позиция мыши
        /// </summary>
        public Point MousePosition
        {
            get { return (Point)GetValue(MousePositionProperty); }
            set { SetValue(MousePositionProperty, value); }
        }
        public static readonly DependencyProperty MousePositionProperty =
            DependencyProperty.Register(nameof(MousePosition), typeof(Point), typeof(BrushControlPanel), new PropertyMetadata(new Point(0, 0)));

        /// <summary>
        /// Текущий документ
        /// </summary>
        public object CurrentDoc
        {
            get { return GetValue(CurrentDocProperty); }
            set { SetValue(CurrentDocProperty, value); }
        }
        public static readonly DependencyProperty CurrentDocProperty =
            DependencyProperty.Register(nameof(CurrentDoc), typeof(object), typeof(BrushControlPanel), new PropertyMetadata(null));

        #endregion
    }
}
