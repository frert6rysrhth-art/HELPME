namespace BelovskayaMonitoring.Models
{
    public class ChatPreview
    {
        public int ChatId { get; set; }
        public string ChatName { get; set; } = string.Empty;
        public bool IsSystem { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? LastSenderName { get; set; }
        public string? LastSenderEmail { get; set; }
        public string? LastMessageText { get; set; }
        public DateTime? LastMessageTime { get; set; }
    }
}