using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Windows;
using Microsoft.Win32;

namespace DrawProject.Services.Plugins
{
    static internal class PluginService
    {
        /// <summary>
        /// Загружает сборку плагина в новый выгружаемый контекст.
        /// </summary>
        /// <returns>Кортеж (сборка, контекст) или (null, null) при ошибке/отмене.</returns>
        private static (Assembly Assembly, AssemblyLoadContext Context) LoadPluginAssembly(string path = "")
        {
            string filePath = path;
            if (path == "")
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "DLL файлы (*.dll)|*.dll",
                    Title = "Выберите сборку с плагином"
                };

                if (openFileDialog.ShowDialog() != true)
                    return (null, null);

                filePath = openFileDialog.FileName;
            }
            try
            {
                // Создаём выгружаемый контекст. Имя может быть любым, но лучше взять имя файла.
                var context = new AssemblyLoadContext(
                    name: Path.GetFileNameWithoutExtension(filePath),
                    isCollectible: true);

                Assembly assembly = context.LoadFromAssemblyPath(filePath);
                return (assembly, context);
            }
            catch (BadImageFormatException)
            {
                MessageBox.Show("Выбранный файл не является допустимой .NET сборкой.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show($"Не удалось найти файл или его зависимости: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки плагина: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return (null, null);
        }

        /// <summary>
        /// Загружает все реализации IPlugin из выбранной DLL.
        /// </summary>
        public static List<PluginController> GetPluginsFromFile(string path = "")
        {
            var (assembly, context) = LoadPluginAssembly(path);
            if (assembly == null) return new List<PluginController>();
            try
            {
                var pluginTypes = assembly.GetTypes()
                    .Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                    .ToList();

                return pluginTypes
                    .Select(t => new PluginController(Activator.CreateInstance(t) as IPlugin, context))
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки плагина: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                context.Unload();
                return new List<PluginController>();
            }
        }

        /// <summary>
        /// Загружает первую реализацию IPlugin из выбранной DLL.
        /// </summary>
        public static PluginController GetPluginFromFile(string path = "")
        {
            var (assembly, context) = LoadPluginAssembly(path);
            if (assembly == null) return null;

            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            if (pluginType == null) return null;

            return new PluginController(Activator.CreateInstance(pluginType) as IPlugin, context);
        }
    }
}