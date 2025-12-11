using uchat.modelbase.Models.Messages;
using uchat.ViewModels; // Чтобы видеть AppState

namespace uchat.ViewModels.VMEntities
{
    public class MessageViewModel : WrapperViewModel<Message>
    {
        private UserViewModel? _senderVm;

        public MessageViewModel(Message model, UserViewModel? senderVm) : base(model)
        {
            _senderVm = senderVm ?? (model.Sender != null ? new UserViewModel(model.Sender) : null);
        }

        // Статический метод-фабрика для создания правильного типа ViewModel
        public static MessageViewModel Create(Message model)
        {
            var senderVm = model.Sender != null ? new UserViewModel(model.Sender) : null;

            if (model is TextMessage textMessage)
            {
                return new TextMessageViewModel(textMessage, senderVm);
            }

            return new MessageViewModel(model, senderVm);
        }

        public UserViewModel? Sender
        {
            get => _senderVm;
            private set => Set(ref _senderVm, value);
        }

        // Глобальная проверка "Свое/Чужое"
        public bool IsMine => (Model.Sender?.Id ?? 0) == AppState.CurrentUserId;

        public DateTime SentAt => Model.SentAt;

        // Конвертация UTC -> Локальное время системы
        public string TimeDisplay
        {
            get
            {
                var utcDate = Model.SentAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(Model.SentAt, DateTimeKind.Utc)
                    : Model.SentAt;
                return utcDate.ToLocalTime().ToString("t");
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