using Microsoft.AspNetCore.Mvc;

namespace TicketSystem.Controllers
{
    public class StatsGraphsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
