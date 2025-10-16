using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/wh-manager")]
[Authorize(Roles = $"{Roles.WarehouseManager},{Roles.Admin}")]
public sealed class WarehouseManagerApiController : ControllerBase
{
    private readonly StaticJsonStore<RackState> _racks;
    private readonly StaticJsonStore<TwinMatchState> _twins;
    private readonly StaticJsonStore<WorkOrderState> _wos;

    public WarehouseManagerApiController(IWebHostEnvironment env)
    {
        _racks = new(env, "racks");
        _twins = new(env, "digital_twin_matches");
        _wos = new(env, "work_orders");
        SeedIfEmpty();
    }

    private void SeedIfEmpty()
    {
        var r = _racks.Read();
        if (r.Racks.Count == 0)
        {
            r.Racks.AddRange(new[]
            {
                new Rack{ Id=r.NextId++, Code="A-01", Capacity=100, Used=35 },
                new Rack{ Id=r.NextId++, Code="A-02", Capacity=120, Used=90 }
            });
            _racks.Write(r);
        }
        var t = _twins.Read();
        if (t.Items.Count == 0)
        {
            t.Items.Add(new TwinMatch { Id = t.NextId++, RackCode = "A-01", Sku = "SKU-1001", Qty = 20 });
            _twins.Write(t);
        }
        var w = _wos.Read();
        if (w.Items.Count == 0)
        {
            w.Items.Add(new WorkOrder { Id = w.NextId++, Title = "A-01 envanter düzeltme", Status = "Open", Assignee = null });
            _wos.Write(w);
        }
    }

    
    [HttpGet("racks")] public IActionResult GetRacks() => Ok(_racks.Read().Racks.OrderBy(x => x.Code));

    public record RackDto(string Code, int Capacity, int Used);
    [HttpPost("racks")]
    public IActionResult CreateRack([FromBody] RackDto dto)
    {
        var s = _racks.Read();
        var item = new Rack { Id = s.NextId++, Code = dto.Code, Capacity = dto.Capacity, Used = dto.Used };
        s.Racks.Add(item); _racks.Write(s);
        return Ok(item);
    }

    [HttpPut("racks/{id:int}")]
    public IActionResult UpdateRack(int id, [FromBody] RackDto dto)
    {
        var s = _racks.Read();
        var item = s.Racks.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        item.Code = dto.Code; item.Capacity = dto.Capacity; item.Used = dto.Used;
        _racks.Write(s); return Ok(item);
    }

    [HttpDelete("racks/{id:int}")]
    public IActionResult DeleteRack(int id)
    {
        var s = _racks.Read();
        s.Racks.RemoveAll(x => x.Id == id);
        _racks.Write(s);
        return Ok(new { deleted = id });
    }

    // -------- Digital Twin Matches --------
    public record TwinDto(string RackCode, string Sku, int Qty);
    [HttpGet("twins")] public IActionResult GetTwins() => Ok(_twins.Read().Items.OrderBy(x => x.RackCode));

    [HttpPost("twins")]
    public IActionResult CreateTwin([FromBody] TwinDto dto)
    {
        var s = _twins.Read();
        var item = new TwinMatch { Id = s.NextId++, RackCode = dto.RackCode, Sku = dto.Sku, Qty = dto.Qty };
        s.Items.Add(item); _twins.Write(s); return Ok(item);
    }

    [HttpPut("twins/{id:int}")]
    public IActionResult UpdateTwin(int id, [FromBody] TwinDto dto)
    {
        var s = _twins.Read();
        var item = s.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        item.RackCode = dto.RackCode; item.Sku = dto.Sku; item.Qty = dto.Qty;
        _twins.Write(s); return Ok(item);
    }

    [HttpDelete("twins/{id:int}")]
    public IActionResult DeleteTwin(int id)
    {
        var s = _twins.Read();
        s.Items.RemoveAll(x => x.Id == id);
        _twins.Write(s); return Ok(new { deleted = id });
    }

    // -------- Work Orders --------
    public record WoDto(string Title, string Status, string? Assignee);
    [HttpGet("workorders")] public IActionResult GetWos() => Ok(_wos.Read().Items.OrderByDescending(x => x.Id));

    [HttpPost("workorders")]
    public IActionResult CreateWo([FromBody] WoDto dto)
    {
        var s = _wos.Read();
        var item = new WorkOrder { Id = s.NextId++, Title = dto.Title, Status = dto.Status, Assignee = dto.Assignee };
        s.Items.Add(item); _wos.Write(s); return Ok(item);
    }

    [HttpPut("workorders/{id:int}")]
    public IActionResult UpdateWo(int id, [FromBody] WoDto dto)
    {
        var s = _wos.Read();
        var item = s.Items.FirstOrDefault(x => x.Id == id);
        if (item == null) return NotFound();
        item.Title = dto.Title; item.Status = dto.Status; item.Assignee = dto.Assignee;
        _wos.Write(s); return Ok(item);
    }

    [HttpDelete("workorders/{id:int}")]
    public IActionResult DeleteWo(int id)
    {
        var s = _wos.Read();
        s.Items.RemoveAll(x => x.Id == id);
        _wos.Write(s); return Ok(new { deleted = id });
    }
}
