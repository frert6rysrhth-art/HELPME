using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Chats
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<ChatViewModel> Chats { get; set; } = new();
        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            Chats = await _db.Chats
                .Include(c => c.Participants)
                .Select(c => new ChatViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParticipantCount = c.Participants.Count
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int chatId)
        {
            var chat = await _db.Chats.FindAsync(chatId);
            if (chat != null)
            {
                _db.Chats.Remove(chat);
                await _db.SaveChangesAsync();
                StatusMessage = $"„ат '{chat.Name}' удалЄн.";
            }
            return RedirectToPage();
        }

        public class ChatViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int ParticipantCount { get; set; }
        }
    }
}