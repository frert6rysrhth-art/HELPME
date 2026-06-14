using System;
using System.ComponentModel.DataAnnotations;

namespace BelovskayaMonitoring.Models
{
    public class EventLog
    {
        public int Id { get; set; }

        public int DeviceId { get; set; }
        public Device? Device { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty; // "Авария", "Восстановление"

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
    }
}