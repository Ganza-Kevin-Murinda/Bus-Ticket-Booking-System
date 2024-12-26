using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinalExam.Pages
{
    public class AdminDashboardModel : PageModel
    {
        public string? Username { get; set; }
        public int? Userid { get; set; }
        public string? Role { get; set; }

        public void OnGet()
        {
            // Retrieve session values
            Username = HttpContext.Session.GetString("userName");
            Userid = HttpContext.Session.GetInt32("userId");
            Role = HttpContext.Session.GetString("role");

            // Validate session values and redirect to SignIn if any are missing
            if (Userid == null || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Role))
            {
                Response.Redirect("/Signin");
                return;
            }
        }
    }
}
