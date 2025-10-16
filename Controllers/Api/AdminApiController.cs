using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Operations},{Roles.Planning},{Roles.Logistics}")]
public class AdminApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminApiController(AppDbContext db) { _db = db; }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var tasks = await _db.WhTasks.CountAsync();
        var routes = await _db.Routes.CountAsync();
        var purchases = await _db.Purchases.CountAsync();
        var risks = await _db.CrmRisks.CountAsync();
        var asn = await _db.AsnOrders.CountAsync();
        var orders = await _db.Orders.CountAsync();

        return Ok(new { tasks, routes, purchases, risks, asn, orders });
    }
}
