using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using RouteEntity = MLYSO.Web.Models.Route;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/driver")]
[Authorize(Roles = $"{Roles.Driver},{Roles.Admin}")]
public class DriverApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public DriverApiController(AppDbContext db) => _db = db;

    // --- Admin: sürücü listesi ---
    [HttpGet("drivers")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DriverList()
        => Ok(await _db.Users.AsNoTracking()
                .Where(u => u.Role == Roles.Driver)
                .Select(u => new { u.Id, u.FullName, u.Username })
                .ToListAsync());

    // --- Sürücünün güncel rotası (admin bakarken ilk sürücüye düşer) ---
    [HttpGet("myroute")]
    public async Task<IActionResult> MyRoute()
    {
        var me = User.Identity?.Name ?? string.Empty;

        var user = await _db.Users.AsNoTracking()
                     .FirstOrDefaultAsync(u => u.Username == me && u.Role == Roles.Driver);

        // Admin veya rolü bulunamazsa: ilk sürücüye düş
        if (user == null)
            user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Role == Roles.Driver);

        if (user == null) return Ok(new { message = "no_driver" });

        var route = await _db.Routes.AsNoTracking()
            .OrderByDescending(r => r.RouteDate)
            .FirstOrDefaultAsync(r => r.DriverUserId == user.Id);

        if (route == null) return Ok(new { message = "no_route" });

        var stops = await _db.RouteStops.AsNoTracking()
            .Where(s => s.RouteId == route.Id)
            .OrderBy(s => s.StopNo)
            .Select(s => new { s.Id, s.StopNo, s.Customer, s.Address, s.OrderId, s.PlannedTime, s.Status })
            .ToListAsync();

        return Ok(new { route.Id, route.Vehicle, route.RouteDate, route.Status, stops });
    }

    // --- Admin: rota oluştur ---
    public record NewRouteDto(int DriverUserId, string Vehicle);

    [HttpPost("route")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateRoute([FromBody] NewRouteDto dto)
    {
        var r = new RouteEntity
        {
            DriverUserId = dto.DriverUserId,
            Vehicle = dto.Vehicle,
            RouteDate = DateTime.UtcNow.Date,
            Status = "Planned"
        };
        _db.Routes.Add(r);
        await _db.SaveChangesAsync();
        return Ok(new { id = r.Id });
    }

    // --- Admin: durak ekle ---
    public record NewStopDto(int RouteId, int StopNo, string Customer, string Address, string OrderId, DateTime PlannedTime);

    [HttpPost("stop")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> CreateStop([FromBody] NewStopDto dto)
    {
        var s = new RouteStop
        {
            RouteId = dto.RouteId,
            StopNo = dto.StopNo,
            Customer = dto.Customer,
            Address = dto.Address,
            OrderId = dto.OrderId,
            PlannedTime = dto.PlannedTime,
            Status = "Pending"
        };
        _db.RouteStops.Add(s);
        await _db.SaveChangesAsync();
        return Ok(new { id = s.Id });
    }

    public record UpdateStopDto(string? Status, string? ProofCode);

    [HttpPut("stop/{id:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Driver}")]
    public async Task<IActionResult> UpdateStop(int id, [FromBody] UpdateStopDto dto)
    {
        var s = await _db.RouteStops.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Status)) s.Status = dto.Status!;
        if (!string.IsNullOrWhiteSpace(dto.ProofCode)) s.ProofCode = dto.ProofCode!;

        await _db.SaveChangesAsync();
        return Ok(new { message = "updated" });
    }
}
