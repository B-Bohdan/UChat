using Microsoft.EntityFrameworkCore;
using uchat.modelbase.Models;
using uchat.modelbase.Models.Messages;

namespace uchat.Models
{
    /// <summary>
    /// Представляє базу даних застосунку. Object relational mapping (ORM) контекст для взаємодії з базою даних.
    /// </summary>
    public class ApplicationContext : DbContext
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options)
            : base(options)
        {
            Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Chat> Chats { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<TextMessage> TextMessages { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Налаштування User
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.UserStatus).HasConversion<string>();
            });

            // налаштування Chat та зв'язків
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.HasMany(c => c.Participants)
                      .WithMany(u => u.Chats)      
                      .UsingEntity(j => j.ToTable("ChatParticipants")); // Говоримо EF явно назвати проміжну таблицю "ChatParticipants"
                
                entity.HasOne(c => c.Creator)
                  .WithMany()             
                  .HasForeignKey(c => c.CreatorId)
                  .OnDelete(DeleteBehavior.Restrict); 

            });

            // Налаштування Message
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.Id);

                // Зв'язок Повідомлення -> Чат
                entity.HasOne(m => m.ChatInstance)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(m => m.ChatId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Зв'язок Повідомлення -> Відправник (Користувач)
                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .OnDelete(DeleteBehavior.Restrict);

                // Наслідування для типів повідомлень
                entity.HasDiscriminator<string>("MessageType")
                      .HasValue<TextMessage>("Text")
                      .HasValue<Message>("Base");
            });

            // Налаштування TextMessage
            modelBuilder.Entity<TextMessage>(entity =>
            {
                entity.Property(t => t.Text).IsRequired();
            });
        }
    }
}
