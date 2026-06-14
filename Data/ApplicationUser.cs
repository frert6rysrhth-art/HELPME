using Microsoft.AspNetCore.Identity;

namespace BelovskayaMonitoring.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarColor { get; set; }

        // Новые поля
        public string? Position { get; set; }   // Должность
        public string? Department { get; set; } // Отдел
    }
}