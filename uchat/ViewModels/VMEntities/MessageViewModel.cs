using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class MessageViewModel : WrapperViewModel<Message>
    {
        private UserViewModel? _senderVm;

        public MessageViewModel(Message model, UserViewModel? senderVm, int currentUserId) : base(model)
        {
            // Якщо передали VM користувача встановлюємо, якщо ні то ініціалізуємо на льоту
            _senderVm = senderVm ?? (model.Sender != null ? new UserViewModel(model.Sender) : null);
            IsMine = (model.Sender?.Id ?? 0) == currentUserId;
        }

        public UserViewModel? Sender
        {
            get => _senderVm;
            private set => Set(ref _senderVm, value);
        }

        public bool IsMine { get; }

        public DateTime SentAt => Model.SentAt;

        public string TimeDisplay
        {
            get
            {
                var utcDate = Model.SentAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(Model.SentAt, DateTimeKind.Utc)
                    : Model.SentAt;
                var localDate = utcDate.ToLocalTime();

                return localDate.ToString("t");
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
