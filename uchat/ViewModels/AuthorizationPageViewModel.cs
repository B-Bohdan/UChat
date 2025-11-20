using uchat.Services.IServices;

namespace uchat.ViewModels
{
    public class AuthorizationPageViewModel : ViewModelBase
    {
        public AuthorizationPageViewModel(AppState appState, IConfigurationService configurationService, IConnectionService connectionService) : base(appState, configurationService, connectionService)
        {

        }
    }
}
