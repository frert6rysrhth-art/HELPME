using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BelovskayaMonitoring.Data;
using BelovskayaMonitoring.Models;
using System.Threading.Tasks;

namespace BelovskayaMonitoring.Pages.Admin.Devices
{
    [Authorize(Roles = "Administrator")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _db;

        public EditModel(ApplicationDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        public Device Device { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var device = await _db.Devices.FindAsync(id);
            if (device == null) return RedirectToPage("./Index");
            Device = device;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            var deviceInDb = await _db.Devices.FindAsync(Device.Id);
            if (deviceInDb == null) return RedirectToPage("./Index");

            deviceInDb.Name = Device.Name;
            deviceInDb.IPAddress = Device.IPAddress;
            await _db.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}