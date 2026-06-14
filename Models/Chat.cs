using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BelovskayaMonitoring.Models
{
    public class Chat
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsSystem { get; set; } = false; // <-- новое поле

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}