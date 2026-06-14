using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Hubs;
using BelovskayaMonitoring.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            IHubContext<ChatHub> hubContext)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _hubContext = hubContext;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; } = string.Empty;

            [Required]
            [StringLength(50, MinimumLength = 1)]
            [Display(Name = "Имя")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [StringLength(50, MinimumLength = 1)]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "Пароль должен быть минимум {2} и максимум {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтверждение пароля")]
            [Compare("Password", ErrorMessage = "Пароль и подтверждение не совпадают.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    AvatarColor = GenerateColorFromEmail(Input.Email)
                };

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь создал новую учётную запись с паролем.");

                    if (!await _roleManager.RoleExistsAsync("User"))
                        await _roleManager.CreateAsync(new IdentityRole("User"));
                    await _userManager.AddToRoleAsync(user, "User");

                    var dbContext = HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                    var systemChats = dbContext.Chats.Where(c => c.IsSystem).ToList();
                    foreach (var chat in systemChats)
                    {
                        dbContext.ChatParticipants.Add(new ChatParticipant { ChatId = chat.Id, UserId = user.Id });
                    }
                    await dbContext.SaveChangesAsync();

                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Оповещаем аналитику
                    await _hubContext.Clients.All.SendAsync("RefreshAnalytics");

                    return LocalRedirect(returnUrl);
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private static string GenerateColorFromEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "#3366CC";
            int hash = email.GetHashCode();
            int r = (hash >> 16 & 0xFF) % 156 + 100;
            int g = (hash >> 8 & 0xFF) % 156 + 100;
            int b = (hash & 0xFF) % 156 + 100;
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
                throw new NotSupportedException("Требуется хранилище пользователей с поддержкой email.");
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}