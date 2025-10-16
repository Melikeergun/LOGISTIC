using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/orders")]
public sealed class OrdersApiController : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Get([FromQuery] int take = 100, [FromQuery] string? q = null)
    {
        var data = new[]
        {
            new { id="ORD-2001", date=DateTime.UtcNow.AddDays(-2), customer="Demo A.Þ.", sku="SKU-DEMO-1", quantity=5,  amount=350.0, status="created"   },
            new { id="ORD-2002", date=DateTime.UtcNow.AddDays(-1), customer="Test Ltd.", sku="SKU-DEMO-2", quantity=12, amount=960.0, status="shipped"   },
            new { id="ORD-2003", date=DateTime.UtcNow,          customer="Örnek SA", sku="SKU-DEMO-3", quantity=8,  amount=560.0, status="delivered" },
        };

        var list = data.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim().ToLowerInvariant();
            list = list.Where(x => ($"{x.id} {x.customer} {x.sku}").ToLowerInvariant().Contains(s));
        }

        return Ok(list.Take(Math.Clamp(take, 1, 1000)));
    }
}
