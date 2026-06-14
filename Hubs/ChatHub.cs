using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace BelovskayaMonitoring.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = _userManager.GetUserId(Context.User);
            if (userId != null)
            {
                var chatIds = await _db.ChatParticipants
                    .Where(cp => cp.UserId == userId)
                    .Select(cp => cp.ChatId)
                    .ToListAsync();

                foreach (var chatId in chatIds)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{chatId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(int chatId, string text)
        {
            var userId = _userManager.GetUserId(Context.User);
            var user = await _userManager.FindByIdAsync(userId);
            var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "Неизвестный";
            if (string.IsNullOrWhiteSpace(userName))
                userName = user?.Email ?? "Неизвестный";

            bool isParticipant = await _db.ChatParticipants
                .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

            if (!isParticipant)
            {
                await Clients.Caller.SendAsync("Error", "Вы не являетесь участником этого чата.");
                return;
            }

            var chat = await _db.Chats.FindAsync(chatId);
            if (chat != null && chat.IsSystem && chat.Name == "Система")
            {
                await Clients.Caller.SendAsync("Error", "В этом чате может писать только система.");
                return;
            }

            var message = new Message
            {
                ChatId = chatId,
                SenderId = userId,
                Text = text,
                Timestamp = DateTime.UtcNow
            };
            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            var senderColor = user?.AvatarColor ?? "#3366CC";
            await Clients.Group($"chat_{chatId}").SendAsync("ReceiveMessage", new
            {
                messageId = message.Id,
                senderId = userId,
                senderName = userName,
                senderEmail = user?.Email ?? "",
                senderColor = senderColor,
                text = message.Text,
                timestamp = message.Timestamp.ToLocalTime().ToString("HH:mm")
            });

            // Оповещаем всех об изменении данных (для чатов)
            await Clients.All.SendAsync("NewMessage");

            // Оповещаем аналитику
            await Clients.All.SendAsync("RefreshAnalytics");
        }
    }
}