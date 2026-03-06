using Microsoft.AspNetCore.Mvc;

namespace GamPI.Controllers
{
    public class DataController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
