using System.IO;
using System.Text.Json;
using uchat.Services.IServices;

namespace uchat.Services
{
    /// <summary>
    /// Потрібний для керування конфігурацією додатку.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private const string ConfigFileName = "appSettings.json";
        private const string DirectoryName = "UChat";
        private Dictionary<string, string> _settings = new();
        private readonly string _localFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        private bool _isInitialized = false;

        public async Task InitAsync()
        {
            if (FileExists(ConfigFileName))
            {
                await LoadConfigurationAsync();
            }
            else
            {
                _settings = new Dictionary<string, string>
                {
                    { "UILanguage", "UA" },
                    { "StartPageTag", "" },
                    { "ServerConnectionString", "" },
                    { "IsAuthorized", "false"}
                };
                await SaveConfigurationAsync();
            }

            _isInitialized = true;
        }

        public T? Get<T>(string key, T? defaultValue = default)
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("ConfigurationService is not initialized.");
            }

            try
            {
                if (_settings.TryGetValue(key, out var value))
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                return defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public async void Set<T>(string key, T value)
        {
            if(!_isInitialized)
            {
                throw new InvalidOperationException("ConfigurationService is not initialized.");
            }

            _settings[key] = value?.ToString() ?? "";
            await SaveConfigurationAsync();
        }

        private async Task LoadConfigurationAsync()
        {
            try
            {
                string json = await File.ReadAllTextAsync(@$"{_localFolder}\{DirectoryName}\{ConfigFileName}");

                _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                            ?? new Dictionary<string, string>();
            }
            catch
            {
                _settings = new Dictionary<string, string>();
            }
        }

        private async Task SaveConfigurationAsync()
        {
            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if(!File.Exists(@$"{_localFolder}\{DirectoryName}"))
            {
                Directory.CreateDirectory(@$"{_localFolder}\{DirectoryName}");
            }

            await File.WriteAllTextAsync(@$"{_localFolder}\{DirectoryName}\{ConfigFileName}", json);
        }

        private bool FileExists(string fileName)
        {
            if (File.Exists(@$"{_localFolder}\{DirectoryName}\{fileName}"))
            {
                return true;
            }
            return false;
        }
    }
}
