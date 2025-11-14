using uchat.Services;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;

namespace uchat.ViewModels
{
    /// <summary>
    /// Базовий клас для всіх ViewModel у застосунку. Підтягує всі сервіси з DI контейнера, 
    /// та фіксує посилання на них у відповідні властивості.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        public AppState ApplicationState { get; private set; }
        public IConfigurationService ConfigurationService { get; private set; }
        public IConnectionService ConnectionService { get; private set; }

        public ViewModelBase(AppState appState, IConfigurationService configurationService, 
            IConnectionService connectionService)
        {
            ApplicationState = appState;
            ConfigurationService = configurationService;
            ConnectionService = connectionService;
        }
    }
}
