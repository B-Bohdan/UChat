using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;
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

            ConnectionService.OnMessageReceived += ConnectionService_OnMessageReceived;
            ConnectionService.OnMessageDeleted += ConnectionService_OnMessageDeleted;
            ConnectionService.OnTextMessageEdited += ConnectionService_OnTextMessageEdited;
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

        private string? _messageToSend;
        public string? MessageToSend
        {
            get => _messageToSend;
            set { _messageToSend = value; OnPropertyChanged(); }
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

        private ICommand? _loadMessageHistoryCommand;
        public ICommand LoadMessageHistoryCommand
        {
            get
            {
                if(_loadMessageHistoryCommand == null)
                {
                    _loadMessageHistoryCommand = new RelayCommand(new Action<object>(LoadMessageHistory));
                }

                return _loadMessageHistoryCommand;
            }
        }

        private ICommand? _sendMessageCommand;
        public ICommand SendMessageCommand
        {
            get
            {
                if (_sendMessageCommand == null)
                {
                    _sendMessageCommand = new RelayCommand(new Action<object>(SendMessage));
                }

                return _sendMessageCommand;
            }
        }

        private ICommand? _editMessageCommand;
        public ICommand EditMessageCommand
        {
            get
            {
                if(_editMessageCommand == null)
                {
                    _editMessageCommand = new RelayCommand(new Action<object>(EditMessage));
                }

                return _editMessageCommand;
            }
        }

        private ICommand? _deleteMessageCommand;
        public ICommand DeleteMessageCommand
        {
            get
            {
                if (_deleteMessageCommand == null)
                {
                    _deleteMessageCommand = new RelayCommand(new Action<object>(DeleteMessage));
                }

                return _deleteMessageCommand;
            }
        }

        private ICommand? _cancelEditingModeCommand;
        public ICommand CancelEditingModeCommand
        {
            get
            {
                if (_cancelEditingModeCommand == null)
                {
                    _cancelEditingModeCommand = new RelayCommand(new Action<object>(CancelEditingMode));
                }

                return _cancelEditingModeCommand;
            }
        }
        #endregion

        #region Methods

        #region Event handlers

        private void ConnectionService_OnMessageReceived(Message message, int chatId)
        {
            if(message is TextMessage textMessage)
            {
                using (var context = DbContextFactory.CreateDbContext())
                {
                    Chat? chat = context.Chats.FirstOrDefault(c => c.Id == chatId);

                    if (chat != null)
                    {
                        TextMessageViewModel textMessageView = new TextMessageViewModel(textMessage, new UserViewModel(textMessage.Sender));

                        SaveIncomingMessageToLocalDb(textMessage);

                        ChatViewModel? chatViewModel = ApplicationState.Chats!.FirstOrDefault(c => c.Model.Id == chatId);
                        //chatViewModel!.Model.Messages = new List<Message>();

                        if (chatViewModel == null)
                            return;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            chatViewModel.AddMessage(textMessageView);
                        });
                    }
                }
            }
        }

        private async void ConnectionService_OnMessageDeleted(int messageId, int chatId)
        {
            using(var context = DbContextFactory.CreateDbContext())
            {
                await context.Messages.Where(m => m.Id == messageId).ExecuteDeleteAsync();
                await context.SaveChangesAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    var chatViewModel = ApplicationState.Chats!.FirstOrDefault(c => c.Model.Id == chatId);

                    if(chatViewModel == null) 
                        return;

                    var messageViewModel = chatViewModel.Messages.FirstOrDefault(m => m.Model.Id == messageId);

                    if(messageViewModel == null)
                        return;

                    chatViewModel.Messages.Remove(messageViewModel);
                });
            }
        }

        private async void ConnectionService_OnTextMessageEdited(int messageId, int chatId, string newText)
        {
            using (var context = DbContextFactory.CreateDbContext())
            {
                await context.TextMessages.Where(m => m.Id == messageId)
                    .ExecuteUpdateAsync(m => m
                        .SetProperty(m => m.Text, newText)
                        .SetProperty(m => m.IsEdited, true));

                await context.SaveChangesAsync();

                App.Current.Dispatcher.Invoke(() =>
                {
                    var chatViewModel = ApplicationState.Chats!.FirstOrDefault(c => c.Model.Id == chatId);

                    if (chatViewModel == null)
                        return;

                    var messageViewModel = chatViewModel.Messages.FirstOrDefault(m => m.Model.Id == messageId) as TextMessageViewModel;

                    if (messageViewModel == null)
                        return;

                    messageViewModel.Text = newText;
                    messageViewModel.IsEdited = true;
                });
            }
        }
        #endregion

        public void SaveIncomingMessageToLocalDb(TextMessage textMessage)
        {
            using (var context = DbContextFactory.CreateDbContext()) // Укажите ваш путь к БД
            {
                bool messageExists = context.Messages.Any(m => m.Id == textMessage.Id);

                if (messageExists)
                {
                    return;
                }

                // Проверка отправителя
                var senderInDb = context.Users.AsNoTracking().FirstOrDefault(u => u.Id == textMessage.Sender.Id);
                if (senderInDb == null)
                {
                    var newUser = textMessage.Sender;
                    newUser.Chats = null!;
                    context.Users.Add(newUser);
                }
                else
                {
                    context.Entry(textMessage.Sender).State = EntityState.Unchanged;
                }

                // Сохранение сообщения
                textMessage.ChatInstance = null!;
                context.Entry(textMessage).State = EntityState.Added;

                try
                {
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
                }
            }
        }

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

        private async void SendMessage(object obj)
        {
            if (!string.IsNullOrWhiteSpace(MessageToSend))
            {
                if (!ApplicationState.IsInMessageEditingMode)
                {
                    await ConnectionService.SendMessage(
                        ApplicationState.LoggedUser!.Id,
                        ApplicationState.SelectedChat!.Model.Id,
                        new TextMessage() { Text = MessageToSend });
                }
                else
                {
                    await ConnectionService.EditMessage(ApplicationState.SelectedChat!.Model.Id, 
                        ApplicationState.CurrentMessageInEditing!.Model.Id, MessageToSend);

                    ApplicationState.IsInMessageEditingMode = false;
                }

                MessageToSend = string.Empty;
            }
        }

        private void LoadMessageHistory(object obj)
        {

        }

        private async void DeleteMessage(object obj)
        {
            if(obj is TextMessageViewModel textMessageViewModel)
            {
                WarningDialog warningDialog = 
                    new WarningDialog("Are you sure you want to delete this message for everyone?", App.Current.MainWindow);

                if(warningDialog.ShowDialog() == true)
                {
                    await ConnectionService.DeleteMessage(textMessageViewModel.Model.Id, ApplicationState.SelectedChat!.Model.Id);
                }
            }
        }

        private void EditMessage(object obj)
        {
            if(obj is TextMessageViewModel textMessageView)
            {
                ApplicationState.IsInMessageEditingMode = true;
                ApplicationState.CurrentMessageInEditing = textMessageView;

                MessageToSend = textMessageView.Text;
            }
        }

        private void CancelEditingMode(object obj)
        {
            ApplicationState.IsInMessageEditingMode = false;
            MessageToSend = string.Empty;
        }
        #endregion
    }
}
