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

        public static int CurrentUserId { get; private set; }

        public AppState(IConnectionService connectionService, IDbContextFactory<ApplicationContext> contextFactory, IConfigurationService configurationService)
        {
            _connectionService = connectionService;
            ContextFactory = contextFactory;
            Chats = new ObservableCollection<ChatViewModel>();

            var connectionState = connectionService.GetConnectionState();
            string? userIdStr = configurationService.Get<string>("AuthorizedUserId");

            if (int.TryParse(userIdStr, out int userId))
            {
                using (var context = contextFactory.CreateDbContext())
                {
                    var userFromLocal = context.Users.FirstOrDefault(u => u.Id == userId);

                    if (userFromLocal != null)
                    {
                        // Присвоение LoggedUser автоматически обновит CurrentUserId (см. сеттер ниже)
                        LoggedUser = new UserViewModel(userFromLocal);

                        // 1. Загружаем чаты со всем содержимым из локальной БД
                        var localChats = context.Chats
                                                .Include(c => c.Participants)
                                                .Include(c => c.Messages!)
                                                    .ThenInclude(m => m.Sender)
                                                .ToList();

                        var loadedViewModels = new List<ChatViewModel>();

                        foreach (var chatModel in localChats)
                        {
                            // Просто создаем ViewModel. 
                            // Конструктор ChatViewModel сам отсортирует сообщения и добавит их внутрь.
                            loadedViewModels.Add(new ChatViewModel(chatModel));
                        }

                        Chats = new ObservableCollection<ChatViewModel>(loadedViewModels);
                    }
                }
            }
        }

        private UserViewModel? _loggeduser;
        public UserViewModel? LoggedUser
        {
            get { return _loggeduser; }
            set
            {
                _loggeduser = value;

                CurrentUserId = _loggeduser?.Id ?? 0;

                OnPropertyChanged();
            }
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

                // Как только выбрали чат - пытаемся подгрузить историю с сервера
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

        // Загрузка истории сообщений при выборе чата
        private async Task LoadHistoryForSelectedChat(ChatViewModel chatVm)
        {
            try
            {
                // Качаем сообщения с сервера
                var serverMessages = await _connectionService.GetChatMessages(chatVm.Model.Id);

                if (serverMessages != null && serverMessages.Any())
                {
                    // Сохраняем в Локальную БД и обновляем UI
                    await SaveMessagesToLocalDbAndUi(chatVm, serverMessages);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        // Сохранение списка сообщений (пришедших с сервера)
        private async Task SaveMessagesToLocalDbAndUi(ChatViewModel chatVm, List<TextMessage> messages)
        {
            // Обновляем UI
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var msg in messages)
                {
                    // Проверяем по ID, чтобы не добавлять дубликаты (например, превью уже есть в списке)
                    if (!chatVm.Messages.Any(vm => vm.Model.Id == msg.Id))
                    {
                        var msgVm = MessageViewModel.Create(msg);
                        // Теперь AddMessage сам найдет правильное место (в начале списка) для старого сообщения
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
                        // Пропускаем дубликаты
                        if (context.Messages.Any(m => m.Id == msg.Id)) continue;

                        if (msg.Sender != null)
                        {
                            // Ищем в локальном кэше EF (то, что добавили в этом цикле ранее)
                            var trackedUser = context.Users.Local.FirstOrDefault(u => u.Id == msg.Sender.Id);

                            if (trackedUser != null)
                            {
                                // Подменяем объект на тот, что уже в памяти
                                msg.Sender = trackedUser;
                            }
                            else
                            {
                                // Если в памяти нет, ищем в БД
                                var dbUser = context.Users.FirstOrDefault(u => u.Id == msg.Sender.Id);

                                if (dbUser != null)
                                {
                                    // Подменяем объект на тот, что в БД
                                    msg.Sender = dbUser;
                                }
                                else
                                {
                                    // Юзера нет нигде. Добавляем как нового.
                                    msg.Sender.Chats = null!; // Чистим связи
                                    context.Entry(msg.Sender).State = EntityState.Added;
                                }
                            }
                        }

                        // Настройка сообщения
                        msg.ChatId = chatVm.Model.Id;
                        msg.ChatInstance = null!; // Разрываем цикл ссылок

                        context.Entry(msg).State = EntityState.Added;
                    }

                    await context.SaveChangesAsync();
                }
            });
        }

        // Обновление списка чатов (после логина или при старте)
        public async Task SaveChatsDataAsync(IEnumerable<Chat> incomingChats)
        {
            // Обновляем UI
            App.Current.Dispatcher.Invoke(() =>
            {
                // Если коллекции нет - создаем
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
                    var existingVm = Chats.FirstOrDefault(vm => vm.Model.Id == incomingChat.Id);

                    if (existingVm != null)
                    {
                        // Чат уже есть. Обновляем мета-данные.
                        existingVm.Tag = incomingChat.Tag;
                    }
                    else
                    {
                        // Чата нет. Создаем новый.
                        Chats.Add(new ChatViewModel(incomingChat));
                    }
                }
            });

            // Сохраняем структуру чатов в БД
            await Task.Run(async () =>
            {
                using (var context = ContextFactory.CreateDbContext())
                {
                    var dbChats = await context.Chats.ToListAsync();

                    foreach (var incomingChat in incomingChats)
                    {
                        // Чистим навигационные свойства для EF
                        incomingChat.Messages = null;
                        incomingChat.Participants = null;

                        var existingChat = dbChats.FirstOrDefault(c => c.Id == incomingChat.Id);

                        if (existingChat != null)
                        {
                            // Обновляем существующий
                            context.Entry(existingChat).CurrentValues.SetValues(incomingChat);
                        }
                        else
                        {
                            // Добавляем новый
                            context.Entry(incomingChat).State = EntityState.Added;
                        }
                    }

                    // Удаляем лишние чаты из БД
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