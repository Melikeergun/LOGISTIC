// Controllers/Api/SupplierApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using System.Linq; // ← ekle

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/supplier")]
[Authorize(Roles = $"{Roles.Supplier},{Roles.Admin}")]
public class SupplierApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public SupplierApiController(AppDbContext db) => _db = db;

    [HttpGet("asn")]
    public async Task<IActionResult> List() =>
        Ok(await _db.AsnOrders.AsNoTracking().OrderByDescending(x => x.Id).Take(200).ToListAsync());

    public record NewAsnDto(DateTime Slot, string Sku, int Qty, string Status);

    [HttpPost("asn")]
    public async Task<IActionResult> Create([FromBody] NewAsnDto dto)
    {
        var a = new AsnOrder { Slot = dto.Slot, Sku = dto.Sku, Qty = dto.Qty, Status = dto.Status };
        _db.AsnOrders.Add(a); await _db.SaveChangesAsync();
        return Ok(new { id = a.Id });
    }

    public record UpdateAsnDto(DateTime? Slot, string? Sku, int? Qty, string? Status);

    [HttpPut("asn/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAsnDto dto)
    {
        var a = await _db.AsnOrders.FirstOrDefaultAsync(x => x.Id == id);
        if (a == null) return NotFound();
        if (dto.Slot.HasValue) a.Slot = dto.Slot.Value;
        if (!string.IsNullOrWhiteSpace(dto.Sku)) a.Sku = dto.Sku!;
        if (dto.Qty.HasValue) a.Qty = dto.Qty.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status)) a.Status = dto.Status!;
        await _db.SaveChangesAsync();
        return Ok(new { message = "updated" });
    }
}
