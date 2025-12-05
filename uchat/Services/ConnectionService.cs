using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
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
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverBaseUrl)
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

            _hubConnection.On<string>("AuthorizationError", (message) =>
            {

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

        public async Task<User?> GetUserInfo(int userId)
        {
            if (_hubConnection == null)
                return null;
            User? user = await _hubConnection.InvokeAsync<User?>("GetUserInfoAsync", userId);
            return user;
        }

        public async Task AuthorizeUser(string googleToken)
        {
            if(_hubConnection != null)
            {
                await _hubConnection.SendAsync("AuthorizeUser", googleToken);
            }
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

        public async Task<string> GetGoogleIdTokenAsync()
        {
            UserCredential credential;

            // Вказуємо які дані беремо: пошту, ім'я та призвище
            string[] scopes = {
                     Oauth2Service.Scope.UserinfoEmail,
                     Oauth2Service.Scope.UserinfoProfile
                };

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "uchat.client_secret.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new Exception("Failed to find client_secrets.json in embedded resources!");
                }

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore("token.json", true));
            }

            if (credential.Token.IsStale)
            {
                bool success = await credential.RefreshTokenAsync(CancellationToken.None);
                if (!success)
                {
                    // Тут потрібно перенаправляти на сторінку авторизації знову.
                }
            }

            // 4. Возвращаем именно IdToken (это та самая JWT строка для сервера)
            return credential.Token.IdToken;
        }
    }
}