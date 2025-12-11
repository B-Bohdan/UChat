using System.Collections.ObjectModel;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class ChatViewModel : WrapperViewModel<Chat>
    {
        public ChatViewModel(Chat model) : base(model)
        {
            Participants = new ObservableCollection<UserViewModel>(
                model.Participants!.Select(u => new UserViewModel(u))
            );

            Messages = new ObservableCollection<MessageViewModel>();

            // Если в модели есть сообщения, создаем для них VM
            if (model.Messages != null)
            {
                var sortedMessages = model.Messages.OrderBy(m => m.SentAt);
                foreach (var msg in sortedMessages)
                {
                    // Фабрика сама разберется с типом и ID юзера
                    Messages.Add(MessageViewModel.Create(msg));
                }
            }
        }

        public ObservableCollection<MessageViewModel> Messages { get; }
        public ObservableCollection<UserViewModel> Participants { get; }

        public string Tag
        {
            get => Model.Tag;
            set
            {
                if (Model.Tag != value)
                {
                    Model.Tag = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastMessagePreview
        {
            get
            {
                if (Model.Messages == null || !Model.Messages.Any())
                    return "No messages yet";

                var lastMsg = Model.Messages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                if (lastMsg is TextMessage txt) return txt.Text;
                return "No messages yet";
            }
        }

        public void AddMessage(MessageViewModel messageVm)
        {
            // В UI
            Messages.Add(messageVm);

            // В Модель (с защитой от null)
            if (Model.Messages == null)
            {
                Model.Messages = new List<Message>();
            }

            if (!Model.Messages.Contains(messageVm.Model))
            {
                Model.Messages.Add(messageVm.Model);
            }

            OnPropertyChanged(nameof(LastMessagePreview));
        }
    }
}