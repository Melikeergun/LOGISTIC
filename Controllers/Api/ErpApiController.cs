using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/erp")]
[Authorize(Roles = $"{Roles.Purchasing},{Roles.Admin}")]
public class ErpApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public ErpApiController(AppDbContext db) => _db = db;

    [HttpGet("purchase")]
    public async Task<IActionResult> List([FromQuery] int take = 200)
    {
        var items = await _db.Purchases.AsNoTracking()
            .OrderByDescending(x => x.Id).Take(take).ToListAsync();
        return Ok(new { items });
    }

    public record NewPurchaseDto(string Supplier, string Sku, int Qty, decimal Price, string Status);
    [HttpPost("purchase")]
    public async Task<IActionResult> Create([FromBody] NewPurchaseDto dto)
    {
        var p = new Purchase { Supplier = dto.Supplier, Sku = dto.Sku, Qty = dto.Qty, Price = dto.Price, Status = dto.Status };
        _db.Purchases.Add(p); await _db.SaveChangesAsync();
        return Ok(new { id = p.Id });
    }

    public record UpdatePurchaseDto(int? Qty, decimal? Price, string? Status);
    [HttpPut("purchase/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseDto dto)
    {
        var p = await _db.Purchases.FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (dto.Qty.HasValue) p.Qty = dto.Qty.Value;
        if (dto.Price.HasValue) p.Price = dto.Price.Value;
        if (!string.IsNullOrWhiteSpace(dto.Status)) p.Status = dto.Status!;
        await _db.SaveChangesAsync();
        return Ok(new { message = "updated" });
    }
}
