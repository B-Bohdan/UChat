using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;
using uchat.Models;
using uchat.Services.IServices;

namespace uchat.Services
{
    /// <summary>
    /// Потрібний для встановлення з'єднання з сервером то керування їм.
    /// </summary>
    public class ConnectionService : IConnectionService
    {
        private readonly IConfigurationService _configurationService;
        private readonly IDbContextFactory<ApplicationContext> _localContextFactory;
        private HubConnection? _hubConnection;

        public event Action? OnConnected;
        public event Action? OnDisconnected;

        public event Action<User>? OnAuthorizationConfirmed;
        public event Action<User?>? OnUserFound;
        public event Action<Chat>? OnAddedToChat;
        public event Action<int>? OnKickedFromChat;

        public event Action<Chat>? OnChatCreated;
        public event Action<int>? OnChatDeleted;

        public event Action<Message, int>? OnMessageReceived;
        public event Action<int, int>? OnMessageDeleted;
        public event Action<int, int, string>? OnTextMessageEdited;
        public event Action<User, int>? OnUserJoinedToChat;
        public event Action<User, int>? OnUserKickedFromChat;
        public event Action<User, int>? OnUserLeavedTheChat;

        public ConnectionService(IConfigurationService configurationService, IDbContextFactory<ApplicationContext> dbContextFactory)
        {
            _configurationService = configurationService;
            _localContextFactory = dbContextFactory;
        }

        public async Task InitializeConnection(string serverBaseUrl)
        {
            int userId = 0; // По умолчанию 0 (если юзера нет)

            // Используем фабрику для создания краткосрочного контекста
            using (var db = _localContextFactory.CreateDbContext())
            {
                // Берем первого и единственного юзера
                var localUser = await db.Users.FirstOrDefaultAsync();
                if (localUser != null)
                {
                    userId = localUser.Id;
                }
            }

            // Добавляем Query Parameter
            var fullUrl = $"{serverBaseUrl}?userId={userId}";

            // 4. СТРОИМ ПОДКЛЮЧЕНИЕ
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(fullUrl)
                .WithAutomaticReconnect()
                .Build();

            // 5. Запускаем
            await StartAsync();

            // Підписка на подію закриття з'єднання
            _hubConnection.Closed += async (error) =>
            {
                OnDisconnected?.Invoke();
                await Task.CompletedTask;
            };

            _hubConnection.On<string>("ErrorReceived", (errorMessage) =>
            {

            });

            /* Загальні події */
            #region Common Events

            // Підписка на подію натходження підтвердження авторизації
            _hubConnection.On<User>("AuthorizationConfirmed", (user) =>
            {
                // Виклик події підтвердження авторизації
                OnAuthorizationConfirmed?.Invoke(user);
            });

            // Підписка на подію надходження знайденого користувача
            _hubConnection.On<User?>("UserFound", (user) =>
            {
                // Виклик події надходження знайденого користувача
                OnUserFound?.Invoke(user);
            });

            // Підписка на подію додавання поточного користувача до чату (без його волі)
            _hubConnection.On<Chat>("YouAddedToChat", async (chat) =>
            {
                await _hubConnection.SendAsync("JoinChatGroup", chat.Id);
                OnAddedToChat?.Invoke(chat);
            });

            // Підписка на подію видалення поточного користувача з чату (без його волі)
            _hubConnection.On<int>("YouKickedFromChat", async (chatId) =>
            {
                await _hubConnection.SendAsync("LeaveChatGroup", chatId);
                OnKickedFromChat?.Invoke(chatId);
            });
            #endregion

            /* Події зовшіншньої взаємодії з чатами: Їх створення, видалення та помилка з цим.*/
            #region Chat External Events

            // Користувач безпосередньо створив чат
            _hubConnection.On<Chat>("ChatCreated", (chat) =>
            {
                OnChatCreated?.Invoke(chat);
            });

            // Чат було видалено, або самим користувачем або адміністратором чату
            _hubConnection.On<int>("ChatDeleted", (chatId) =>
            {
                OnChatDeleted?.Invoke(chatId);
            });

            _hubConnection.On<string>("ChatCreationFailed", (message) =>
            {

            });
            #endregion

            /* Події внутрішньої взаємодії з чатами: Надходження повідомлень,
            редагування, видалення та результати менеджменту над користувачами в чаті.*/
            #region Chat Internal Events

            // Підписка на подію надходження нового повідомлення
            _hubConnection.On<Message, int>("ReceivedMessage", (message, chatId) =>
            {
                // Виклик події надходження повідомлення
                OnMessageReceived?.Invoke(message, chatId);
            });

            // Підписка на подію видалення повідомлення
            _hubConnection.On<int, int>("MessageDeleted", (mesageId, chatId) =>
            {
               OnMessageDeleted?.Invoke(mesageId, chatId);
            });

            // Підписка на подію редагування текстового повідомлення
            _hubConnection.On<int, int, string>("TextMessageEdited", (messageId, chatId, newContent) =>
            {
                OnTextMessageEdited?.Invoke(messageId, chatId, newContent);
            });

            // Підписка на подію приєднання користувача до чату
            _hubConnection.On<User, int>("UserJoined", (user, chatId) =>
            {
                OnUserJoinedToChat?.Invoke(user, chatId);
            });

            // Підписка на подію виходу користувача з чату
            _hubConnection.On<User, int>("UserLeft", (user, chatId) =>
            {
                OnUserLeavedTheChat?.Invoke(user, chatId);
            });

            // Підписка на подію видалення користувача з чату (без його волі)
            _hubConnection.On<User, int>("UserKicked", (user, chatId) =>
            {
                OnUserKickedFromChat?.Invoke(user, chatId);
            });
            #endregion
        }

        /// <summary>
        /// Asynchronously starts the connection to the hub if it has been initialized.
        /// </summary>
        /// <remarks>If the connection is successfully started, the OnConnected event is invoked. This
        /// method has no effect if the hub connection has not been initialized.</remarks>
        /// <returns>A task that represents the asynchronous start operation.</returns>
        public async Task StartAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StartAsync();
                OnConnected?.Invoke();
            }
            else
            {
                throw new Exception("Hub connection is not initialized.");
            }
        }

        /// <summary>
        /// Asynchronously stops the active SignalR hub connection and releases associated resources.
        /// </summary>
        /// <remarks>If the hub connection is not active, this method completes without performing any
        /// action. This method is safe to call multiple times; subsequent calls after the connection is stopped have no
        /// effect.</remarks>
        /// <returns>A task that represents the asynchronous stop operation.</returns>
        public async Task StopAsync()
        {
            if(_hubConnection != null)
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
            }
        }

        public Task CheckUserAuthorization(string emailAddress)
        {
            throw new NotImplementedException();
        }

        public Task SendMessage(int senderId, int chatId, Message message)
        {
            if(message is TextMessage textMessage)
            {

            }
            throw new NotImplementedException();
        }

        public HubConnectionState? GetConnectionState()
        {
            if (_hubConnection != null)
                return _hubConnection.State;
            return null;
        }
    }
}
