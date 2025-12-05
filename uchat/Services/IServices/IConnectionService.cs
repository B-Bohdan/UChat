using Microsoft.AspNetCore.SignalR.Client;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;

namespace uchat.Services.IServices
{
    public interface IConnectionService
    {
        public event Action? OnConnected;
        public event Action? OnDisconnected;

        // Common events
        public event Action<User>? OnAuthorizationConfirmed;
        public event Action<User?>? OnUserFound;
        public event Action<Chat>? OnAddedToChat;
        public event Action<int>? OnKickedFromChat;

        // Chat external events
        public event Action<Chat>? OnChatCreated;
        public event Action<int>? OnChatDeleted;

        // Chat internal events
        public event Action<Message, int>? OnMessageReceived;
        public event Action<int, int>? OnMessageDeleted;
        public event Action<int, int, string>? OnTextMessageEdited;
        public event Action<User, int>? OnUserJoinedToChat;
        public event Action<User, int>? OnUserKickedFromChat;
        public event Action<User, int>? OnUserLeavedTheChat;

        public Task<string> GetGoogleIdTokenAsync();

        public Task InitializeConnection(string serverBaseUrl);

        public Task StartAsync();

        public Task StopAsync();

        public HubConnectionState? GetConnectionState();

        public Task AuthorizeUser(string googleToken);
        public Task SendMessage(int senderId, int chatId, Message message);
        public Task<User?> GetUserInfo(int userId);
    }
}
