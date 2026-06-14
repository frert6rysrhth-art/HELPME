using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Services;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public UsersController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        // GET api/users/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                user.Position,
                user.Department,
                user.AvatarColor,
                Roles = roles
            });
        }

        // GET api/users/chat/{chatId} – участники чата
        [HttpGet("chat/{chatId:int}")]
        public async Task<IActionResult> GetChatUsers(int chatId)
        {
            var participants = _db.ChatParticipants
                .Where(cp => cp.ChatId == chatId)
                .Select(cp => cp.User)
                .ToList();

            var result = participants.Select(u => new {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.AvatarColor,
                u.Position,
                u.Department
            });

            return Ok(result);
        }
    }
}