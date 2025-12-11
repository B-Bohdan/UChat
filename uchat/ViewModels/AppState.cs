using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;

namespace uchat.ViewModels
{
    public class AppState : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationContext> ContextFactory;
        private readonly IConnectionService _connectionService;

        public AppState(IConnectionService connectionService, IDbContextFactory<ApplicationContext> contextFactory, IConfigurationService configurationService)
        {
            _connectionService = connectionService;
            ContextFactory = contextFactory;
            Chats = new ObservableCollection<ChatViewModel>();

            var connectionState = connectionService.GetConnectionState();

            string? userId = configurationService.Get<string>("AuthorizedUserId");

            if (int.TryParse(userId, out int id))
            {
                using (var context = contextFactory.CreateDbContext())
                {
                    var userFromLocal = context.Users.FirstOrDefault(u => u.Id == id);

                    if (userFromLocal != null)
                    {
                        LoggedUser = new UserViewModel(userFromLocal);

                        var localChats = context.Chats
                                                .Include(c => c.Participants)
                                                .Include(c => c.Messages!)
                                                    .ThenInclude(m => m.Sender)
                                                .ToList();

                        var loadedViewModels = new List<ChatViewModel>();

                        foreach (var chatModel in localChats)
                        {
                            var chatVm = new ChatViewModel(chatModel);

                            // Если в модели есть сообщения, обрабатываем их
                            if (chatModel.Messages != null)
                            {
                                var sortedMessages = chatModel.Messages.OrderBy(m => m.SentAt);

                                foreach (var msgModel in sortedMessages)
                                {
                                    var msgVm = new TextMessageViewModel((TextMessage)msgModel, LoggedUser!.Id, new UserViewModel(msgModel.Sender));

                                    chatVm.Messages.Add(msgVm);
                                }
                            }

                            loadedViewModels.Add(chatVm);
                        }

                        // Присваиваем итоговую коллекцию
                        Chats = new ObservableCollection<ChatViewModel>(loadedViewModels);
                    }
                }
            }
        }

        private UserViewModel? _loggeduser;
        public UserViewModel? LoggedUser
        {
            get { return _loggeduser; }
            set { _loggeduser = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ChatViewModel>? _chats;
        public ObservableCollection<ChatViewModel>? Chats
        {
            get => _chats;
            set
            {
                _chats = value; OnPropertyChanged();
            }
        }

        private ChatViewModel? _selectedChat;
        public ChatViewModel? SelectedChat
        {
            get => _selectedChat;
            set
            {
                _selectedChat = value;
                OnPropertyChanged();

                if (_selectedChat != null)
                {
                    _ = LoadHistoryForSelectedChat(_selectedChat);
                }
            }
        }

        private bool _isInMessageEditingMode;
        public bool IsInMessageEditingMode
        {
            get => _isInMessageEditingMode;
            set
            {
                _isInMessageEditingMode = value; 
                OnPropertyChanged();
            }
        }

        private TextMessageViewModel? _currentMessageInEditing;
        public TextMessageViewModel? CurrentMessageInEditing
        {
            get => _currentMessageInEditing;
            set
            {
                _currentMessageInEditing = value;
                OnPropertyChanged();
            }
        }

        #region Methods

        private async Task LoadHistoryForSelectedChat(ChatViewModel chatVm)
        {
            try
            {
                // Качаем сообщения с сервера
                var serverMessages = await _connectionService.GetChatMessages(chatVm.Model.Id);

                if (serverMessages != null && serverMessages.Any())
                {
                    // Cохраняем в Локальную БД и обновляем UI
                    await SaveMessagesToLocalDbAndUi(chatVm, serverMessages);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        private async Task SaveMessagesToLocalDbAndUi(ChatViewModel chatVm, List<TextMessage> messages)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                foreach (var msg in messages)
                {
                    // Проверяем, нет ли уже такого сообщения в ViewModel
                    if (!chatVm.Messages.Any(vm => vm.Model.Id == msg.Id))
                    {
                        var msgVm = new TextMessageViewModel(msg, LoggedUser!.Id,
                            new UserViewModel(msg.Sender));

                        chatVm.AddMessage(msgVm);
                    }
                }
            });

            // Сохраняем в локальную БД в фоне
            await Task.Run(async () =>
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    foreach (var msg in messages)
                    {
                        if (context.Messages.Any(m => m.Id == msg.Id)) continue;

                        if (msg.Sender != null)
                        {
                            // Проверяем, не следит ли контекст УЖЕ за этим юзером
                            var trackedUser = context.Users.Local.FirstOrDefault(u => u.Id == msg.Sender.Id);

                            if (trackedUser != null)
                            {
                                // Подменяем пришедший объект на тот, который уже в памяти.
                                msg.Sender = trackedUser;
                            }
                            else
                            {
                                // Если в памяти нет, проверяем в базе данных
                                var dbUser = context.Users.FirstOrDefault(u => u.Id == msg.Sender.Id);

                                if (dbUser != null)
                                {
                                    msg.Sender = dbUser;
                                }
                                else
                                {
                                    msg.Sender.Chats = null!;

                                    // Явно говорим, что этого юзера надо добавить
                                    context.Entry(msg.Sender).State = EntityState.Added;
                                }
                            }
                        }

                        // Настройка сообщения
                        msg.ChatId = chatVm.Model.Id;
                        msg.ChatInstance = null!;

                        context.Entry(msg).State = EntityState.Added;
                    }

                    await context.SaveChangesAsync();
                }
            });
        }

