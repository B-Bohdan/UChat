using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.Services.IServices;

namespace uchat.ViewModels.VMPages
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(AppState appState, IConfigurationService configurationService, 
            IConnectionService connectionService, IDbContextFactory<ApplicationContext> appContextFactory) : base(appState, configurationService, connectionService, appContextFactory)
        {

        }
    }
}
