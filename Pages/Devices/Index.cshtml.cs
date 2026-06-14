using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Devices
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public IndexModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public List<EventLog> RecentEvents { get; set; } = new();

        public async Task OnGetAsync()
        {
            RecentEvents = await _db.EventLogs
                .Include(e => e.Device)
                .OrderByDescending(e => e.Timestamp)
                .Take(20)
                .ToListAsync();
        }
    }
}