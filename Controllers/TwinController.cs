using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using MLYSO.Web.Models.Twin;
using MLYSO.Web.Services.Twin;

namespace MLYSO.Web.Controllers;

[Authorize]
public sealed class TwinController : Controller
{
    private readonly AppDbContext _db;
    private readonly TwinPlanner _planner;

    public TwinController(AppDbContext db)
    {
        _db = db;
        _planner = new TwinPlanner(db);
    }

    // HUB'daki kart buraya gelecek
    [HttpGet("/twin")]
    public async Task<IActionResult> Index()
    {
        await Data.SeedTwin.EnsureAsync(_db);

        var wh = await _db.Warehouses.AsNoTracking().FirstAsync();
        ViewBag.Warehouse = wh;
        ViewBag.Modes = Enum.GetValues(typeof(TransportMode));
        ViewBag.ContainerTypes = await _db.ContainerTypes.AsNoTracking().OrderBy(c => c.Mode).ToListAsync();
        ViewBag.BoxTypes = await _db.BoxTypes.AsNoTracking().ToListAsync();
        return View();
    }

    public sealed record OptimizeRequest(
        int warehouseId,
        TransportMode mode,
        Dictionary<int, int> containerTypeIdToQty,
        Dictionary<int, int> boxTypeIdToQty);

    [ValidateAntiForgeryToken]
    [HttpPost("/api/twin/optimize")]
    public async Task<IActionResult> Optimize([FromBody] OptimizeRequest req)
    {
        if (req is null) return BadRequest();

        var job = new PackingJob { WarehouseId = req.warehouseId };

        foreach (var kv in req.containerTypeIdToQty.Where(kv => kv.Value > 0))
            job.Containers.Add(new PackingJobContainer { ContainerTypeId = kv.Key, Quantity = kv.Value });

        foreach (var kv in req.boxTypeIdToQty.Where(kv => kv.Value > 0))
            job.Items.Add(new PackingJobItem { BoxTypeId = kv.Key, Quantity = kv.Value });

        _db.PackingJobs.Add(job);
        await _db.SaveChangesAsync();

        var plan = await _planner.PlanAsync(job.Id);

        var dto = new
        {
            plan.Id,
            plan.VolumeUtilizationPct,
            plan.WeightUtilizationPct,
            plan.ContainersUsed,
            warehouse = await _db.Warehouses.Where(w => w.Id == job.WarehouseId)
                .Select(w => new { w.LengthMm, w.WidthMm, w.HeightMm }).SingleAsync(),
            placements = plan.WarehousePlacements.Select(p => new
            {
                container = _db.ContainerTypes.Where(c => c.Id == p.ContainerTypeId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Code,
                        c.Name,
                        dim = new { innerL = c.InnerL, innerW = c.InnerW, innerH = c.InnerH }
                    }).First(),
                p.X,
                p.Y,
                p.RotDeg,
                boxes = p.BoxPlacements.Select(b => new
                {
                    size = _db.BoxTypes.Where(x => x.Id == b.BoxTypeId)
                        .Select(x => new { l = x.L, w = x.W, h = x.H, x.Code }).First(),
                    b.X,
                    b.Y,
                    b.Z,
                    b.RotX,
                    b.RotY,
                    b.RotZ
                })
            })
        };
        return Json(dto);
    }
}
