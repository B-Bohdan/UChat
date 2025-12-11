using uchat.modelbase.Models.Messages;

namespace uchat.modelbase.Models
{
    public class Chat
    {
        public Chat()
        {
            Participants = new List<User>();
            Messages = new List<Message>();
        }

        public int Id { get; set; }
        public string Tag { get; set; } = null!;
        public virtual List<User>? Participants { get; set; } = new();
        public virtual List<Message>? Messages { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}
