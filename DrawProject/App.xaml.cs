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
                if (pluginFiles.Length == 0)
                {

                }
                else
                {
                    int i = 0;
                    foreach (var item in pluginFiles)
                    {
                        var plugConfig = new PluginConfig();
                        plugConfig.Name = $"Plugin {i++}";
                        plugConfig.Path = item;
                        Config.Plugins.Add(plugConfig);
                    }
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
