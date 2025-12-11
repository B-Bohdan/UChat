using System.Collections.ObjectModel;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class ChatViewModel : WrapperViewModel<Chat>
    {
        public ChatViewModel(Chat model) : base(model)
        {
            Messages = new ObservableCollection<MessageViewModel>();
            Participants = new ObservableCollection<UserViewModel>(
                model.Participants!.Select(u => new UserViewModel(u))
            );
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
            // Додаємо до візуальної коллекції
            Messages.Add(messageVm);

            // Додаємо до моделі
            if (!Model.Messages!.Contains(messageVm.Model))
            {
                Model.Messages.Add(messageVm.Model);
            }

            // Повідомляємо UI, що останнє повідомлення змінилось
            OnPropertyChanged(nameof(LastMessagePreview));
        }
    }
}
