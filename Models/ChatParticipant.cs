using BelovskayaMonitoring.Data;

namespace BelovskayaMonitoring.Models
{
	public class ChatParticipant
	{
		public int Id { get; set; }
		public int ChatId { get; set; }
		public Chat Chat { get; set; } = null!;
		public string UserId { get; set; } = string.Empty;
		public ApplicationUser User { get; set; } = null!;
	}
}