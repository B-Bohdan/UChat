using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using uchat.modelbase.Models;
using uchat.Models;
using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;

namespace uchat.ViewModels
{
    /// <summary>
    /// Зберігає глоабальні стани додатку, які не можно динамічно грузити з json конфігурації.
    /// </summary>
    public class AppState : ObservableObject
    {
        private readonly IDbContextFactory<ApplicationContext> ContextFactory;

        public AppState(IConnectionService connectionService, IDbContextFactory<ApplicationContext> contextFactory, IConfigurationService configurationService)
        {
            ContextFactory = contextFactory;

            Chats = new ObservableCollection<ChatViewModel>();

            var connectionState = connectionService.GetConnectionState();

            // Завантаження даних про користувача з локальної БД, якщо підключитись не вдалось
            if (connectionState == null || connectionState == HubConnectionState.Disconnected)
            {
                string? userId = configurationService.Get<string>("AuthorizedUserId");

                if(int.TryParse(userId, out int id))
                {
                    using (var context = contextFactory.CreateDbContext())
                    {
                        var userFromLocal = context.Users.FirstOrDefault(u => u.Id == id);

                        if(userFromLocal == null)
                        {
                            // ...
                            return;
                        }

                        LoggedUser = new UserViewModel(userFromLocal);
                        Chats = new ObservableCollection<ChatViewModel>(context.Chats.ToList().Select(c => new ChatViewModel(c)));
                    }
                }
            }
        }

        private UserViewModel? _loggeduser;
        public UserViewModel? LoggedUser
        {
            get { return _loggeduser; } 
            set { _loggeduser = value; OnPropertyChanged();}
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
            }
        }

        #region Methods

        public async Task SaveChatsDataAsync(IEnumerable<Chat> incomingChats)
        {
            // Обновляем UI
            Chats = new ObservableCollection<ChatViewModel>(incomingChats.Select(c => new ChatViewModel(c)));

            using (var context = ContextFactory.CreateDbContext())
            {
                // 1. Загружаем все существующие локальные чаты в память
                // (Опять же, если пользователей много, не забудьте добавить .Where(u => u.UserId == ...))
                var dbChats = await context.Chats.ToListAsync();

                // Превращаем входящий список в Словарь для мгновенного поиска по ID
                var incomingMap = incomingChats.ToDictionary(c => c.Id);

                // --- ЭТАП 1: УДАЛЕНИЕ (DELETE) ---
                // Находим те чаты, которые есть в БД, но их ID нет во входящем словаре
                var chatsToDelete = dbChats
                                    .Where(dbChat => !incomingMap.ContainsKey(dbChat.Id))
                                    .ToList();

                if (chatsToDelete.Any())
                {
                    context.Chats.RemoveRange(chatsToDelete);
                }

                // --- ЭТАП 2: ВСТАВКА И ОБНОВЛЕНИЕ (UPSERT) ---
                foreach (var incomingChat in incomingChats)
                {
                    // Сразу чистим навигационные свойства, чтобы избежать ошибки "Value cannot be null"
                    incomingChat.Messages = null;
                    incomingChat.Participants = null;

                    // Ищем этот чат в уже загруженном списке из БД
                    var existingChat = dbChats.FirstOrDefault(c => c.Id == incomingChat.Id);

                    if (existingChat != null)
                    {
                        // СЦЕНАРИЙ: UPDATE
                        // Запись найдена, копируем в нее новые значения полей
                        context.Entry(existingChat).CurrentValues.SetValues(incomingChat);
                    }
                    else
                    {
                        // СЦЕНАРИЙ: INSERT
                        // Записи нет локально - добавляем новую.
                        // Так как у incomingChat уже проставлен ID (с сервера),
                        // EF попытается вставить именно этот ID.
                        await context.Chats.AddAsync(incomingChat);
                    }
                }

                // Сохраняем все изменения за одну транзакцию
                await context.SaveChangesAsync();
            }
        }
        #endregion
    }
}