using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;
using uchat.Views.Pages;

namespace uchat.ViewModels.VMPages
{
    public class AuthorizationPageViewModel : ViewModelBase
    {
        public AuthorizationPageViewModel(AppState appState, IConfigurationService configurationService,
            IConnectionService connectionService, IDbContextFactory<ApplicationContext> appContextFactory, INavigationService navigationService) : base(appState, configurationService, connectionService, appContextFactory, navigationService)
        {

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
                // Отримуємо токен від Google, або вперше або із кореневого каталогу
                //string token = await ConnectionService.GetGoogleIdTokenAsync();

                // Ініціалізуємо підключення
                await ConnectionService.InitializeConnection(App.ServerBaseUrl!);

                // Кидаємо запит перевірити, чи існує користувач з цього токену у БД хосту
                // У будь-якому випадку далі буде визвано подію ConnectionService_OnAuthorizationConfirmed
                //await ConnectionService.AuthorizeUser(token);
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