        public async Task SaveChatsDataAsync(IEnumerable<Chat> incomingChats)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                // Если коллекция null, создаем новую
                if (Chats == null) Chats = new ObservableCollection<ChatViewModel>();

                var incomingList = incomingChats.ToList();
                var incomingIds = incomingList.Select(c => c.Id).ToHashSet();

                // Удаляем из UI чаты, которых больше нет на сервере
                for (int i = Chats.Count - 1; i >= 0; i--)
                {
                    if (!incomingIds.Contains(Chats[i].Model.Id))
                    {
                        Chats.RemoveAt(i);
                    }
                }

                // Обновляем существующие или добавляем новые
                foreach (var incomingChat in incomingList)
                {
                    // Ищем чат в памяти (который мы загрузили из локальной БД в конструкторе)
                    var existingVm = Chats.FirstOrDefault(vm => vm.Model.Id == incomingChat.Id);

                    if (existingVm != null)
                    {
                        // Обновляем только мета-данные, которые могли измениться на сервере.
                        existingVm.Tag = incomingChat.Tag;
                    }
                    else
                    {
                        Chats.Add(new ChatViewModel(incomingChat));
                    }
                }
            });

            // Сохраняем структуру чатов в БД
            await Task.Run(async () =>
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    var dbChats = await context.Chats.ToListAsync(); // Тут сообщения грузить не обязательно

                    foreach (var incomingChat in incomingChats)
                    {
                        // Чистим навигационные свойства, для EF
                        incomingChat.Messages = null;
                        incomingChat.Participants = null;

                        var existingChat = dbChats.FirstOrDefault(c => c.Id == incomingChat.Id);

                        if (existingChat != null)
                        {
                            // Обновляем поля (название и т.д.)
                            context.Entry(existingChat).CurrentValues.SetValues(incomingChat);
                        }
                        else
                        {
                            // Добавляем новый чат в кэш
                            context.Entry(incomingChat).State = EntityState.Added;
                        }
                    }

                    // Удаляем лишние из БД
                    var incomingKeys = incomingChats.Select(c => c.Id).ToList();
                    var toDelete = dbChats.Where(c => !incomingKeys.Contains(c.Id)).ToList();
                    context.Chats.RemoveRange(toDelete);

                    await context.SaveChangesAsync();
                }
            });
        }

        #endregion
    }
}