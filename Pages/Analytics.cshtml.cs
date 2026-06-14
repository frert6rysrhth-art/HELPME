using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BelovskayaMonitoring.Pages
{
    [Authorize]
    public class AnalyticsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}