using Microsoft.AspNetCore.Mvc;

namespace Auth.API.Controllers
{
    public class RunnerAuthController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
