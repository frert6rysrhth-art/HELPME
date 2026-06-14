using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace BelovskayaMonitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ExportController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ExportController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("events")]
        public async Task<IActionResult> ExportEvents()
        {
            var events = await _db.EventLogs
                .Include(e => e.Device)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => new
                {
                    Время = e.Timestamp.ToString("dd.MM.yyyy HH:mm:ss"),
                    Устройство = e.Device != null ? e.Device.Name : "—",
                    Событие = e.EventType,
                    Описание = e.Description
                })
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Журнал событий");
                ws.Cell(1, 1).Value = "Время";
                ws.Cell(1, 2).Value = "Устройство";
                ws.Cell(1, 3).Value = "Событие";
                ws.Cell(1, 4).Value = "Описание";
                ws.Range(1, 1, 1, 4).Style.Font.Bold = true;

                for (int i = 0; i < events.Count; i++)
                {
                    ws.Cell(i + 2, 1).Value = events[i].Время;
                    ws.Cell(i + 2, 2).Value = events[i].Устройство;
                    ws.Cell(i + 2, 3).Value = events[i].Событие;
                    ws.Cell(i + 2, 4).Value = events[i].Описание;
                }

                ws.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(
                        stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "events.xlsx"
                    );
                }
            }
        }

        [HttpGet("chat/{chatId:int}")]
        public async Task<IActionResult> ExportChat(int chatId)
        {
            var chatService = new ChatService(_db);
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var text = await chatService.ExportChatAsTextAsync(chatId, userId);
            if (text == null) return NotFound("Чат не найден или нет доступа.");

            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            return File(bytes, "text/plain", $"chat_{chatId}.txt");
        }
    }
}