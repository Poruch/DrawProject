using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DrawProject.Models;

namespace DrawProject.Services
{
    public static class ConfigService
    {
        private static readonly string ConfigFileName = "config.json";
        private static readonly string ConfigPath;

        static ConfigService()
        {
            ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
        }

        /// <summary>
        /// Загружает конфигурацию из файла. Если файла нет, возвращает новый экземпляр с настройками по умолчанию.
        /// </summary>
        public static AppConfig Load()
        {
            if (!File.Exists(ConfigPath))
                return new AppConfig();

            try
            {
                string json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json, GetJsonOptions());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
                return new AppConfig();
            }
        }

        /// <summary>
        /// Сохраняет конфигурацию в файл.
        /// </summary>
        public static void Save(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, GetJsonOptions());
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения конфигурации: {ex.Message}");
            }
        }

        private static JsonSerializerOptions GetJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }
    }
}