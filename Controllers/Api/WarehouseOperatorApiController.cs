using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/wh-operator")]
[Authorize(Roles = $"{Roles.WarehouseOperator},{Roles.Admin}")]
public sealed class WarehouseOperatorApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarehouseOperatorApiController(AppDbContext db) => _db = db;

    [HttpGet("tasks")]
    public async Task<IActionResult> List([FromQuery] string? type = null, [FromQuery] string? status = null)
    {
        var q = _db.WhTasks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(type)) q = q.Where(x => x.Type == type);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var rows = await q.OrderBy(x => x.CreatedAt).Take(500).ToListAsync();
        return Ok(rows);
    }

    public record NewTaskDto(string Type, string Location, string Sku, int Quantity, string? Notes);
    [HttpPost("tasks")]
    public async Task<IActionResult> Create([FromBody] NewTaskDto dto)
    {
        var t = new WhTask { Type = dto.Type, Status = "Open", Location = dto.Location, Sku = dto.Sku, Quantity = dto.Quantity, Notes = dto.Notes, CreatedAt = DateTime.UtcNow };
        _db.WhTasks.Add(t); await _db.SaveChangesAsync();
        return Ok(t);
    }

    public record UpdateTaskDto(string? Status, string? Location, string? Sku, int? Quantity, string? Notes);
    [HttpPut("tasks/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        var t = await _db.WhTasks.FirstOrDefaultAsync(x => x.Id == id);
        if (t == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(dto.Status)) t.Status = dto.Status!;
        if (!string.IsNullOrWhiteSpace(dto.Location)) t.Location = dto.Location!;
        if (!string.IsNullOrWhiteSpace(dto.Sku)) t.Sku = dto.Sku!;
        if (dto.Quantity.HasValue) t.Quantity = dto.Quantity.Value;
        if (dto.Notes != null) t.Notes = dto.Notes;
        await _db.SaveChangesAsync();
        return Ok(t);
    }

    [HttpDelete("tasks/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var t = await _db.WhTasks.FindAsync(id);
        if (t == null) return NotFound();
        _db.WhTasks.Remove(t); await _db.SaveChangesAsync();
        return Ok(new { deleted = id });
    }
}
