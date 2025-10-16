using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers;
[Authorize]
[ApiController]
[Route("api/activities")]
public class ActivitiesApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ActivityService _svc;
    public ActivitiesApiController(AppDbContext db, ActivityService svc) { _db = db; _svc = svc; }

    private static Audience Map(string role) => role switch
    {
        Roles.Admin => Audience.Admin,
        Roles.Operations => Audience.Operations,
        Roles.Planning => Audience.Planning,
        Roles.Logistics => Audience.Logistics,
        Roles.WarehouseManager => Audience.WarehouseManager,
        Roles.WarehouseChief => Audience.WarehouseManager,
        Roles.WarehouseOperator => Audience.WarehouseOperator,
        Roles.Driver => Audience.Driver,
        Roles.Purchasing => Audience.Purchasing,
        Roles.CrmAgent => Audience.CrmAgent,
        Roles.CustomerService => Audience.CustomerService,   // 🔹 eksikti
        Roles.Supplier => Audience.Supplier,
        Roles.Customer => Audience.Customer,
        _ => Audience.None
    };

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int take = 50, [FromQuery] string? entityType = null, [FromQuery] string? entityId = null)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        var aud = Map(role);

        var q = _db.Activities.AsNoTracking()
                              .OrderByDescending(x => x.Id)
                              .Where(x => (x.Audience & aud) != 0);

        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(x => x.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            q = q.Where(x => x.EntityId == entityId
                          || x.OrderId == entityId
                          || x.RouteId == entityId
                          || x.TaskId == entityId);

        take = Math.Clamp(take, 1, 500);
        return Ok(await q.Take(take).ToListAsync());
    }

    public record DriverDelayDto(string? OrderId, string? RouteId, int Minutes, string Reason);
    [Authorize(Roles = Roles.Admin + "," + Roles.Driver)]
    [HttpPost("driver-delay")]
    public async Task<IActionResult> DriverDelay([FromBody] DriverDelayDto dto)
        => Ok(await _svc.DriverDelayAsync(dto.OrderId ?? "", dto.RouteId ?? "", dto.Minutes, dto.Reason, User));

    public record OperatorBlockDto(string TaskId, string? OrderId, string Location, string Reason);
    [Authorize(Roles = Roles.Admin + "," + Roles.WarehouseOperator)]
    [HttpPost("operator-block")]
    public async Task<IActionResult> OperatorBlock([FromBody] OperatorBlockDto dto)
        => Ok(await _svc.OperatorBlockedAsync(dto.TaskId, dto.OrderId, dto.Location, dto.Reason, User));

    public record OrderStatusDto(string OrderId, string Status, string? Reason);
    [Authorize(Roles = Roles.Admin + "," + Roles.Operations + "," + Roles.Planning + "," + Roles.Logistics + "," + Roles.CrmAgent + "," + Roles.CustomerService)]
    [HttpPost("order-status")]
    public async Task<IActionResult> OrderStatus([FromBody] OrderStatusDto dto)
        => Ok(await _svc.OrderStatusAsync(dto.OrderId, dto.Status, dto.Reason, User));
}
