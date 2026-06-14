using System;
using System.ComponentModel.DataAnnotations;

namespace BelovskayaMonitoring.Models
{
    public class LoadRecord
    {
        public int Id { get; set; }

        public int DeviceId { get; set; }
        public Device? Device { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Значение загрузки канала в процентах (0–100)
        /// </summary>
        [Range(0, 100)]
        public double Value { get; set; }
    }
}