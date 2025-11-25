namespace uchat.Services.IServices
{
    public interface IConfigurationService
    {
        Task InitAsync();
        T? Get<T>(string key, T? defaultValue = default);
        void Set<T>(string key, T value);
    }
}
