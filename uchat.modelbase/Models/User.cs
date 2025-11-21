namespace uchat.modelbase.Models
{
    public class User
    {
        public enum Status
        {
            Online,
            DontDisturb,
            Idle,
            Invisible
        }

        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string MiddleName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public Status UserStatus { get; set; }
        public List<Chat> Chats { get; set; } = new();
    }
}
