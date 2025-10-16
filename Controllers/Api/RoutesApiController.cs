using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api
{
    [ApiController]
    [Route("api/routes")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Planning},{Roles.Logistics},{Roles.Driver}")]
    public class RoutesApiController : ControllerBase
    {
        private readonly TrGeoService _geo;
        private readonly AppDbContext _db;

        public RoutesApiController(TrGeoService geo, AppDbContext db)
        {
            _geo = geo;
            _db = db;
        }

        [HttpGet("provinces")]
        public IActionResult Provinces() => Ok(_geo.Provinces());

        [HttpGet("districts")]
        public IActionResult Districts([FromQuery] string il) => Ok(_geo.Districts(il));

        [HttpGet("places")]
        public IActionResult Places([FromQuery] string il, [FromQuery] string ilce)
        {
            if (string.Equals(ilce, "*ALL*", StringComparison.OrdinalIgnoreCase))
            {
                var all = _geo.Places(il, "*ALL*")
                              .Select(p => new { key = p.Key, p.semt, p.lat, p.lng, p.ilce });
                return Ok(all);
            }

            var list = _geo.Places(il, ilce)
                           .Select(p => new { key = p.Key, p.semt, p.lat, p.lng, p.ilce });
            return Ok(list);
        }



        public record StopDto(string? key, string title, string? address, double lat, double lng);
        public record SaveDto(string name, string? vehiclePlate, System.Collections.Generic.List<StopDto> stops);

        [HttpPost("optimize")]
        public IActionResult Optimize([FromBody] System.Collections.Generic.List<StopDto> stops)
        {
            var routeStops = stops.Select(s => new RoutePlanStop
            {
                GeoKey = s.key,
                Title = s.title,
                Address = s.address ?? "",
                Lat = s.lat,
                Lng = s.lng
            }).ToList();

            var optimized = RoutingService.OptimizeSingle(routeStops);
            var totals = RoutingService.Totals(optimized);

            return Ok(new
            {
                stops = optimized.Select(s => new
                {
                    s.OrderNo,
                    s.Title,
                    s.Address,
                    s.Lat,
                    s.Lng
                }),
                totalKm = totals.km,
                totalMin = totals.minutes
            });
        }

        [HttpPost("save")]
        public async Task<IActionResult> Save([FromBody] SaveDto dto)
        {
            var routeStops = dto.stops.Select(s => new RoutePlanStop
            {
                GeoKey = s.key,
                Title = s.title,
                Address = s.address ?? "",
                Lat = s.lat,
                Lng = s.lng
            }).ToList();

            var optimized = RoutingService.OptimizeSingle(routeStops);
            var totals = RoutingService.Totals(optimized);

            var rp = new RoutePlan
            {
                Name = dto.name,
                VehiclePlate = dto.vehiclePlate ?? "",
                CreatedBy = User.Identity?.Name ?? "user",
                TotalDistanceKm = totals.km,
                TotalEstimatedMinutes = totals.minutes,
                Optimization = "Süre",
                Stops = optimized
            };

            _db.RoutePlans.Add(rp);
            await _db.SaveChangesAsync();
            return Ok(new { id = rp.Id, code = rp.Code });
        }
    }
}
