using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DMS.Migration.Web.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public IActionResult Index()
        {
            // If user is authenticated, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Dashboard");
            }
            // Otherwise show landing page
            return View("Landing");
        }

        [AllowAnonymous]
        public IActionResult Landing()
        {
            return View();
        }

        [Authorize]
        public IActionResult Dashboard()
        {
            // User is authenticated via [Authorize] attribute
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
