using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers
{
    // Sadece şoförler
    [Authorize(Roles = Roles.Driver)]
    [Route("driver")]                   // /driver
    public class DriverUiController : Controller
    {
        [HttpGet("myroute")]            // GET /driver/myroute
        public IActionResult MyRoute()
            // View dosyasını açık yol ile veriyoruz, taşımaya gerek yok:
            => View("~/Views/Driver/MyRoute.cshtml");
    }
}
