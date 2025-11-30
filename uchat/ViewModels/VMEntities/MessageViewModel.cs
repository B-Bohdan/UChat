using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class MessageViewModel : WrapperViewModel<Message>
    {
        private UserViewModel? _senderVm;

        public MessageViewModel(Message model, UserViewModel? senderVm = null) : base(model)
        {
            // Якщо передали VM користувача встановлюємо, якщо ні то ініціалізуємо на льоту
            _senderVm = senderVm ?? (model.Sender != null ? new UserViewModel(model.Sender) : null);
        }

        public UserViewModel? Sender
        {
            get => _senderVm;
            private set => Set(ref _senderVm, value);
        }

        public DateTime SentAt => Model.SentAt;

        public string TimeDisplay => Model.SentAt.ToLocalTime().ToString("t");

        public bool IsRead
        {
            get => Model.IsRead;
            set
            {
                if (Model.IsRead != value)
                {
                    Model.IsRead = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsEdited
        {
            get => Model.IsEdited;
            set
            {
                if (Model.IsEdited != value)
                {
                    Model.IsEdited = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
