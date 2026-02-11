using DrawProject.Attributes;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;

public class PropertyEditorControl : StackPanel
{
    public List<PropertyInfo> Properties { get; set; }

    public PropertyEditorControl(List<PropertyInfo> properties)
    {
        // Подписываемся на изменение DataContext
        Properties = properties;
        this.DataContextChanged += OnDataContextChangedHandler;
    }

    private void OnDataContextChangedHandler(object sender, DependencyPropertyChangedEventArgs e)
    {
        Children.Clear();

        if (DataContext == null || Properties == null)
            return;

        foreach (var prop in Properties)
        {
            var attr = prop.GetCustomAttribute<InspectableAttribute>();
            var displayName = attr?.DisplayName ?? prop.Name;

            var type = prop.PropertyType;

            if (type == typeof(bool))
            {
                // Для bool: подпись + CheckBox в одной строке (слева направо)
                var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };

                var label = new Label
                {
                    Content = displayName,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0), // отступ справа от текста
                    FontWeight = FontWeights.Normal,
                    FontSize = 13
                };

                var editor = CreateEditorForProperty(prop, DataContext);
                stackPanel.Children.Add(label);
                stackPanel.Children.Add(editor);
                Children.Add(stackPanel);
            }
            else
            {
                // Для остальных типов: Grid с двумя колонками
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto,
                    MaxWidth = 180 // ← ограничиваем максимальную ширину подписи
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });

                var label = new Label
                {
                    Content = displayName,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Normal,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 10, 0),
                    ToolTip = displayName // на случай обрезки — покажется тултип
                };
                Grid.SetColumn(label, 0);

                var editor = CreateEditorForProperty(prop, DataContext);
                Grid.SetColumn(editor, 1);

                grid.Children.Add(label);
                grid.Children.Add(editor);
                Children.Add(grid);
            }
        }
    }

    private UIElement CreateEditorForProperty(PropertyInfo prop, object instance)
    {
        var type = prop.PropertyType;
        var binding = new Binding(prop.Name)
        {
            Source = instance,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            ValidatesOnExceptions = true,
            NotifyOnValidationError = true
        };

        if (type == typeof(bool))
        {
            // Для bool возвращаем просто CheckBox — подпись будет слева от него
            var checkBox = new CheckBox
            {
                Margin = new Thickness(4, 6, 0, 6),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            checkBox.SetBinding(CheckBox.IsCheckedProperty, binding);
            return checkBox;
        }

        // Общий стиль для TextBox
        var textBox = new TextBox
        {
            Padding = new Thickness(4),
            Margin = new Thickness(0, 2, 0, 2),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Left
        };

        if (type == typeof(int) || type == typeof(double))
        {
            textBox.Width = 80;
            // Можно добавить валидацию позже, если нужно
        }
        else if (type == typeof(string))
        {
            textBox.MinWidth = 100;
            textBox.MaxWidth = 200;
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        else
        {
            // Неподдерживаемый тип
            return new TextBlock
            {
                Text = $"[{type.Name}]",
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 2)
            };
        }

        textBox.SetBinding(TextBox.TextProperty, binding);
        return textBox;
    }
}