using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers
{
    [Authorize]
    [Route("ui/warehouse")]   // Tüm depo ekranları bu taban altında
    public class WarehouseController : Controller
    {
        // GET /ui/warehouse  => Yönetici ekranı (tab parametresi ile alt sekme seçimi)
        [HttpGet("")]
        [Authorize(Policy = "WarehouseManagerOrAdmin")]
        public IActionResult Manager([FromQuery] string? tab = null)
        {
            ViewBag.Tab = (tab ?? "").ToLowerInvariant();   // örn: racks, twin, workorders...
            return View("~/Views/Warehouse/Manager.cshtml");
        }

        // GET /ui/warehouse/manager  => alias
        [HttpGet("manager")]
        [Authorize(Policy = "WarehouseManagerOrAdmin")]
        public IActionResult ManagerAlias([FromQuery] string? tab = null)
            => RedirectToAction(nameof(Manager), new { tab });

        // GET /ui/warehouse/operator  => Operatör ekranı (tab opsiyonel)
        [HttpGet("operator")]
        [Authorize(Policy = "WarehouseOperatorOrAdmin")]
        public IActionResult Operator([FromQuery] string? tab = null)
        {
            ViewBag.Tab = (tab ?? "").ToLowerInvariant();   // örn: pick / putaway / count
            return View("~/Views/Warehouse/Operator.cshtml");
        }

        // GET /ui/warehouse/tasks  => operator alias (eski linkler kırılmasın)
        [HttpGet("tasks")]
        [Authorize(Policy = "WarehouseOperatorOrAdmin")]
        public IActionResult TasksAlias([FromQuery] string? tab = null)
            => RedirectToAction(nameof(Operator), new { tab });

        // GET /ui/warehouse/racks  => (İsterseniz doğrudan raf 3D sahnesi için ayrı view)
        // Hub kartı zaten /ui/warehouse?tab=racks ile Manager'a düşüyor; ancak
        // doğrudan rota da kalsın isterseniz:
        [HttpGet("racks")]
        [Authorize(Policy = "WarehouseManagerOrAdmin")]
        public IActionResult Racks()
            => View("~/Views/Warehouse/Racks.cshtml");
    }
}
