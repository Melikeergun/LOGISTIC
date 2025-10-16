using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace MLYSO.Web.Controllers
{
    [Authorize]
    public class RolesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _cfg;

        public RolesController(AppDbContext db, IConfiguration cfg)
        { _db = db; _cfg = cfg; }

        [HttpGet("/roles/choose")]
        public IActionResult Choose([FromQuery] string? returnUrl = null)
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/ui" : returnUrl;
            return View();
        }

        [HttpPost("/roles/choose")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChoosePost(string role, [FromQuery] string? returnUrl = null, bool rememberMe = true)
        {
            role = (role ?? "").Trim();
            var all = new[]
            {
                Roles.Admin, Roles.Operations, Roles.Planning, Roles.Logistics,
                Roles.WarehouseManager, Roles.WarehouseChief, Roles.WarehouseOperator,
                Roles.Driver, Roles.Purchasing, Roles.CrmAgent, Roles.CustomerService,
                Roles.Supplier, Roles.Customer
            };
            if (!all.Contains(role)) role = Roles.Customer;

            var username = User.Identity?.Name ?? User.FindFirstValue("sub") ?? "";
            var dbUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
            if (dbUser == null) return Redirect("/account/login");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                new Claim(ClaimTypes.Name, dbUser.Username),
                new Claim(ClaimTypes.Role, role),
                new Claim("sub", dbUser.Username),
                new Claim("full_name", dbUser.FullName ?? "")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = rememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 7 : 1)
                });

            var token = GenerateJwt(dbUser.Username, dbUser.FullName ?? "", role);
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(rememberMe ? 7 : 1)
            });

            var fallback = LandingUrlFor(role);
            return Redirect(SafeReturnUrl(returnUrl, fallback));
        }

        private static string SafeReturnUrl(string? ret, string fallback)
            => string.IsNullOrWhiteSpace(ret) || !ret.StartsWith("/") ? fallback : ret;

        private static string LandingUrlFor(string? role)
        {
            role ??= Roles.Customer;
            if (role.Equals(Roles.Admin, StringComparison.OrdinalIgnoreCase)) return "/ui/dashboard";
            if (role.Equals(Roles.Operations, StringComparison.OrdinalIgnoreCase)
             || role.Equals(Roles.Planning, StringComparison.OrdinalIgnoreCase)) return "/ui/plan";
            if (role.Equals(Roles.WarehouseManager, StringComparison.OrdinalIgnoreCase)
             || role.Equals(Roles.WarehouseChief, StringComparison.OrdinalIgnoreCase)) return "/ui/warehouse";
            if (role.Equals(Roles.WarehouseOperator, StringComparison.OrdinalIgnoreCase)) return "/ui/tasks";
            if (role.Equals(Roles.Driver, StringComparison.OrdinalIgnoreCase)) return "/ui/driver";
            if (role.Equals(Roles.Purchasing, StringComparison.OrdinalIgnoreCase)) return "/ui/erp-purchase";
            if (role.Equals(Roles.CrmAgent, StringComparison.OrdinalIgnoreCase)
             || role.Equals(Roles.CustomerService, StringComparison.OrdinalIgnoreCase)) return "/ui/crm-risk";
            if (role.Equals(Roles.Supplier, StringComparison.OrdinalIgnoreCase)) return "/ui/supplier-asn";
            return "/ui/orders";
        }

        private string GenerateJwt(string user, string full, string role)
        {
            var keyStr = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
            var issuer = _cfg["Jwt:Issuer"] ?? "ml-issuer";
            var audience = _cfg["Jwt:Audience"] ?? "ml-audience";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user),
                new Claim(ClaimTypes.Name, user),
                new Claim(ClaimTypes.Role, role),
                new Claim("full_name", full)
            };
            var hours = int.TryParse(_cfg["Jwt:ExpireHours"], out var h) ? h : 8;
            var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddHours(hours), signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
