namespace BelovskayaMonitoring.Models
{
    public class UserChatPin
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        public int ChatId { get; set; }
        public Chat? Chat { get; set; }
    }
}