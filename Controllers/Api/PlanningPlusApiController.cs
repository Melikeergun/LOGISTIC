using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

// Çakışmayı çözen alias:
using RouteEntity = MLYSO.Web.Models.Route;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
// İsterseniz eskiyi bırakabilirsiniz ama + işareti yerine sade yol öneririm
[Route("api/planning")]
[Authorize(Roles = $"{Roles.Operations},{Roles.Planning},{Roles.Admin}")]
public sealed class PlanningPlusApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StaticJsonStore<ForecastState> _store;

    public PlanningPlusApiController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db; _store = new(env, "forecasts");
        var s = _store.Read();
        if (s.Items.Count == 0)
        {
            s.Items.Add(new ForecastRow
            {
                Id = s.NextId++,
                Sku = "SKU-1001",
                Date = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(7)),
                Demand = 120,
                Note = "Olası kampanya"
            });
            _store.Write(s);
        }
    }

    // ---- Forecasts (JSON store) ----
    [HttpGet("forecasts")]
    public IActionResult Forecasts() => Ok(_store.Read().Items.OrderBy(x => x.Date));

    public record NewForecast(string Sku, DateOnly Date, int Demand, string? Note);

    [HttpPost("forecasts")]
    public IActionResult Create([FromBody] NewForecast dto)
    {
        var s = _store.Read();
        var row = new ForecastRow
        {
            Id = s.NextId++,
            Sku = dto.Sku,
            Date = dto.Date,
            Demand = dto.Demand,
            Note = dto.Note
        };
        s.Items.Add(row);
        _store.Write(s);
        return Ok(row);
    }

    [HttpPut("forecasts/{id:int}")]
    public IActionResult Update(int id, [FromBody] NewForecast dto)
    {
        var s = _store.Read();
        var row = s.Items.FirstOrDefault(x => x.Id == id);
        if (row == null) return NotFound();
        row.Sku = dto.Sku; row.Date = dto.Date; row.Demand = dto.Demand; row.Note = dto.Note;
        _store.Write(s);
        return Ok(row);
    }

    [HttpDelete("forecasts/{id:int}")]
    public IActionResult Delete(int id)
    {
        var s = _store.Read();
        s.Items.RemoveAll(x => x.Id == id);
        _store.Write(s);
        return Ok(new { deleted = id });
    }

    // ---- Routes (EF) ----
    [HttpGet("routes")]
    public async Task<IActionResult> Routes() =>
        Ok(await _db.Routes.AsNoTracking().OrderByDescending(r => r.RouteDate).Take(200).ToListAsync());

    public record NewRoute(DateTime RouteDate, string Vehicle, int? DriverUserId);

    [HttpPost("routes")]
    public async Task<IActionResult> CreateRoute([FromBody] NewRoute dto)
    {
        if (dto.DriverUserId is null)
            return BadRequest(new { error = "DriverUserId is required." });

        var r = new RouteEntity
        {
            RouteDate = dto.RouteDate.Date,
            Vehicle = dto.Vehicle,
            Status = "Planned",
            DriverUserId = dto.DriverUserId.Value   
        };

        _db.Routes.Add(r);
        await _db.SaveChangesAsync();
        return Ok(r);
    }


    public record NewStop(int RouteId, int StopNo, string Customer, string Address, string OrderId, DateTime PlannedTime);

    [HttpPost("routes/stop")]
    public async Task<IActionResult> AddStop([FromBody] NewStop dto)
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
        return Ok(s);
    }

    // Body JSON'ı { "status": "...", "proofCode": "..." } şeklinde bekleyelim
    public record UpdateStopDto(string? Status, string? ProofCode);

    [HttpPut("routes/stop/{id:int}")]
    public async Task<IActionResult> UpdateStop(int id, [FromBody] UpdateStopDto dto)
    {
        var s = await _db.RouteStops.FirstOrDefaultAsync(x => x.Id == id);
        if (s == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Status))
            s.Status = dto.Status!;

        if (!string.IsNullOrWhiteSpace(dto.ProofCode))
            s.ProofCode = dto.ProofCode; // modelde alan yoksa ekleyin

        await _db.SaveChangesAsync();
        return Ok(s);
    }

    [HttpDelete("routes/stop/{id:int}")]
    public async Task<IActionResult> DeleteStop(int id)
    {
        var s = await _db.RouteStops.FindAsync(id);
        if (s == null) return NotFound();
        _db.RouteStops.Remove(s);
        await _db.SaveChangesAsync();
        return Ok(new { deleted = id });
    }
}
