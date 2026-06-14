using BelovskayaMonitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Services
{
    public class AnalyticsService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnalyticsService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Общая сводка
        public async Task<object> GetSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var totalUsers = await _db.Users.CountAsync();
            var activeUsers = await _db.Messages
                .Where(m => m.Timestamp >= now.AddMinutes(-15))
                .Select(m => m.SenderId)
                .Distinct()
                .CountAsync();
            var totalChats = await _db.Chats.CountAsync();
            var systemChats = await _db.Chats.CountAsync(c => c.IsSystem);
            var messagesToday = await _db.Messages
                .Where(m => m.Timestamp >= todayStart)
                .CountAsync();
            var alertsToday = await _db.EventLogs
                .Where(e => e.Timestamp >= todayStart && e.EventType == "Авария")
                .CountAsync();

            return new
            {
                totalUsers,
                activeUsers,
                totalChats,
                systemChats,
                messagesToday,
                alertsToday
            };
        }

        // График нагрузки (сообщения в час за последние 24 часа)
        public async Task<List<object>> GetHourlyLoadAsync()
        {
            var since = DateTime.UtcNow.AddHours(-24);
            var messages = await _db.Messages
                .Where(m => m.Timestamp >= since)
                .Select(m => new { m.Timestamp, IsSystem = m.SenderId == null })
                .ToListAsync();

            var hourly = messages
                .GroupBy(m => new DateTime(m.Timestamp.Year, m.Timestamp.Month, m.Timestamp.Day, m.Timestamp.Hour, 0, 0, DateTimeKind.Utc))
                .Select(g => new
                {
                    Hour = g.Key,
                    Total = g.Count(),
                    System = g.Count(x => x.IsSystem),
                    User = g.Count(x => !x.IsSystem)
                })
                .OrderBy(x => x.Hour)
                .ToList();

            var result = new List<object>();
            foreach (var h in hourly)
            {
                result.Add(new
                {
                    hour = h.Hour.ToString("yyyy-MM-dd HH:mm"),
                    total = h.Total,
                    system = h.System,
                    user = h.User
                });
            }
            return result;
        }

        // Топ-10 активных пользователей за всё время
        public async Task<List<object>> GetTopUsersAsync(int count = 10)
        {
            // Получаем топ отправителей (группировка по SenderId)
            var topSenders = await _db.Messages
                .Where(m => m.SenderId != null)
                .GroupBy(m => m.SenderId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToListAsync();

            var result = new List<object>();
            foreach (var sender in topSenders)
            {
                // Для каждого ID получаем данные пользователя через UserManager
                var user = await _userManager.FindByIdAsync(sender.UserId!);
                if (user != null)
                {
                    result.Add(new
                    {
                        name = $"{user.FirstName} {user.LastName}".Trim(),
                        email = user.Email,
                        count = sender.Count
                    });
                }
            }

            return result;
        }

        // Круговая диаграмма: обычные vs системные
        public async Task<object> GetMessageTypeRatioAsync()
        {
            var total = await _db.Messages.CountAsync();
            var system = await _db.Messages.CountAsync(m => m.SenderId == null);
            return new { total, system, user = total - system };
        }

        // Журнал последних событий (сообщения + события)
        public async Task<List<object>> GetRecentActivityAsync(int count = 20)
        {
            var recentMessages = await _db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Chat)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .Select(m => new
                {
                    Time = m.Timestamp,
                    Type = m.SenderId == null ? "Системное сообщение" : "Сообщение",
                    Description = m.SenderId == null
                        ? m.Text
                        : $"{m.Sender!.FirstName} {m.Sender.LastName} в чате «{m.Chat!.Name}»: {m.Text}"
                })
                .ToListAsync();

            var recentEvents = await _db.EventLogs
                .Include(e => e.Device)
                .OrderByDescending(e => e.Timestamp)
                .Take(count)
                .Select(e => new
                {
                    Time = e.Timestamp,
                    Type = "Событие",
                    Description = $"{e.EventType}: {e.Description} (устройство: {e.Device!.Name})"
                })
                .ToListAsync();

            var combined = recentMessages
                .Concat(recentEvents)
                .OrderByDescending(x => x.Time)
                .Take(count)
                .Select(x => new
                {
                    time = x.Time.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                    type = x.Type,
                    description = x.Description
                })
                .ToList<object>();

            return combined;
        }
    }
}