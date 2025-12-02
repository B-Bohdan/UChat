using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;
using uchat.Views.Pages;

namespace uchat.ViewModels.VMPages
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(AppState appState, IConfigurationService configurationService, 
            IConnectionService connectionService, IDbContextFactory<ApplicationContext> appContextFactory, INavigationService navigationService) : base(appState, configurationService, connectionService, appContextFactory, navigationService)
        {
            MenuVisibility = Visibility.Collapsed;
        }

        #region Properties

        private Visibility _menuVisibility;
        public Visibility MenuVisibility
        {
            get => _menuVisibility;
            set
            {
                _menuVisibility = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands

        private ICommand? _showMenuCommand;
        public ICommand ShowMenuCommand
        {
            get
            {
                if (_showMenuCommand == null)
                {
                    _showMenuCommand = new RelayCommand(new Action<object>(ShowMenu));
                }
                return _showMenuCommand;
            }
        }

        private ICommand? _hideMenuCommand;
        public ICommand HideMenuCommand
        {
            get
            {
                if (_hideMenuCommand == null)
                {
                    _hideMenuCommand = new RelayCommand(new Action<object>(HideMenu));
                }
                return _hideMenuCommand;
            }
        }
        #endregion

        #region Methods

        public void ShowMenu(object obj)
        {
            MenuVisibility = Visibility.Visible;
        }

        public void HideMenu(object obj)
        {
            MenuVisibility = Visibility.Hidden;
        }
        #endregion
    }
}
