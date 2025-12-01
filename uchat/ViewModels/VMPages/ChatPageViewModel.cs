using Microsoft.EntityFrameworkCore;
using uchat.Models;
using uchat.Services.IServices;

namespace uchat.ViewModels.VMPages
{
    public class ChatPageViewModel : ViewModelBase
    {
        public ChatPageViewModel(AppState appState, IConfigurationService configurationService, IConnectionService connectionService, IDbContextFactory<ApplicationContext> dbContextFactory) : base(appState, configurationService, connectionService, dbContextFactory)
        {
        }
    }
}
