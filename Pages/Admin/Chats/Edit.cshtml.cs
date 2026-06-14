using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public EditModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [BindProperty(SupportsGet = true)]
        public int ChatId { get; set; }

        public string ChatName { get; set; } = string.Empty;
        public List<ApplicationUser> CurrentParticipants { get; set; } = new();
        public List<ApplicationUser> AllUsers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var chat = await _db.Chats.FindAsync(ChatId);
            if (chat == null) return RedirectToPage("./Index");
            ChatName = chat.Name;

            CurrentParticipants = await _db.ChatParticipants
                .Where(cp => cp.ChatId == ChatId)
                .Include(cp => cp.User)
                .Select(cp => cp.User)
                .ToListAsync();

            var participantIds = CurrentParticipants.Select(u => u.Id).ToList();
            AllUsers = await _userManager.Users
                .Where(u => !participantIds.Contains(u.Id))
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddUserAsync(int chatId, string userId)
        {
            if (!string.IsNullOrEmpty(userId))
            {
                _db.ChatParticipants.Add(new ChatParticipant
                {
                    ChatId = chatId,
                    UserId = userId
                });
                await _db.SaveChangesAsync();
            }
            return RedirectToPage(new { chatId });
        }

        public async Task<IActionResult> OnPostRemoveAsync(int chatId, string userId)
        {
            var participant = await _db.ChatParticipants
                .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);
            if (participant != null)
            {
                _db.ChatParticipants.Remove(participant);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage(new { chatId });
        }
    }
}