using System.IO;
using System.IO.IsolatedStorage;
using System.Text.Json;
using System.Windows;
using uchat.Services.IServices;

namespace uchat.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private const string ConfigFileName = "appSettings.json";
        private Dictionary<string, string> _settings = new();
        private bool _isInitialized = false;

        // Семафор ограничивает доступ к файлу. (1, 1) означает "только 1 поток за раз".
        private readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

        public async Task InitAsync()
        {
            // Ждем очереди, чтобы прочитать файл (если вдруг кто-то пишет прямо сейчас)
            await _fileLock.WaitAsync();
            try
            {
                using var storage = IsolatedStorageFile.GetUserStoreForAssembly();
                if (storage.FileExists(ConfigFileName))
                {
                    // Логику загрузки вынес сюда, чтобы не дублировать блокировки
                    using var stream = storage.OpenFile(ConfigFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    _settings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream)
                                ?? new Dictionary<string, string>();
                }
                else
                {
                    _settings = new Dictionary<string, string>
                    {
                        { "ServerConnectionString", "http://127.0.0.1:8000/chatHub" },
                        { "IsAuthorized", "false"},
                        { "AuthorizedUserId", "" }
                    };

                    // Сохранение вызываем внутри try, но так как мы УЖЕ внутри блокировки _fileLock,
                    // нам нужен отдельный метод сохранения БЕЗ блокировки, либо освободить и вызвать публичный.
                    // Проще сохранить прямо тут:
                    using var stream = storage.OpenFile(ConfigFileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await JsonSerializer.SerializeAsync(stream, _settings, new JsonSerializerOptions { WriteIndented = true });
                }
            }
            catch
            {
                _settings = new Dictionary<string, string>();
            }
            finally
            {
                // Всегда освобождаем семафор, даже если была ошибка
                _fileLock.Release();
            }

            _isInitialized = true;
        }

        public T? Get<T>(string key, T? defaultValue = default)
        {
            if (!_isInitialized) throw new InvalidOperationException("ConfigurationService is not initialized.");

            // Get можно не блокировать, так как мы читаем из памяти (_settings), а не с диска
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
            if (!_isInitialized) throw new InvalidOperationException("ConfigurationService is not initialized.");

            _settings[key] = value?.ToString() ?? "";

            // Вызываем сохранение
            await SaveConfigurationAsync();
        }

        private async Task SaveConfigurationAsync()
        {
            // Входим в "опасную зону". Если другой поток уже тут, мы ждем его завершения.
            await _fileLock.WaitAsync();
            try
            {
                using var storage = IsolatedStorageFile.GetUserStoreForAssembly();
                using var stream = storage.OpenFile(ConfigFileName, FileMode.Create, FileAccess.Write, FileShare.Read);

                await JsonSerializer.SerializeAsync(stream, _settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception e)
            {
                // В void методе (каким является Set через await) исключения могут уронить приложение,
                // поэтому логируем или показываем, но не даем приложению упасть.
                System.Diagnostics.Debug.WriteLine($"Error saving config: {e.Message}");
            }
            finally
            {
                // Освобождаем место для следующих сохранений
                _fileLock.Release();
            }
        }
    }
}