using System;
using System.Collections;
using System.Windows;
using System.Windows.Input;

namespace DrawProject.Controls
{
    /// <summary>
    /// Логика взаимодействия для LayersPanelControl.xaml
    /// </summary>
    public partial class LayersPanelControl : System.Windows.Controls.UserControl
    {
        public LayersPanelControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        /// <summary>
        /// Коллекция слоёв
        /// </summary>
        public IEnumerable Layers
        {
            get { return (IEnumerable)GetValue(LayersProperty); }
            set { SetValue(LayersProperty, value); }
        }
        public static readonly DependencyProperty LayersProperty =
            DependencyProperty.Register(nameof(Layers), typeof(IEnumerable), typeof(LayersPanelControl), new PropertyMetadata(null));

        /// <summary>
        /// Индекс выбранного слоя
        /// </summary>
        public int SelectedLayerIndex
        {
            get { return (int)GetValue(SelectedLayerIndexProperty); }
            set { SetValue(SelectedLayerIndexProperty, value); }
        }
        public static readonly DependencyProperty SelectedLayerIndexProperty =
            DependencyProperty.Register(nameof(SelectedLayerIndex), typeof(int), typeof(LayersPanelControl), new PropertyMetadata(-1));

        /// <summary>
        /// Команда добавления слоя
        /// </summary>
        public ICommand AddLayerCommand
        {
            get { return (ICommand)GetValue(AddLayerCommandProperty); }
            set { SetValue(AddLayerCommandProperty, value); }
        }
        public static readonly DependencyProperty AddLayerCommandProperty =
            DependencyProperty.Register(nameof(AddLayerCommand), typeof(ICommand), typeof(LayersPanelControl), new PropertyMetadata(null));

        /// <summary>
        /// Команда удаления слоя
        /// </summary>
        public ICommand RemoveLayerCommand
        {
            get { return (ICommand)GetValue(RemoveLayerCommandProperty); }
            set { SetValue(RemoveLayerCommandProperty, value); }
        }
        public static readonly DependencyProperty RemoveLayerCommandProperty =
            DependencyProperty.Register(nameof(RemoveLayerCommand), typeof(ICommand), typeof(LayersPanelControl), new PropertyMetadata(null));

        /// <summary>
        /// Команда выбора слоя
        /// </summary>
        public ICommand SelectLayerCommand
        {
            get { return (ICommand)GetValue(SelectLayerCommandProperty); }
            set { SetValue(SelectLayerCommandProperty, value); }
        }
        public static readonly DependencyProperty SelectLayerCommandProperty =
            DependencyProperty.Register(nameof(SelectLayerCommand), typeof(ICommand), typeof(LayersPanelControl), new PropertyMetadata(null));

        /// <summary>
        /// Команда перемещения слоя вверх
        /// </summary>
        public ICommand MoveLayerUpCommand
        {
            get { return (ICommand)GetValue(MoveLayerUpCommandProperty); }
            set { SetValue(MoveLayerUpCommandProperty, value); }
        }
        public static readonly DependencyProperty MoveLayerUpCommandProperty =
            DependencyProperty.Register(nameof(MoveLayerUpCommand), typeof(ICommand), typeof(LayersPanelControl), new PropertyMetadata(null));

        /// <summary>
        /// Команда перемещения слоя вниз
        /// </summary>
        public ICommand MoveLayerDownCommand
        {
            get { return (ICommand)GetValue(MoveLayerDownCommandProperty); }
            set { SetValue(MoveLayerDownCommandProperty, value); }
        }
        public static readonly DependencyProperty MoveLayerDownCommandProperty =
            DependencyProperty.Register(nameof(MoveLayerDownCommand), typeof(ICommand), typeof(LayersPanelControl), new PropertyMetadata(null));

        #endregion
    }
}