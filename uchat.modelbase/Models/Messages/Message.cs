using System.Text.Json.Serialization;

namespace uchat.modelbase.Models.Messages
{
    [JsonDerivedType(typeof(TextMessage), typeDiscriminator: "Text")]
    public abstract class Message
    {
        public int Id { get; set; }
        public User Sender { get; set; } = null!;
        public bool IsEdited { get; set; }
        public DateTime SentAt { get; set; }
        public int ChatId { get; set; }
        public Chat ChatInstance { get; set; } = null!;
    }
}
