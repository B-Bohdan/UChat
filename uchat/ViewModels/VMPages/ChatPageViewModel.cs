using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;
using uchat.Views.Dialogs;

namespace uchat.ViewModels.VMPages
{
    public class ChatPageViewModel : ViewModelBase
    {
        private readonly MainPageViewModel _mainPageViewModel;

        public ChatPageViewModel(AppState appState, IConfigurationService configurationService, IConnectionService connectionService, 
            IDbContextFactory<ApplicationContext> dbContextFactory, INavigationService navigationService, MainPageViewModel mainPageViewModel) : base(appState, configurationService, connectionService, dbContextFactory, navigationService)
        {
            _mainPageViewModel = mainPageViewModel;
            FoundedUsersToAdd = new ObservableCollection<UserViewModel>();
            ParticipantsOfSelectedChat = new ObservableCollection<UserViewModel>();
        }

        #region

        public ObservableCollection<UserViewModel> FoundedUsersToAdd { get; private set; }

        public ObservableCollection<UserViewModel> ParticipantsOfSelectedChat { get; private set; }

        private string? _enteredUserEmail;
        public string? EnteredUserEmail
        {
            get => _enteredUserEmail;
            set { _enteredUserEmail = value; OnPropertyChanged(); }
        }
        #endregion

        #region Commands

        private ICommand? _openChatMenuCommand;
        public ICommand OpenChatMenuCommand
        {
            get
            {
                if(_openChatMenuCommand == null)
                {
                    _openChatMenuCommand = new RelayCommand(new Action<object>(OpenChatMenu));
                }

                return _openChatMenuCommand;
            }
        }

        private ICommand? _hideChatMenuPanelCommand;
        public ICommand HideChatMenuPanelCommand
        {
            get
            {
                if (_hideChatMenuPanelCommand == null)
                {
                    _hideChatMenuPanelCommand = new RelayCommand(new Action<object>(HideChatMenu));
                }

                return _hideChatMenuPanelCommand;
            }
        }

        private ICommand? _openSearchInvitePanelCommand;
        public ICommand OpenSearchInvitePanelCommand
        {
            get
            {
                if (_openSearchInvitePanelCommand == null)
                {
                    _openSearchInvitePanelCommand = new RelayCommand(new Action<object>(OpenSearchInvitePanel));
                }

                return _openSearchInvitePanelCommand;
            }
        }

        private ICommand? _hideSearchInvitePanelCommand;
        public ICommand HideSearchInvitePanelCommand
        {
            get
            {
                if (_hideSearchInvitePanelCommand == null)
                {
                    _hideSearchInvitePanelCommand = new RelayCommand(new Action<object>(HideSearchInvitePanel));
                }

                return _hideSearchInvitePanelCommand;
            }
        }

        private ICommand? _inviteToChatCommand;
        public ICommand InviteToChatCommand
        {
            get
            {
                if (_inviteToChatCommand == null)
                {
                    _inviteToChatCommand = new RelayCommand(new Action<object>(InviteToChat), 
                        (obj) => FoundedUsersToAdd.Any());
                }

                return _inviteToChatCommand;
            }
        }

        private ICommand? _leaveChatCommand;
        public ICommand LeaveChatCommand
        {
            get
            {
                if (_leaveChatCommand == null)
                {
                    _leaveChatCommand = new RelayCommand(new Action<object>(LeaveChat));
                }

                return _leaveChatCommand;
            }
        }

        private ICommand? _searchForUserCommand;
        public ICommand SearchForUserCommand
        {
            get
            {
                if (_searchForUserCommand == null)
                {
                    _searchForUserCommand = new RelayCommand(new Action<object>(SearchForUser));
                }

                return _searchForUserCommand;
            }
        }
        #endregion

        #region Methods

        private async void OpenChatMenu(object obj)
        {
            _mainPageViewModel.SelectedChatPanelVisibility = Visibility.Visible;

            var list = await ConnectionService.GetParticipantsFromChat(ApplicationState.SelectedChat!.Model.Id);

            if (list == null)
                return;

            ParticipantsOfSelectedChat.Clear();

            foreach (var user in list.Select(u => new UserViewModel(u)))
            {
                ParticipantsOfSelectedChat.Add(user);
            }
        }

        private void HideChatMenu(object obj)
        {
            _mainPageViewModel.SelectedChatPanelVisibility = Visibility.Collapsed;
        }

        // Панель на якій буде розміщено UI для пошуку користувача та його додавання у чат
        private void OpenSearchInvitePanel(object obj)
        {
            _mainPageViewModel.AddUserToChatPanelVisibility = Visibility.Visible;
        }

        private void HideSearchInvitePanel(object obj)
        {
            _mainPageViewModel.AddUserToChatPanelVisibility = Visibility.Collapsed;
            FoundedUsersToAdd.Clear();
        }

        private async void SearchForUser(object obj)
        {
            if (!string.IsNullOrWhiteSpace(EnteredUserEmail))
            {
                var foundedUser = await ConnectionService.FindUserByEmail(EnteredUserEmail);

                if (foundedUser != null)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        FoundedUsersToAdd.Clear();
                        FoundedUsersToAdd.Add(new UserViewModel(foundedUser));
                        (InviteToChatCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    });
                }
            }
        }

        private async void InviteToChat(object obj)
        {
            if (FoundedUsersToAdd.Any())
            {
                var userToInvite = FoundedUsersToAdd.First();
                var selectedChat = ApplicationState.SelectedChat!.Model;

                if (userToInvite.Id == ApplicationState.LoggedUser!.Id)
                {
                    InfoDialog infoDialog = new InfoDialog("You could not invite yourself to chat.", App.Current.MainWindow);
                    infoDialog.ShowDialog();
                    return;
                }
                
                if(await ConnectionService.CheckUserInChat(userToInvite.Id, selectedChat.Id))
                {
                    InfoDialog infoDialog = new InfoDialog("There is already such a user in the chat.", App.Current.MainWindow);
                    infoDialog.ShowDialog();
                    return;
                }

                await ConnectionService.InviteToChat(userToInvite.Id, selectedChat.Id);

                _mainPageViewModel.AddUserToChatPanelVisibility = Visibility.Collapsed;
            }
        }

        private async void LeaveChat(object obj)
        {
            WarningDialog warningDialog = new WarningDialog(
                "Are you sure you want to leave this chat?",
                App.Current.MainWindow);

            if (warningDialog.ShowDialog() == true)
            {
                var loggedUser = ApplicationState.LoggedUser;
                var selectedChat = ApplicationState.SelectedChat;

                if (loggedUser != null && selectedChat != null)
                {
                    await ConnectionService.LeaveChat(loggedUser.Id, selectedChat.Model.Id);
                }
            }
        }
        #endregion
    }
}
