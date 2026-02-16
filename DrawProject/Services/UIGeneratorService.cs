using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DrawProject.Attributes;
using DrawProject.Controls;
using DrawProject.Models;
using DrawProject.Models.Instruments;
using DrawProject.Services;

namespace DrawProject.Services
{
    internal static class UIGeneratorService
    {
        public static (List<MenuItem>, List<Tool>) GenerateToolMenuItems(Action<Tool> function1, Action<Tool, List<PropertyInfo>> function2)
        {
            var menuItems = new List<MenuItem>();
            var tools = new List<Tool>();
            // Находим все классы-наследники Tool в текущей сборке
            var toolTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Tool)))
                .ToList();

            foreach (var toolType in toolTypes)
            {
                if (Activator.CreateInstance(toolType) is Tool tool)
                {
                    tools.Add(tool);
                    var mainItem = new MenuItem { Header = tool.Name };
                    // Пункт настроек (если есть)
                    var inspectableProps = tool.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetCustomAttribute<InspectableAttribute>() != null && p.CanRead && p.CanWrite)
                        .ToList();
                    if (inspectableProps.Any())
                    {
                        // Пункт для выбора инструмента
                        var selectItem = new MenuItem
                        {
                            Header = $"Chose: {tool.Name}",
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => function1(tool))
                        };

                        mainItem.Items.Add(selectItem);

                        var settingsSubmenu = new MenuItem { Header = "⚙ Settings..." };
                        settingsSubmenu.Command = new RelayCommand(() => function2(tool, inspectableProps));
                        mainItem.Items.Add(settingsSubmenu);
                    }
                    else
                    {
                        mainItem.Command = new RelayCommand(() => function1(tool));
                    }
                    menuItems.Add(mainItem);
                }
            }

            return (menuItems, tools);
        }
        public static (List<UIElement>, List<Tool>) GenerateToolRibbonControls(Action<Tool> function1, Action<Tool, List<PropertyInfo>> function2)
        {
            var ribbonControls = new List<UIElement>();
            var tools = new List<Tool>();


            var toolTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Tool)))
                .ToList();

            foreach (var toolType in toolTypes)
            {
                if (Activator.CreateInstance(toolType) is Tool tool)
                {
                    tools.Add(tool);

                    var icon = FileService.LoadIconFromResource(tool.CursorPath);

                    var inspectableProps = tool.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetCustomAttribute<InspectableAttribute>() != null && p.CanRead && p.CanWrite)
                        .ToList();

                    if (inspectableProps.Any())
                    {
                        var splitButton = new RibbonSplitButton
                        {
                            Label = tool.Name,
                            SmallImageSource = icon,
                            LargeImageSource = icon,
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => function1(tool)),
                            IsCheckable = false
                        };

                        var settingsItem = new RibbonMenuItem
                        {
                            Header = "Settings...",
                            Command = new RelayCommand(() => function2(tool, inspectableProps))
                        };
                        var settingsIcon = FileService.LoadIconFromResource("SettingsIcon.png");
                        if (settingsIcon != null)
                        {
                            settingsItem.ImageSource = settingsIcon;
                        }
                        splitButton.Items.Add(settingsItem);
                        ribbonControls.Add(splitButton);
                    }
                    else
                    {
                        var button = new RibbonButton
                        {
                            Label = tool.Name,
                            SmallImageSource = icon,
                            LargeImageSource = icon,
                            ToolTip = tool.ToolTip,
                            Command = new RelayCommand(() => function1(tool)),
                            Focusable = true,
                            IsHitTestVisible = true,
                        };

                        ribbonControls.Add(button);
                    }
                }
            }

            return (ribbonControls, tools);
        }



    }
}
