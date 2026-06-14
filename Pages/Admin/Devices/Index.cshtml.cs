using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Hubs;
using BelovskayaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Devices
{
    [Authorize(Roles = "Administrator")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IHubContext<MonitoringHub> _monitoringHub;

        public IndexModel(ApplicationDbContext db,
                          IHubContext<ChatHub> hubContext,
                          IHubContext<MonitoringHub> monitoringHub)
        {
            _db = db;
            _hubContext = hubContext;
            _monitoringHub = monitoringHub;
        }

        public List<Device> Devices { get; set; } = new();

        public async Task OnGetAsync()
        {
            Devices = await _db.Devices.ToListAsync();
        }

        public async Task<IActionResult> OnPostChangeStatusAsync(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device == null) return RedirectToPage();

            var systemChat = await _db.Chats.FirstOrDefaultAsync(c => c.IsSystem && c.Name == "Система");
            if (systemChat == null)
            {
                ModelState.AddModelError(string.Empty, "Системный чат не найден.");
                return Page();
            }

            var now = DateTime.UtcNow;
            var oldStatus = device.Status;
            device.Status = oldStatus == DeviceStatus.Online ? DeviceStatus.Offline : DeviceStatus.Online;
            device.LastChecked = now;

            var eventType = device.Status == DeviceStatus.Offline ? "Авария" : "Восстановление";
            var description = device.Status == DeviceStatus.Offline
                ? $"Потеря связи с {device.Name} ({device.IPAddress})"
                : $"Связь с {device.Name} ({device.IPAddress}) восстановлена";

            _db.EventLogs.Add(new EventLog
            {
                DeviceId = device.Id,
                EventType = eventType,
                Timestamp = now,
                Description = description
            });

            var message = new Message
            {
                ChatId = systemChat.Id,
                SenderId = null,
                Text = $"⚠️ Система: {description}",
                Timestamp = now
            };
            _db.Messages.Add(message);
            await _db.SaveChangesAsync();

            // Мониторинг
            await _monitoringHub.Clients.All.SendAsync("StatusUpdated", new
            {
                device.Id,
                device.Name,
                device.IPAddress,
                Status = device.Status.ToString(),
                LastChecked = device.LastChecked?.ToLocalTime().ToString("HH:mm:ss") ?? "—"
            });

            // Чат
            await _hubContext.Clients.Group($"chat_{systemChat.Id}").SendAsync("ReceiveMessage", new
            {
                messageId = message.Id,
                senderName = "Система",
                text = message.Text,
                timestamp = message.Timestamp.ToLocalTime().ToString("HH:mm")
            });

            // Оповещаем всех об изменении данных
            await _hubContext.Clients.All.SendAsync("NewMessage");
            await _hubContext.Clients.All.SendAsync("RefreshAnalytics");

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device != null)
            {
                _db.Devices.Remove(device);
                await _db.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}