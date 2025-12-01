using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class TextMessageViewModel : MessageViewModel
    {
        // Приводимо нашу модель одразу до потрібного типу
        private TextMessage TypedModel => (TextMessage)Model;

        public TextMessageViewModel(TextMessage model, int currentUserId, UserViewModel? senderVm = null)
            : base(model, senderVm, currentUserId) { }

        public string Text
        {
            get => TypedModel.Text;
            set
            {
                if (TypedModel.Text != value)
                {
                    TypedModel.Text = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
