namespace uchat.modelbase.Models.Messages
{
    // кружочеееек
    // Пока в долгий ящик, сначала делаем текстовые сообщения, и налаживаем общую работу :)
    public class VideoMessage : Message
    {
        // File reference on the server
        public string VideoUrl { get; set; } = string.Empty;

        // Preview image reference
        public string ThumbnailUrl { get; set; } = string.Empty;

        // Duration of the video
        public TimeSpan Duration { get; set; }
    }
}
