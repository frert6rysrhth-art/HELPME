using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BelovskayaMonitoring.Services;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var data = await _analyticsService.GetSummaryAsync();
            return Ok(data);
        }

        [HttpGet("hourlyload")]
        public async Task<IActionResult> GetHourlyLoad()
        {
            var data = await _analyticsService.GetHourlyLoadAsync();
            return Ok(data);
        }

        [HttpGet("topusers")]
        public async Task<IActionResult> GetTopUsers()
        {
            var data = await _analyticsService.GetTopUsersAsync();
            return Ok(data);
        }

        [HttpGet("messagetyperatio")]
        public async Task<IActionResult> GetMessageTypeRatio()
        {
            var data = await _analyticsService.GetMessageTypeRatioAsync();
            return Ok(data);
        }

        [HttpGet("recentactivity")]
        public async Task<IActionResult> GetRecentActivity()
        {
            var data = await _analyticsService.GetRecentActivityAsync();
            return Ok(data);
        }
    }
}