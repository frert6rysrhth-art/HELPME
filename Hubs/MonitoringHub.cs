using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using BelovskayaMonitoring.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Hubs
{
    public class MonitoringHub : Hub
    {
        private readonly ApplicationDbContext _db;

        public MonitoringHub(ApplicationDbContext db)
        {
            _db = db;
        }

        public override async Task OnConnectedAsync()
        {
            var devices = await _db.Devices
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IPAddress,
                    Status = d.Status.ToString(),
                    LastChecked = d.LastChecked.HasValue ? d.LastChecked.Value.ToLocalTime().ToString("HH:mm:ss") : "—"
                })
                .ToListAsync();

            await Clients.Caller.SendAsync("DevicesStatus", devices);
            await base.OnConnectedAsync();
        }
    }
}