using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DrawProject.Models
{
    public class AppConfig
    {

        public string LastOpenedFile { get; set; } = string.Empty;

        // Настройки плагинов (список)
        public List<PluginConfig> Plugins { get; set; } = new List<PluginConfig>();

    }

    public class PluginConfig
    {
        public string Name { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string Path { get; set; }

    }
}