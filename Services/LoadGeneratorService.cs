using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Hubs;
using BelovskayaMonitoring.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Services
{
    public class LoadGeneratorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LoadGeneratorService> _logger;

        private readonly Dictionary<int, double> _lastValues = new();
        private readonly Random _random = new();

        public LoadGeneratorService(IServiceScopeFactory scopeFactory, ILogger<LoadGeneratorService> logger)
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
                    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<MonitoringHub>>();

                    var devices = await db.Devices.ToListAsync(stoppingToken);

                    foreach (var device in devices)
                    {
                        if (stoppingToken.IsCancellationRequested)
                            break;

                        double value;
                        if (device.Status == DeviceStatus.Offline)
                        {
                            value = 0.0;
                            _lastValues[device.Id] = 0.0;
                        }
                        else
                        {
                            double lastValue = _lastValues.TryGetValue(device.Id, out double v) ? v : 50.0;
                            double delta = (_random.NextDouble() * 10) - 5;
                            double newValue = lastValue + delta;
                            newValue = Math.Max(30, Math.Min(80, newValue));
                            value = Math.Round(newValue, 1);
                            _lastValues[device.Id] = value;
                        }

                        var now = DateTime.UtcNow;
                        var record = new LoadRecord
                        {
                            DeviceId = device.Id,
                            Timestamp = now,
                            Value = value
                        };
                        db.LoadRecords.Add(record);

                        // Обновляем LastChecked
                        device.LastChecked = now;

                        // Точка для графика (timestamp в UTC, клиент сам преобразует)
                        long unixMs = new DateTimeOffset(record.Timestamp).ToUnixTimeMilliseconds();
                        await hubContext.Clients.All.SendAsync("NewLoadPoint", new
                        {
                            deviceId = device.Id,
                            deviceName = device.Name,
                            timestamp = unixMs,
                            value = record.Value
                        }, stoppingToken);
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    // Рассылаем обновлённые карточки с локальным временем
                    foreach (var device in devices)
                    {
                        await hubContext.Clients.All.SendAsync("StatusUpdated", new
                        {
                            device.Id,
                            device.Name,
                            device.IPAddress,
                            Status = device.Status.ToString(),
                            LastChecked = device.LastChecked?.ToLocalTime().ToString("HH:mm:ss") ?? "—"
                        }, stoppingToken);
                    }

                    if (DateTime.UtcNow.Minute % 10 == 0)
                    {
                        await CleanOldRecords(db);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в генераторе данных загрузки");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private async Task CleanOldRecords(ApplicationDbContext db)
        {
            var cutoff = DateTime.UtcNow.AddHours(-1);
            var oldRecords = db.LoadRecords.Where(lr => lr.Timestamp < cutoff);
            db.LoadRecords.RemoveRange(oldRecords);
            await db.SaveChangesAsync();
        }
    }
}