using Google.Apis.Auth;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;
using uchat.Models;

namespace uchat_server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationContext _applicationContext;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ApplicationContext applicationContext, ILogger<ChatHub> logger)
        {
            _applicationContext = applicationContext;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public async Task<User?> GetUserInfoAsync(int userId)
        {
            User? user = await _applicationContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return user;
        }

        // Метод получения списка чатов (Только с последним сообщением)
        public async Task<List<Chat>> GetUserChats(int userId)
        {
            var chats = await _applicationContext.Chats
                .Include(c => c.Participants)
                .Where(c => c.Participants!.Any(p => p.Id == userId))
                .Select(c => new Chat
                {
                    Id = c.Id,
                    Tag = c.Tag,
                    CreatedAt = c.CreatedAt,
                    Participants = c.Participants,

                    // ИСПРАВЛЕНИЕ ТУТ:
                    Messages = c.Messages!
                                .OfType<TextMessage>()    // Фильтруем только TextMessage (EF это умеет)
                                .OrderByDescending(m => m.SentAt)
                                .Take(1)
                                .Cast<Message>()          // Приводим обратно к Message для списка
                                .ToList()
                })
                .ToListAsync();

            return chats;
        }

        // Новый метод для получения истории (пагинация пока не реализована, грузим N последних)
        public async Task<List<TextMessage>> GetChatMessages(int chatId)
        {
            var messages = await _applicationContext.TextMessages
                .Where(m => m.ChatId == chatId)
                .Include(m => m.Sender)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
            return messages;
        }

        public async Task<List<User>?> GetParticipantsList(int chatId)
        {
            var chat = _applicationContext.Chats
                .Include(c => c.Participants)
                .FirstOrDefault(c => c.Id == chatId);

            if (chat == null)
                return null;

            return chat.Participants;
        }

        public async Task<List<Message>> GetChatHistory(int chatId, int skip, int take = 20)
        {
            // Отримання повідомлень для конкретного чату
            var messages = await _applicationContext.Messages
                .Include(m => m.Sender)
                .Where(m => m.ChatId == chatId)
                // Сортуємо в зворотньому порядку, щоб отримати останні повідомлення
                .OrderByDescending(m => m.SentAt)

                // Пагінація
                .Skip(skip) // Пропускаємо ті повідомлення, які ми вже завантажували
                .Take(take) // Беремо по 20 повідомлень за запит
                .ToListAsync();

            messages.Reverse();

            return messages;
        }

        /* Методи які кліент викликає після того як його додали або видалили з чату */

        // Додає його в групу підключень, щоб сервер міг надсилати йому інформацію з цього чату
        public async Task JoinChatGroup(int chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }

        // Видаляє його з групи підключень
        public async Task RemoveFromConnectionGroup(int userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

            var userChats = await _applicationContext.Chats
                    .Where(c => c.Participants!.Any(p => p.Id == userId))
                    .Select(c => c.Id)
                    .ToListAsync();

            foreach (var chatId in userChats)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
            }
        }

        #region Message Interactions
        public async Task SendTextMessageInChat(int senderId, int chatId, TextMessage textMessage)
        {
            var chat = await _applicationContext.Chats.FindAsync(chatId);
            var sender = await _applicationContext.Users.FindAsync(senderId);

            if (chat != null && sender != null)
            {
                textMessage.ChatInstance = chat;
                textMessage.ChatId = chatId;
                textMessage.Sender = sender;
                textMessage.SentAt = DateTime.UtcNow;

                textMessage.Id = 0;

                await _applicationContext.Messages.AddAsync(textMessage);
                await _applicationContext.SaveChangesAsync();

                var messageToSend = new TextMessage
                {
                    Id = textMessage.Id,
                    Text = textMessage.Text,
                    SentAt = textMessage.SentAt,
                    ChatId = chatId,
                    IsEdited = textMessage.IsEdited,

                    Sender = new User
                    {
                        Id = sender.Id,
                        Email = sender.Email,
                        FirstName = sender.FirstName,
                        LastName = sender.LastName
                    },
                    ChatInstance = null!
                };

                await Clients.Group($"Chat_{chatId}").SendAsync("ReceivedMessage", messageToSend, chat.Id);
            }
            else
            {
                await Clients.Group($"Chat_{chatId}").SendAsync("ErrorReceived", "Chat or sender is null");
            }
        }

        public async Task DeleteMessage(int messageId, int chatId)
        {
            await _applicationContext.Messages
                .Where(m => m.Id == messageId)
                .ExecuteDeleteAsync();

            await Clients.Group($"Chat_{chatId}").SendAsync("MessageDeleted", messageId, chatId);
        }

        public async Task EditTextMessage(int messageId, int chatId, string text)
        {
            await _applicationContext.TextMessages
                .Where(m => m.Id == messageId)
                .ExecuteUpdateAsync(m => m
                    .SetProperty(t => t.Text, text)
                    .SetProperty(t => t.IsEdited, true));

            await Clients.Group($"Chat_{chatId}").SendAsync("TextMessageEdited", messageId, chatId, text);
        }
        #endregion

        #region User Interactions

        // Авторизує користувача коли він реєструється, логінится та просто заходить
        public async Task AuthorizeUser(string googleToken)
        {
            try
            {
                // Валідація токена
                var payload = await GoogleJsonWebSignature.ValidateAsync(googleToken);

                // Отримаємо дані
                string safeEmail = payload.Email;
                string safeFirstName = payload.GivenName;
                string safeLastName = payload.FamilyName;

                // Шукаємо користувача в системі
                var userInSystem = await _applicationContext.Users
                    .FirstOrDefaultAsync(u => u.Email == safeEmail);

                // Якщо немає, то створюємо його на основі отриманих даних
                if (userInSystem == null)
                {
                    User newUser = new User()
                    {
                        FirstName = safeFirstName,
                        LastName = safeLastName,
                        Email = safeEmail,
                        CreatedAt = DateTime.UtcNow.Date,
                    };

                    await _applicationContext.Users.AddAsync(newUser);
                    await _applicationContext.SaveChangesAsync();

                    // Додаємо до групи підключень
                    await AddConnectionContextToGroup(Context, newUser.Id);

                    await Clients.Caller.SendAsync("AuthorizationConfirmed", newUser);
                }
                else
                {
                    //userInSystem.UserStatus = User.Status.Online;
                    //await _applicationContext.SaveChangesAsync();

                    // Додаємо до групи підключень
                    await AddConnectionContextToGroup(Context, userInSystem.Id);

                    await Clients.Caller.SendAsync("AuthorizationConfirmed", userInSystem);
                }
            }
            catch (InvalidJwtException ex)
            {
                // Токен не валідний, потрібна переавторизація або була спроба підробки
                await Clients.Caller.SendAsync("AuthorizationError", $"Ошибка валидации Google токена: {ex.Message}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.SendAsync("AuthorizationError", $"Внутренняя ошибка сервера: {ex.Message}");
            }
        }

        private async Task AddConnectionContextToGroup(HubCallerContext context, int userId)
        {
            await Groups.AddToGroupAsync(context.ConnectionId, $"User_{userId}");

            var userChats = await _applicationContext.Chats
                    .Where(c => c.Participants!.Any(p => p.Id == userId))
                    .Select(c => c.Id)
                    .ToListAsync();

            foreach (var chatId in userChats)
            {
                await Groups.AddToGroupAsync(context.ConnectionId, $"Chat_{chatId}");
            }

            //await Clients.Group($"Chat_{userChats.First()}").SendAsync("ErrorReceived", "Hello World");
        }

        public async Task<User?> FindUserByEmail(string emailAddress)
        {
            var user = await _applicationContext.Users.FirstOrDefaultAsync(u => u.Email == emailAddress);
            return user;
        }
        #endregion

        #region Chat Interactions
        public async Task CreateChatWith(int senderId, int companionId, string chatName)
        {
            var senderUser = await _applicationContext.Users.FindAsync(senderId);
            var companionUser = await _applicationContext.Users.FindAsync(companionId);

            if (senderUser == null || companionUser == null)
            {
                await Clients.Caller.SendAsync("ChatCreationFailed", "One or both users not found.");
                return;
            }

            Chat newChat = new Chat()
            {
                CreatedAt = DateTime.UtcNow,
                Tag = chatName,
                Participants = new List<User> { senderUser, companionUser },
            };

            _applicationContext.Chats.Add(newChat);
            await _applicationContext.SaveChangesAsync();

            await Clients.Group($"User_{senderId}").SendAsync("YouAddedToChat", newChat);
            await Clients.Group($"User_{companionId}").SendAsync("YouAddedToChat", newChat);
        }

        public async Task AddUserToChat(int targetUserId, int chatId)
        {
            var chat = await _applicationContext.Chats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            var targetUser = await _applicationContext.Users.FindAsync(targetUserId);

            if (chat == null || targetUser == null)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "Chat or User not found.");
                return;
            }

            if (chat.Participants!.Any(p => p.Id == targetUserId))
            {
                await Clients.Caller.SendAsync("ErrorReceived", "User is already in the chat.");
                return;
            }

            chat.Participants!.Add(targetUser);
            await _applicationContext.SaveChangesAsync();

            await Clients.Group($"Chat_{chatId}").SendAsync("UserJoined", targetUser, chatId);

            // Повідомляємо користувачу, щоб він підписався через JoinChatGroup
            await Clients.Group($"User_{targetUserId}").SendAsync("YouAddedToChat", chat);
        }

        public async Task LeaveChat(int senderId, int chatId)
        {
            var chat = await _applicationContext.Chats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return;

            var participant = chat.Participants!.FirstOrDefault(u => u.Id == senderId);
            if (participant == null)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "You are not in this chat.");
                return;
            }

            chat.Participants!.Remove(participant);

            if(chat.Participants!.Count < 1)
                _applicationContext.Chats.Remove(chat);

            await _applicationContext.SaveChangesAsync();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
            await Clients.Group($"Chat_{chatId}").SendAsync("UserLeft", senderId);
            await Clients.Groups($"User_{senderId}").SendAsync("LeavedSuccessfully", chatId);
        }

        public async Task<bool> CheckUserInChat(int userId, int chatId)
        {
            using (_applicationContext)
            {
                var chat = _applicationContext.Chats
                    .Include (c => c.Participants)
                    .FirstOrDefault(c => c.Id == chatId);
                
                if (chat == null) 
                    return false;

                return chat.Participants!.Any(u => u.Id == userId);
            }
        }
        #endregion
    }
}
