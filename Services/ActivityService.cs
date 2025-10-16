using System.Security.Claims;
using System.Text.Json;
using MLYSO.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MLYSO.Web.Services;

public sealed class ActivityService
{
    private readonly AppDbContext _db;
    public ActivityService(AppDbContext db) { _db = db; }

    private static Audience RoleToAudience(string role) => role switch
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

    private static string U(ClaimsPrincipal u) => u?.Identity?.Name ?? u?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "sys";
    private static string R(ClaimsPrincipal u) => u?.FindFirstValue(ClaimTypes.Role) ?? "";

    public async Task<Activity> PublishAsync(Activity a, ClaimsPrincipal actor)
    {
        a.ActorUser = U(actor);
        a.ActorRole = R(actor);
        _db.Activities.Add(a);
        await _db.SaveChangesAsync();
        return a;
    }

    public Task<Activity> DriverDelayAsync(string orderId, string routeId, int minutes, string reason, ClaimsPrincipal actor)
    {
        var audience = Audience.Customer | Audience.Operations | Audience.WarehouseManager | Audience.WarehouseOperator | Audience.CrmAgent | Audience.CustomerService;
        var a = new Activity
        {
            Code = "DRIVER_DELAY",
            Title = "Teslimatta gecikme",
            Detail = $"Şoför raporu: {minutes} dk gecikme. Nedeni: {reason}",
            Severity = minutes >= 30 ? "warn" : "info",
            EntityType = "Order",
            EntityId = orderId,
            OrderId = orderId,
            RouteId = routeId,
            Reason = reason,
            DelayMinutes = minutes,
            Audience = audience
        };
        return PublishAsync(a, actor);
    }

    public Task<Activity> OperatorBlockedAsync(string taskId, string? orderId, string loc, string reason, ClaimsPrincipal actor)
    {
        var audience = Audience.Operations | Audience.WarehouseManager | Audience.CrmAgent | Audience.CustomerService;
        var a = new Activity
        {
            Code = "WH_TASK_BLOCKED",
            Title = "Depo görevi beklemede",
            Detail = $"Lokasyon {loc}: {reason}",
            Severity = "warn",
            EntityType = "Task",
            EntityId = taskId,
            TaskId = taskId,
            OrderId = orderId,
            Reason = reason,
            Audience = audience
        };
        return PublishAsync(a, actor);
    }

    public Task<Activity> OrderStatusAsync(string orderId, string status, string? reason, ClaimsPrincipal actor)
    {
        var audience = Audience.Customer | Audience.Operations | Audience.CrmAgent | Audience.CustomerService;
        var a = new Activity
        {
            Code = $"ORDER_{status.ToUpperInvariant()}",
            Title = $"Sipariş {status}",
            Detail = string.IsNullOrWhiteSpace(reason) ? "" : $"Not: {reason}",
            EntityType = "Order",
            EntityId = orderId,
            OrderId = orderId,
            Reason = reason,
            Severity = status.Equals("returned", StringComparison.OrdinalIgnoreCase) ? "warn" : "info",
            Audience = audience
        };
        return PublishAsync(a, actor);
    }
}