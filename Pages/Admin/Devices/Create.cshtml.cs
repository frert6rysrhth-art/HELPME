using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Devices
{
    [Authorize(Roles = "Administrator")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public CreateModel(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> OnPostAsync(string name, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(name)) return Page();

            _db.Devices.Add(new Device
            {
                Name = name,
                IPAddress = ipAddress
            });
            await _db.SaveChangesAsync();
            return RedirectToPage("./Index");
        }
    }
}