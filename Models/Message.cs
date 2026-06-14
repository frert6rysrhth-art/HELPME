using System;
using System.ComponentModel.DataAnnotations;
using BelovskayaMonitoring.Data;

namespace BelovskayaMonitoring.Models
{
	public class Message
	{
		public int Id { get; set; }
		public int ChatId { get; set; }
		public Chat Chat { get; set; } = null!;
		public string? SenderId { get; set; }
		public ApplicationUser? Sender { get; set; }
		[Required]
		[MaxLength(2000)]
		public string Text { get; set; } = string.Empty;
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
	}
}