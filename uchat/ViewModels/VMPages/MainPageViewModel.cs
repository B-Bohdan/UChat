using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Input;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.modelbase.Models;
using System.Collections.ObjectModel;
using uchat.ViewModels.VMEntities;
using uchat.Views.Pages;
using System.IO;

namespace uchat.ViewModels.VMPages
{
    public class MainPageViewModel : ViewModelBase
    {
        public MainPageViewModel(AppState appState, IConfigurationService configurationService, 
            IConnectionService connectionService, IDbContextFactory<ApplicationContext> appContextFactory, INavigationService navigationService) : base(appState, configurationService, connectionService, appContextFactory, navigationService)
        {
            MenuVisibility = Visibility.Collapsed;
            FindUserPanelVisibility = Visibility.Collapsed;
            SelectedChatPanelVisibility = Visibility.Collapsed;
            AddUserToChatPanelVisibility = Visibility.Collapsed;

            FoundedUsers = new ObservableCollection<UserViewModel>();

            ConnectionService.OnAddedToChat += ConnectionService_OnAddedToChat;
            ConnectionService.OnSuccessfullyLeavedTheChat += ConnectionService_OnSuccessfullyLeavedTheChat;
        }

        #region Properties

        public ObservableCollection<UserViewModel> FoundedUsers { get; private set; }

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

