// Controllers/Api/CrmApiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using System.Linq; // ← ekle

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/crm")]
[Authorize(Roles = $"{Roles.CrmAgent},{Roles.Admin}")]
public class CrmApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public CrmApiController(AppDbContext db) => _db = db;

    [HttpGet("risk")]
    public async Task<IActionResult> List([FromQuery] int take = 200, [FromQuery] string? cust = null, [FromQuery] string? seg = null)
    {
        var q = _db.CrmRisks.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(cust)) q = q.Where(x => x.Customer.Contains(cust));
        if (!string.IsNullOrWhiteSpace(seg)) q = q.Where(x => x.Segment == seg);
        var items = await q.OrderByDescending(x => x.Id).Take(take).ToListAsync();
        return Ok(new { items });
    }

    public record NewRiskDto(string Customer, string Segment, int Risk, string? Note);

    [HttpPost("risk")]
    public async Task<IActionResult> Create([FromBody] NewRiskDto dto)
    {
        var r = new CrmRisk { Customer = dto.Customer, Segment = dto.Segment, Risk = dto.Risk, Note = dto.Note };
        _db.CrmRisks.Add(r); await _db.SaveChangesAsync();
        return Ok(new { id = r.Id });
    }

    public record UpdateRiskDto(int? Risk, string? Note);

    [HttpPut("risk/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRiskDto dto)
    {
        var r = await _db.CrmRisks.FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (dto.Risk.HasValue) r.Risk = dto.Risk.Value;
        if (dto.Note != null) r.Note = dto.Note;
        await _db.SaveChangesAsync();
        return Ok(new { message = "updated" });
    }
}
