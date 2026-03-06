using Microsoft.AspNetCore.Mvc;

namespace GamPI.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
