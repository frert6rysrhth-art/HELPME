using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Services;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatsApiController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatsApiController(ChatService chatService, UserManager<ApplicationUser> userManager)
        {
            _chatService = chatService;
            _userManager = userManager;
        }

        [HttpPost("{chatId}/pin")]
        public async Task<IActionResult> TogglePin(int chatId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var result = await _chatService.TogglePinChat(chatId, userId);
            if (result) return Ok(new { success = true });
            return BadRequest("Не удалось переключить закрепление.");
        }
    }
}