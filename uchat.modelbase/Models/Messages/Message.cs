namespace uchat.modelbase.Models.Messages
{
    public abstract class Message
    {
        public int Id { get; set; }
        public User Sender { get; set; } = null!;
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public DateTime SentAt { get; set; }
        public int ChatId { get; set; }
        public Chat ChatInstance { get; set; } = null!;
    }
}
