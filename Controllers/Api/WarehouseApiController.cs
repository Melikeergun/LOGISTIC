using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/warehouse")]
[Authorize(Roles = $"{Roles.WarehouseOperator},{Roles.WarehouseManager},{Roles.Admin}")]
public class WarehouseApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehouseApiController(AppDbContext db) { _db = db; }

    // Liste
    [HttpGet("tasks")]
    public async Task<IActionResult> ListTasks([FromQuery] string? status = null)
    {
        var q = _db.WhTasks.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var items = await q.OrderBy(x => x.Status).ThenBy(x => x.Id)
            .Select(x => new { x.Id, x.Type, x.Status, x.Location, x.Sku, x.Quantity })
            .ToListAsync();
        return Ok(new { items });
    }

    // Durum güncelle
    public record UpdDto(string? Status);

    [HttpPut("tasks/{id:int}")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdDto dto)
    {
        var t = await _db.WhTasks.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.Status)) t.Status = dto.Status!;
        await _db.SaveChangesAsync();
        return Ok(new { t.Id, t.Type, t.Status, t.Location, t.Sku, t.Quantity });
    }

    // Yeni görev (sadece yönetici+admin)
    public record NewTaskDto(string Type, string Location, string Sku, int Quantity);

    [Authorize(Roles = $"{Roles.WarehouseManager},{Roles.Admin}")]
    [HttpPost("tasks")]
    public async Task<IActionResult> CreateTask([FromBody] NewTaskDto dto)
    {
        var t = new WhTask { Type = dto.Type, Location = dto.Location, Sku = dto.Sku, Quantity = dto.Quantity, Status = "Open" };
        _db.WhTasks.Add(t);
        await _db.SaveChangesAsync();
        return Ok(new { t.Id, t.Type, t.Status, t.Location, t.Sku, t.Quantity });
    }
}
