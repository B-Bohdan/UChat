using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;
using uchat.Models;

namespace uchat_server.Hubs
{
    // + 1. Налаживаем подключение, когда пользователь делает это впервые (На стороне клиента)
    // + 1.1. Добавить возможность подключить клиент вручную по указаному адресу сервера через терминал
    // + 2. Наводим порядок в InitializeConnecion на клиенте, групируем все и дописываем недостающее
    // + 3. Добавляем методы для выгрузки данных чатов, и данных сообщений пагинацией

    // 5. Будет время, пишем обертку для модели данных на клиенте

    public class ChatHub : Hub
    {
        private readonly ApplicationContext _applicationContext;

        public ChatHub(ApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var userIdStr = httpContext!.Request.Query["userId"];

            if (int.TryParse(userIdStr, out int userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                var userChats = await _applicationContext.Chats
                    .Where(c => c.Participants.Any(p => p.Id == userId))
                    .Select(c => c.Id)
                    .ToListAsync();

                foreach (var chatId in userChats)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
                }
            }

            await base.OnConnectedAsync();
        }

        public async Task<List<Chat>> GetUserChats(int userId)
        {
            // Завантажуємо всі чати, в яких бере участь користувач
            var chats = await _applicationContext.Chats
                .Include(c => c.Participants)
                .Where(c => c.Participants.Any(p => p.Id == userId))
                .OrderByDescending(c => c.Messages.Max(m => m.SentAt)) // Сортуємо свіжі чати зверху
                .ToListAsync();
            return chats;
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

        // Видаляє його з групи підключень, щоб сервер більше не надсилав йому інформацію з цього чату
        public async Task LeaveChatGroup(int chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
        }

        #region Message Interactions
        public async Task SendTextMessageInChat(int senderId, int chatId, TextMessage textMessage)
        {
            var chat = await _applicationContext.Chats.FindAsync(chatId);
            var sender = await _applicationContext.Users.FindAsync(senderId);

            if (chat != null && sender != null)
            {
                textMessage.ChatInstance = chat;
                textMessage.Sender = sender;

                await _applicationContext.Messages.AddAsync(textMessage);
                await _applicationContext.SaveChangesAsync();

                await Clients.Group($"Chat_{chatId}").SendAsync("ReceivedMessage", textMessage);
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

        public async Task CheckUser(User user)
        {
            var userInSystem = await _applicationContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email); // Лучше async

            if (userInSystem == null)
            {
                User newUser = new User()
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    MiddleName = user.MiddleName,
                    Email = user.Email,
                    CreatedAt = DateTime.UtcNow.Date,
                    UserStatus = User.Status.Online
                };

                await _applicationContext.Users.AddAsync(newUser);
                await _applicationContext.SaveChangesAsync();

                await Clients.Caller.SendAsync("AuthorizationConfirmed", newUser);
            }
            else
            {
                await Clients.Caller.SendAsync("AuthorizationConfirmed", userInSystem);
            }
        }

        public async Task FindUserByEmail(string emailAddress)
        {
            var user = await _applicationContext.Users.FirstOrDefaultAsync(u => u.Email == emailAddress);
            await Clients.Caller.SendAsync("UserFound", user);
        }
        #endregion

        #region Chat Interactions
        public async Task CreateChatWith(int senderId, int companionId)
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
                Tag = $"{senderUser.FirstName} & {companionUser.FirstName}",
                Participants = new List<User> { senderUser, companionUser },
                CreatorId = senderId,
                Creator = senderUser
            };

            _applicationContext.Chats.Add(newChat);
            await _applicationContext.SaveChangesAsync();

            await Clients.Group($"User_{senderId}").SendAsync("YouAddedToChat", newChat);
            await Clients.Group($"User_{companionId}").SendAsync("YouAddedToChat", newChat);
        }

        public async Task CreateChat(int senderId, string tagName)
        {
            var creator = await _applicationContext.Users.FindAsync(senderId);

            if (creator == null)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "User not found");
                return;
            }

            Chat newChat = new Chat()
            {
                CreatedAt = DateTime.UtcNow,
                Tag = tagName,
                CreatorId = senderId,
                Creator = creator,
                Participants = new List<User> { creator }
            };

            _applicationContext.Chats.Add(newChat);
            await _applicationContext.SaveChangesAsync();

            await Clients.Caller.SendAsync("ChatCreated", newChat);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{newChat.Id}");
        }

        public async Task DeleteChat(int ownerId, int chatId)
        {
            var chat = await _applicationContext.Chats
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "Chat not found.");
                return;
            }

            if (chat.CreatorId != ownerId)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "Access denied.");
                return;
            }

            await Clients.Group($"Chat_{chatId}").SendAsync("ChatDeleted", chatId);

            _applicationContext.Chats.Remove(chat);
            await _applicationContext.SaveChangesAsync();
        }

        public async Task AddUserToChat(int senderId, int targetUserId, int chatId)
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

            if (chat.Participants.Any(p => p.Id == targetUserId))
            {
                await Clients.Caller.SendAsync("ErrorReceived", "User is already in the chat.");
                return;
            }

            chat.Participants.Add(targetUser);
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

            var participant = chat.Participants.FirstOrDefault(u => u.Id == senderId);
            if (participant == null)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "You are not in this chat.");
                return;
            }

            chat.Participants.Remove(participant);
            await _applicationContext.SaveChangesAsync();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
            await Clients.Group($"Chat_{chatId}").SendAsync("UserLeft", senderId);
        }

        public async Task RemoveFromChat(int ownerId, int targetUserId, int chatId)
        {
            var chat = await _applicationContext.Chats
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null) return;

            if (chat.CreatorId != ownerId)
            {
                await Clients.Caller.SendAsync("ErrorReceived", "Only owner can remove participants.");
                return;
            }

            if (ownerId == targetUserId) return;

            var victim = chat.Participants.FirstOrDefault(u => u.Id == targetUserId);
            if (victim == null) return;

            chat.Participants.Remove(victim);
            await _applicationContext.SaveChangesAsync();

            await Clients.Group($"Chat_{chatId}").SendAsync("UserKicked", targetUserId);

            // Повідомляємо користувачу, щоб він відписався через LeaveChatGroup
            await Clients.Group($"User_{targetUserId}").SendAsync("YouKickedFromChat", chatId);
        }
        #endregion
    }
}
