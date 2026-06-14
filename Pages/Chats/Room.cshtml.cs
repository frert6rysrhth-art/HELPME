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
    public class RoomModel : PageModel
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public RoomModel(ChatService chatService,
                         UserManager<ApplicationUser> userManager,
                         IHubContext<ChatHub> hubContext)
        {
            _chatService = chatService;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        public Chat? Chat { get; set; }
        public List<Message> Messages { get; set; } = new();
        [TempData]
        public string? StatusMessage { get; set; }
        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(int chatId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            bool isParticipant = await _chatService.IsParticipant(chatId, userId);
            if (!isParticipant) return RedirectToPage("/Chats/Index");

            var chats = await _chatService.GetUserChats(userId);
            Chat = chats.FirstOrDefault(c => c.Id == chatId);
            if (Chat == null) return RedirectToPage("/Chats/Index");

            Messages = await _chatService.GetChatMessages(chatId);
            IsAdmin = User.IsInRole("Administrator");
            return Page();
        }

        public async Task<IActionResult> OnPostLeaveAsync(int chatId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return RedirectToPage("/Account/Login", new { area = "Identity" });

            var (success, error) = await _chatService.LeaveChat(chatId, userId);
            if (!success)
            {
                StatusMessage = error;
                return RedirectToPage(new { chatId });
            }

            // Оповещаем аналитику о возможных изменениях
            await _hubContext.Clients.All.SendAsync("RefreshAnalytics");

            return RedirectToPage("/Chats/Index");
        }

        public async Task<IActionResult> OnPostClearHistoryAsync(int chatId)
        {
            if (!User.IsInRole("Administrator"))
            {
                StatusMessage = "Только администратор может очистить историю.";
                return RedirectToPage(new { chatId });
            }

            var (success, error) = await _chatService.ClearChatHistoryAsync(chatId);
            StatusMessage = success ? "История чата очищена." : error;

            if (success)
            {
                // Оповещаем аналитику
                await _hubContext.Clients.All.SendAsync("RefreshAnalytics");
            }

            return RedirectToPage(new { chatId });
        }
    }
}