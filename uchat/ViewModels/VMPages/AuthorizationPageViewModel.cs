using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;

namespace uchat.ViewModels.VMPages
{
    public class AuthorizationPageViewModel : ViewModelBase
    {
        public AuthorizationPageViewModel(AppState appState, IConfigurationService configurationService,
            IConnectionService connectionService, IDbContextFactory<ApplicationContext> appContextFactory) : base(appState, configurationService, connectionService, appContextFactory)
        {
            ConnectionService.OnAuthorizationConfirmed += ConnectionService_OnAuthorizationConfirmed;
        }

        private void ConnectionService_OnAuthorizationConfirmed(modelbase.Models.User obj)
        {
            MessageBox.Show($"{obj.FirstName}, {obj.LastName}");
        }

        #region Commands

        private ICommand? _signInCommand;
        public ICommand SignInCommand
        {
            get
            {
                if (_signInCommand == null)
                    _signInCommand = new RelayCommand(new Action<object>(SignIn));
                return _signInCommand;
            }
        }
        #endregion

        #region Methods

        public async void SignIn(object obj)
        {
            try
            {
                string token = await ConnectionService.GetGoogleIdTokenAsync();

                //...

                await ConnectionService.CheckUserAuthorization(token);
            }
            catch (Exception ex)
            {
                // Если пользователь закрыл браузер или возникла ошибка сети
                MessageBox.Show($"AuthorizationError: {ex.Message}");
            }
        }
        #endregion
    }
}
