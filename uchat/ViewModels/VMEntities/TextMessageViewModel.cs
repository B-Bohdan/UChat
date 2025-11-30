using uchat.modelbase.Models.Messages;

namespace uchat.ViewModels.VMEntities
{
    public class TextMessageViewModel : MessageViewModel
    {
        // Приводимо нашу модель одразу до потрібного типу
        private TextMessage TypedModel => (TextMessage)Model;

        public TextMessageViewModel(TextMessage model, UserViewModel? senderVm = null)
            : base(model, senderVm) { }

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
