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
                if (Messages.Count > 0)
                {
                    var lastVm = Messages.Last();

                    if (lastVm is TextMessageViewModel txtVm)
                        return txtVm.Text;

                    return "Media message"; // Мабуть в майбутньому...
                }

                return "No messages yet";
            }
        }

        public void AddMessage(MessageViewModel messageVm)
        {

            // Если список пуст или сообщение новее последнего - добавляем в конец (оптимизация)
            if (Messages.Count == 0 || Messages.Last().SentAt <= messageVm.SentAt)
            {
                Messages.Add(messageVm);
            }
            // Если сообщение старее первого - добавляем в начало (оптимизация для подгрузки истории)
            else if (Messages.First().SentAt > messageVm.SentAt)
            {
                Messages.Insert(0, messageVm);
            }
            else
            {
                // Иначе ищем правильное место (от старых к новым)
                int index = 0;
                while (index < Messages.Count && Messages[index].SentAt < messageVm.SentAt)
                {
                    index++;
                }
                Messages.Insert(index, messageVm);
            }

            if (Model.Messages == null)
            {
                Model.Messages = new List<Message>();
            }

            if (!Model.Messages.Any(m => m.Id == messageVm.Model.Id))
            {
                Model.Messages.Add(messageVm.Model);
            }

            OnPropertyChanged(nameof(LastMessagePreview));
        }
    }
}