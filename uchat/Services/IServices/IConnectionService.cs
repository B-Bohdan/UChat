using Microsoft.AspNetCore.SignalR.Client;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;

namespace uchat.Services.IServices
{
    public interface IConnectionService
    {
        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action? OnReconnecting;

        // Common events
        public event Action<User>? OnAuthorizationConfirmed;
        public event Action<User?>? OnUserFound;
        public event Action<Chat>? OnAddedToChat;

        // Chat internal events
        public event Action<Message, int>? OnMessageReceived;
        public event Action<int, int>? OnMessageDeleted;
        public event Action<int, int, string>? OnTextMessageEdited;
        public event Action<User, int>? OnUserJoinedToChat;
        public event Action<User, int>? OnUserLeavedTheChat;
        public event Action<int>? OnSuccessfullyLeavedTheChat;

        public Task<string> GetGoogleIdTokenAsync();
        public Task InitializeConnection(string serverBaseUrl);
        public Task StartAsync();
        public Task StopAsync();
        public HubConnectionState? GetConnectionState();
        public Task AuthorizeUser(string googleToken);
        public Task SendMessage(int senderId, int chatId, Message message);
        public Task<User?> GetUserInfo(int userId);
        public Task<List<Chat>?> GetUserChats(int userId);
        public Task<List<TextMessage>?> GetChatMessages(int chatId);
        public Task<User?> FindUserByEmail(string email);
        public Task CreateChatWithUser(int authorizedUserId, int companionUserId, string chatName);
        public Task LeaveChat(int senderId, int chatId);
        public Task InviteToChat(int userId, int chatId);
        public Task<bool> CheckUserInChat(int userId, int chatId);
        public Task<List<User>?> GetParticipantsFromChat(int chatId);
        public Task DeleteMessage(int chatId, int messageId);
        public Task EditMessage(int chatId, int messageId, string newText);
        public Task RemoveOldConnection(int userId);
    }
}
