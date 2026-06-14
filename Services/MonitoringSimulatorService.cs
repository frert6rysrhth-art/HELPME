using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Hubs;
using BelovskayaMonitoring.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Services
{
    public class MonitoringSimulatorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonitoringSimulatorService> _logger;
        private const int CommonChatId = 1; // ID чата "Дежурная смена"

        public MonitoringSimulatorService(IServiceScopeFactory scopeFactory, ILogger<MonitoringSimulatorService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

                    var devices = await db.Devices.ToListAsync(stoppingToken);
                    if (devices.Any())
                    {
                        var random = new Random();
                        var device = devices[random.Next(devices.Count)];

                        var oldStatus = device.Status;
                        device.Status = oldStatus == DeviceStatus.Online ? DeviceStatus.Offline : DeviceStatus.Online;
                        device.LastChecked = DateTime.UtcNow;

                        var eventType = device.Status == DeviceStatus.Offline ? "Авария" : "Восстановление";
                        var description = device.Status == DeviceStatus.Offline
                            ? $"Потеря связи с {device.Name} ({device.IPAddress})"
                            : $"Связь с {device.Name} ({device.IPAddress}) восстановлена";

                        var logEntry = new EventLog
                        {
                            DeviceId = device.Id,
                            EventType = eventType,
                            Timestamp = DateTime.UtcNow,
                            Description = description
                        };
                        db.EventLogs.Add(logEntry);

                        var message = new Message
                        {
                            ChatId = CommonChatId,
                            SenderId = null,
                            Text = $"⚠️ Система: {description}",
                            Timestamp = DateTime.UtcNow
                        };
                        db.Messages.Add(message);

                        await db.SaveChangesAsync(stoppingToken);

                        await hubContext.Clients.Group($"chat_{CommonChatId}").SendAsync("ReceiveMessage", new
                        {
                            messageId = message.Id,
                            senderName = "Система",
                            text = message.Text,
                            timestamp = message.Timestamp.ToString("HH:mm")
                        }, stoppingToken);

                        _logger.LogInformation("Эмуляция: изменён статус устройства {DeviceName} на {Status}", device.Name, device.Status);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в фоновом эмуляторе мониторинга");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
    }
}