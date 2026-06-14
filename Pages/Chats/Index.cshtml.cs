using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Hubs;
using BelovskayaMonitoring.Models;
using BelovskayaMonitoring.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Chats
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public IndexModel(ChatService chatService,
                          UserManager<ApplicationUser> userManager,
                          IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public List<ChatPreview> ChatPreviews { get; set; } = new();
        public Dictionary<int, string> UserAvatarColors { get; set; } = new();
        public bool IsAdmin { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            IsAdmin = User.IsInRole("Administrator");

            if (userId != null)
            {
                ChatPreviews = await _chatService.GetUserChatPreviewsAsync(userId);

                foreach (var preview in ChatPreviews)
                {
                    var hash = preview.ChatName.GetHashCode();
                    int r = (hash >> 16 & 0xFF) % 156 + 100;
                    int g = (hash >> 8 & 0xFF) % 156 + 100;
                    int b = (hash & 0xFF) % 156 + 100;
                    var color = $"#{r:X2}{g:X2}{b:X2}";
                    UserAvatarColors[preview.ChatId] = color;
                }
            }
        }

        public async Task<IActionResult> OnPostDeleteChatAsync(int chatId)
        {
            if (!User.IsInRole("Administrator"))
            {
                return RedirectToPage();
            }

            var (success, error) = await _chatService.DeleteChatAsync(chatId);
            if (!success)
            {
                TempData["StatusMessage"] = error;
            }
            else
            {
                // Оповещаем аналитику
                await _hubContext.Clients.All.SendAsync("RefreshAnalytics");
            }
            return RedirectToPage();
        }
    }
}