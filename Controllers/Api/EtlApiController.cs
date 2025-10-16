using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Services;
using MLYSO.Web.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/etl")]
public class EtlApiController : ControllerBase
{
    private readonly EtlService _etl;
    private readonly AppDbContext _db;

    public EtlApiController(EtlService etl, AppDbContext db)
    {
        _etl = etl ?? throw new ArgumentNullException(nameof(etl));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    [HttpPost("run")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Run()
    {
        var result = await _etl.RunAsync();   // parametresiz
        if (!result.ok) return BadRequest(new { error = result.message });
        return Ok(new { message = result.message, lastRunUtc = _etl.LastRunUtc });
    }
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + "," + CookieAuthenticationDefaults.AuthenticationScheme)]

    [HttpGet("status")]
    [Authorize]
    public IActionResult Status()
    {
        return Ok(new
        {
            lastRunUtc = _etl.LastRunUtc,
            orders = _db.OOrders.Count(),
            items = _db.OOrderItems.Count(),
            payments = _db.OPayments.Count(),
            customers = _db.OCustomers.Count(),
            sellers = _db.OSellers.Count()   
        });
    }
}
