using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/driver+")]
[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
public sealed class DriverPlusApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public DriverPlusApiController(AppDbContext db) => _db = db;

    [HttpGet("my-route")]
    public async Task<IActionResult> MyRoute()
    {
        var uidStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(uidStr, out var uid)) return Ok(new { route = (object?)null, stops = Array.Empty<RouteStop>() });

        var r = await _db.Routes.AsNoTracking().OrderByDescending(x => x.RouteDate)
                       .FirstOrDefaultAsync(x => x.DriverUserId == uid);
        if (r == null) return Ok(new { route = (object?)null, stops = Array.Empty<RouteStop>() });
        var stops = await _db.RouteStops.AsNoTracking().Where(x => x.RouteId == r.Id).OrderBy(x => x.StopNo).ToListAsync();
        return Ok(new { route = r, stops });
    }

    public record PodDto(string ProofCode);
    [HttpPost("deliver/{stopId:int}")]
    public async Task<IActionResult> Deliver(int stopId, [FromBody] PodDto dto)
    {
        var s = await _db.RouteStops.FirstOrDefaultAsync(x => x.Id == stopId);
        if (s == null) return NotFound();
        s.Status = "Delivered"; s.ProofCode = dto.ProofCode; await _db.SaveChangesAsync(); return Ok(s);
    }

    [HttpPost("return/{stopId:int}")]
    public async Task<IActionResult> MarkReturn(int stopId, [FromBody] string reason)
    {
        var s = await _db.RouteStops.FirstOrDefaultAsync(x => x.Id == stopId);
        if (s == null) return NotFound();
        s.Status = "ReturnRequested"; s.ProofCode = reason; await _db.SaveChangesAsync(); return Ok(s);
    }
}
