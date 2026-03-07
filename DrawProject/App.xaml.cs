using DrawProject.Models;
using DrawProject.Services;
using System.IO;
using System.Windows;

namespace DrawProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppConfig Config { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Config = ConfigService.Load();

            string pluginsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

            if (Directory.Exists(pluginsFolder))
            {
                var pluginFiles = Directory.GetFiles(pluginsFolder, "*.dll");

                var existingPaths = new HashSet<string>(Config.Plugins.Select(p => p.Path), StringComparer.OrdinalIgnoreCase);

                var existingNames = new HashSet<string>(Config.Plugins.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

                foreach (var filePath in pluginFiles)
                {
                    if (existingPaths.Contains(filePath))
                        continue;

                    string baseName = Path.GetFileNameWithoutExtension(filePath);
                    string uniqueName = baseName;

                    int counter = 1;
                    while (existingNames.Contains(uniqueName))
                    {
                        uniqueName = $"{baseName} ({counter})";
                        counter++;
                    }

                    var pluginConfig = new PluginConfig
                    {
                        Name = uniqueName,
                        Path = filePath
                    };

                    Config.Plugins.Add(pluginConfig);
                    existingPaths.Add(filePath);
                    existingNames.Add(uniqueName);
                }
            }
            else
            {
                // Создать папку, если её нет
                Directory.CreateDirectory(pluginsFolder);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ConfigService.Save(Config);
            base.OnExit(e);
        }
    }

}
