using uchat.Services.IServices;

namespace uchat.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(AppState appState, IConfigurationService configurationService, 
            IConnectionService connectionService) : base(appState, configurationService, connectionService)
        {

        }
    }
}
