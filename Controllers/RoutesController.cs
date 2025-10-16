using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers;

[Authorize(Roles = $"{Roles.Admin},{Roles.Planning},{Roles.Logistics},{Roles.Driver}")]
public class RoutesController : Controller
{
    private readonly AppDbContext _db;
    public RoutesController(AppDbContext db) { _db = db; }

    [HttpGet("/routes")]
    public IActionResult Index() => View();

    [HttpGet("/routes/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var plan = await _db.RoutePlans.Include(r => r.Stops.OrderBy(s => s.OrderNo))
                                       .FirstOrDefaultAsync(r => r.Id == id);
        if (plan == null) return NotFound();
        return View(plan);
    }
}
