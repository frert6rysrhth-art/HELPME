using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Chats
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CreateModel(ChatService chatService, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        public List<ApplicationUser> AllUsers { get; set; } = new();

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            AllUsers = (await _userManager.Users.ToListAsync())
                .Where(u => u.Id != currentUserId)
                .ToList();
        }

        public async Task<IActionResult> OnPostAsync(string chatName, List<string> selectedUserIds)
        {
            if (string.IsNullOrWhiteSpace(chatName))
            {
                ModelState.AddModelError("ChatName", "Название не может быть пустым.");
                return Page();
            }

            var currentUserId = _userManager.GetUserId(User);
            var allUserIds = new List<string>();
            if (currentUserId != null)
                allUserIds.Add(currentUserId);
            if (selectedUserIds != null)
                allUserIds.AddRange(selectedUserIds);
            allUserIds = allUserIds.Distinct().ToList();

            await _chatService.CreateChat(chatName, allUserIds);
            return RedirectToPage("./Index");
        }
    }
}