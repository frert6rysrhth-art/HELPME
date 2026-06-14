using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BelovskayaMonitoring.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Users
{
    [Authorize(Roles = "Administrator")]
    public class EditModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EditModel(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [BindProperty(SupportsGet = true)]
        public string UserId { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;
        public List<string> UserRoles { get; set; } = new();
        public List<string> AllRoles { get; set; } = new();
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return RedirectToPage("./Index");

            UserEmail = user.Email ?? "без email";
            UserRoles = (await _userManager.GetRolesAsync(user)).ToList();
            AllRoles = _roleManager.Roles.Select(r => r.Name!).ToList();
            Position = user.Position ?? "";
            Department = user.Department ?? "";
            PhoneNumber = user.PhoneNumber ?? "";
            return Page();
        }

        public async Task<IActionResult> OnPostAsync([FromForm] List<string> selectedRoles,
            string position, string department, string phoneNumber)
        {
            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null) return RedirectToPage("./Index");

            // Обновляем дополнительные поля
            user.Position = position;
            user.Department = department;
            user.PhoneNumber = phoneNumber;
            await _userManager.UpdateAsync(user);

            // Обновляем роли
            var currentRoles = await _userManager.GetRolesAsync(user);
            var rolesToRemove = currentRoles.Except(selectedRoles).ToList();
            var rolesToAdd = selectedRoles.Except(currentRoles).ToList();

            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            return RedirectToPage("./Index");
        }
    }
}