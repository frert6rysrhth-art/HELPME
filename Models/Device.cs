using System;
using System.ComponentModel.DataAnnotations;

namespace BelovskayaMonitoring.Models
{
    public enum DeviceStatus
    {
        Online,
        Offline,
        Warning
    }

    public class Device
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(45)]
        public string IPAddress { get; set; } = string.Empty;

        public DeviceStatus Status { get; set; } = DeviceStatus.Online;

        public DateTime? LastChecked { get; set; }
    }
}