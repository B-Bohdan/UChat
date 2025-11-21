using uchat.modelbase.Models.Messages;

namespace uchat.modelbase.Models
{
    public class Chat
    {
        public int Id { get; set; }
        public string Tag { get; set; } = null!;
        public int FirstUserId { get; set; }
        public User FirstUser { get; set; } = null!;
        public int SecondUserId { get; set; }
        public User SecondUser { get; set; } = null!;
        public double DialogTemperature { get; set; }
        public List<Message> Messages { get; set; } = new();
    }
}
