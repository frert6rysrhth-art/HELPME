using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Chats
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public List<ApplicationUser> AllUsers { get; set; } = new();

        public async Task OnGetAsync()
        {
            AllUsers = await _userManager.Users.ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync(string chatName, List<string> selectedUserIds)
        {
            if (string.IsNullOrWhiteSpace(chatName))
            {
                ModelState.AddModelError("ChatName", "Название не может быть пустым.");
                AllUsers = await _userManager.Users.ToListAsync();
                return Page();
            }

            var chat = new Chat { Name = chatName };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();

            if (selectedUserIds != null)
            {
                foreach (var userId in selectedUserIds)
                {
                    _db.ChatParticipants.Add(new ChatParticipant
                    {
                        ChatId = chat.Id,
                        UserId = userId
                    });
                }
                await _db.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}