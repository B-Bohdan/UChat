using uchat.modelbase.Models.Messages;
using uchat.ViewModels.VMEntities;

namespace uchat.Models.Factories
{
    public static class MessageFactory
    {
        public static MessageViewModel Create(Message model, UserViewModel? senderVm = null)
        {
            return model switch
            {
                TextMessage txt => new TextMessageViewModel(txt, senderVm),
                _ => throw new NotImplementedException($"Unknown message type: {model.GetType().Name}")
            };
        }
    }
}