        private Visibility _findUserPanelVisibility;
        public Visibility FindUserPanelVisibility
        {
            get => _findUserPanelVisibility;
            set
            {
                _findUserPanelVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility _selectedChatPanelVisibility;
        public Visibility SelectedChatPanelVisibility
        {
            get => _selectedChatPanelVisibility;
            set
            {
                _selectedChatPanelVisibility = value;
                OnPropertyChanged();
            }
        }

        private Visibility _addUserToChatPanelVisibility;
        public Visibility AddUserToChatPanelVisibility
        {
            get => _addUserToChatPanelVisibility;
            set
            {
                _addUserToChatPanelVisibility = value;
                OnPropertyChanged();
            }
        }

        // Для пошуку користувача за мейлом
        private string? _enteredUserEmail;
        public string? EnterteredUserEmail
        {
            get => _enteredUserEmail;
            set
            {
                _enteredUserEmail = value;
                OnPropertyChanged();
            }
        }

        private string? _enteredNewChatName;
        public string? EnteredNewChatName
        {
            get => _enteredNewChatName;
            set
            {
                _enteredNewChatName = value;
                OnPropertyChanged();
                (CreateChatWithCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        private ICommand? _logOutCommand;
        public ICommand LogOutCommand
        {
            get
            {
                if (_logOutCommand == null)
                {
                    _logOutCommand = new RelayCommand(new Action<object>(LogOut));
                }
                return _logOutCommand;
            }
        }

        private ICommand? _findUserCommand;
        public ICommand FindUserCommand
        {
            get
            {
                if (_findUserCommand == null)
                {
                    _findUserCommand = new RelayCommand(new Action<object>(FindUser));
                }
                return _findUserCommand;
            }
        }

        private ICommand? _hideFindUserPanelCommand;
        public ICommand HideFindUserPanelCommand
        {
            get
            {
                if (_hideFindUserPanelCommand == null)
                {
                    _hideFindUserPanelCommand = new RelayCommand(new Action<object>(HideFindUserPanel));
                }
                return _hideFindUserPanelCommand;
            }
        }

        private ICommand? _createChatWithCommand;
        public ICommand CreateChatWithCommand
        {
            get
            {
                if(_createChatWithCommand == null)
                {
                    _createChatWithCommand = new RelayCommand(new Action<object>(CreateChatWith)
                        , (obj) => !string.IsNullOrWhiteSpace(EnteredNewChatName));
                }

                return _createChatWithCommand;
            }
        }
        #endregion

        #region Methods

        #region Event handlers
        private async void ConnectionService_OnAddedToChat(Chat chat)
        {
            // 1. Сохраняем список ID участников во временную переменную
            // (Предполагаю, что у User ключ - это Id. Если другое имя - замените)
            var participantIds = chat.Participants!.Select(u => u.Id).ToList();

            using (var context = DbContextFactory.CreateDbContext())
            {
                // 2. Обнуляем навигационные свойства, чтобы разорвать граф
                chat.Messages = null;
                chat.Participants = null; // ВАЖНО: убираем связь с "чужими" объектами User

                // 3. Добавляем "чистый" чат. Теперь EF не ругается, так как вложенностей нет.
                context.Chats.Add(chat);

                // 4. Загружаем "родные" для этого контекста сущности пользователей
                // Это скажет EF: "Эти юзеры уже есть в БД, не надо их создавать заново"
                if (participantIds.Any())
                {
                    var existingUsers = await context.Users
                                             .Where(u => participantIds.Contains(u.Id))
                                             .ToListAsync();

                    // Инициализируем список заново, так как мы его занулили выше
                    chat.Participants = new List<User>();

                    // 5. Добавляем загруженных пользователей. 
                    // EF поймет: "Ага, Чат новый (Added), Юзеры старые (Unchanged) -> значит надо добавить только запись в таблицу связей (Many-to-Many join table)"
                    chat.Participants.AddRange(existingUsers);
                }

                await context.SaveChangesAsync();
            }

            App.Current.Dispatcher.Invoke(() =>
            {
                // Для UI используем исходный объект chat, так как в нем уже могут быть данные, 
                // которые мы занулили для БД (хотя мы их там восстановили частично, 
                // лучше пересоздать ViewModel чисто)
                ApplicationState.Chats!.Add(new ChatViewModel(chat));
            });
        }

        private async void ConnectionService_OnSuccessfullyLeavedTheChat(int chatId)
        {
            using(var context = DbContextFactory.CreateDbContext())
            {
                var chat = context.Chats.FirstOrDefault(c => c.Id == chatId);

                if (chat == null)
                    return;

                context.Chats.Remove(chat);
                await context.SaveChangesAsync();
            }

            var chatView = ApplicationState.Chats!.FirstOrDefault(cv => cv.Model.Id == chatId);

            if(chatView == null) 
                return;

            Application.Current.Dispatcher?.Invoke(() =>
            {
                ApplicationState.Chats!.Remove(chatView);
                SelectedChatPanelVisibility = Visibility.Collapsed;
            });
        }
        #endregion

        private void ShowMenu(object obj)
        {
            MenuVisibility = Visibility.Visible;
        }

        private void HideMenu(object obj)
        {
            MenuVisibility = Visibility.Hidden;
        }

        private async void LogOut(object obj)
        {
            MessageBoxResult result = MessageBox.Show(
                "Have you backed up your data? After perfoming this action, message data and log in information will be erased.",
                "Confirmation Warning",                                         
                MessageBoxButton.YesNo,                                       
                MessageBoxImage.Warning                                          
            );

            if(result == MessageBoxResult.Yes)
            {
                ConfigurationService.Set("AuthorizedUserId", string.Empty);
                ConfigurationService.Set("IsAuthorized", "false");

                using (var context = DbContextFactory.CreateDbContext())
                {
                    await context.Users.ExecuteDeleteAsync();
                    await context.Chats.ExecuteDeleteAsync();
                    await context.Messages.ExecuteDeleteAsync();
                    await context.SaveChangesAsync();
                }

                ApplicationState.LoggedUser = null;

                string tokenDirectory = "token.json";

                if (Directory.Exists(tokenDirectory))
                {
                    Directory.Delete(tokenDirectory, true);
                }

                MenuVisibility = Visibility.Collapsed;
                NavigationService.ChangePage<AuthorizationPage>();
            }
        }

        private async void FindUser(object obj)
        {
            if (!string.IsNullOrWhiteSpace(EnterteredUserEmail))
            {
                var foundedUser = await ConnectionService.FindUserByEmail(EnterteredUserEmail);

                if (foundedUser != null)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        FoundedUsers.Add(new UserViewModel(foundedUser));
                        EnteredNewChatName = $"{ApplicationState.LoggedUser!.FirstName} & {foundedUser.FirstName}";
                    });
                }
            }

            FindUserPanelVisibility = Visibility.Visible;
        }

        private void HideFindUserPanel(object? obj)
        {
            FindUserPanelVisibility = Visibility.Collapsed;
            FoundedUsers.Clear();
        }

        private async void CreateChatWith(object obj)
        {
            UserViewModel? loggedUser = ApplicationState.LoggedUser;
            UserViewModel companion = FoundedUsers.First();

            if (loggedUser != null && companion != null && !string.IsNullOrWhiteSpace(EnteredNewChatName))
            {
                if (loggedUser.Id == companion.Id)
                {
                    //...
                    // Тут зовем окошко с сообщениям, шо низя создавать чат с самим собой :)
                    MessageBox.Show("There is no any possible way to create chat with yourself."); // Пока меседж бокс, потом красивый диалог будет
                    return;
                }

                HideFindUserPanel(null);

                await ConnectionService.CreateChatWithUser(loggedUser.Id, companion.Id, EnteredNewChatName);
            }
        }
        #endregion
    }
}
