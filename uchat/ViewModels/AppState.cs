using uchat.Services.IServices;
using uchat.ViewModels.Tools;
using uchat.ViewModels.VMEntities;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using uchat.Models;
using System.Collections.ObjectModel;

namespace uchat.ViewModels
{
    /// <summary>
    /// Зберігає глоабальні стани додатку, які не можно динамічно грузити з json конфігурації.
    /// </summary>
    public class AppState : ObservableObject
    {
        public AppState(IConnectionService connectionService, IDbContextFactory<ApplicationContext> contextFactory, IConfigurationService configurationService)
        {
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
    }
}
