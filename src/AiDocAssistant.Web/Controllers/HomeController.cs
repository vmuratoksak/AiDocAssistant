using Microsoft.AspNetCore.Mvc;

namespace AiDocAssistant.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
