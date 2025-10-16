using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MLYSO.Web.Models;
using MLYSO.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace MLYSO.Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly PasswordService _pwd;
    private readonly IConfiguration _cfg;

    public AccountController(AppDbContext db, PasswordService pwd, IConfiguration cfg)
    { _db = db; _pwd = pwd; _cfg = cfg; }

    private void AddNoCacheHeaders()
    {
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Headers["Expires"] = "0";
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

    // ---------- Login ----------
    [HttpGet("/account/login")]
    public IActionResult Login([FromQuery] string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? Roles.Customer;
            return Redirect(SafeReturnUrl(returnUrl, LandingUrlFor(role)));
        }
        AddNoCacheHeaders();
        ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
        return View();
    }

    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(string username, string password, bool rememberMe = true, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Kullanıcı adı ve şifre zorunludur.");
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
            return View("Login");
        }

        var uname = username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == uname && u.IsActive);
        if (user == null)
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
            return View("Login");
        }

        if (!_pwd.Verify(password ?? "", user.PasswordHash ?? "", out var upgraded))
        {
            ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
            return View("Login");
        }
        if (upgraded != null) { user.PasswordHash = upgraded; await _db.SaveChangesAsync(); }

        // cookie + jwt ve rol seçimi yönlendirmesi
        await SignInCookieAsync(user, rememberMe);
        var token = GenerateJwt(user);
        WriteJwtCookie(token, rememberMe);

        var fallback = LandingUrlFor(user.Role);
        return Redirect($"/roles/choose?returnUrl={Uri.EscapeDataString(SafeReturnUrl(returnUrl, fallback))}");

    }


    // ---------- Register ----------
    [HttpGet("/account/register")]
    public IActionResult Register([FromQuery] string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirstValue(ClaimTypes.Role) ?? Roles.Customer;
            return Redirect(SafeReturnUrl(returnUrl, LandingUrlFor(role)));
        }
        AddNoCacheHeaders();
        ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
        return View();
    }

    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterPost(
        string username, string fullName, string password,
        bool rememberMe = true, string? roleRequest = null, string? inviteCode = null, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Kullanıcı adı ve şifre zorunludur.");
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
            return View("Register");
        }

        var uname = username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == uname))
        {
            ModelState.AddModelError("", "Bu kullanıcı adı zaten mevcut.");
            ViewBag.ReturnUrl = SafeReturnUrl(returnUrl, "/ui");
            return View("Register");
        }

        var finalRole = DetermineRole(roleRequest, inviteCode);
        var u = new User
        {
            Username = uname,
            FullName = string.IsNullOrWhiteSpace(fullName) ? uname : fullName.Trim(),
            Role = finalRole,
            PasswordHash = _pwd.HashPassword(password),
            IsActive = true
        };
        _db.Users.Add(u);
        await _db.SaveChangesAsync();

        var next = SafeReturnUrl(returnUrl, "/roles/choose");
        return Redirect($"/account/login?returnUrl={Uri.EscapeDataString(next)}");
    }

    // ---------- Logout ----------
    [Authorize]
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogoutPost([FromQuery] string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        DeleteJwtCookie();
        return Redirect(SafeReturnUrl(returnUrl, "/account/login"));
    }

    [Authorize]
    [HttpGet("/account/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> LogoutGet([FromQuery] string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        DeleteJwtCookie();
        return Redirect(SafeReturnUrl(returnUrl, "/account/login"));
    }

    // ---------- Helpers ----------
    private string DetermineRole(string? roleRequest, string? inviteCode)
    {
        var def = _cfg["Registration:DefaultRole"] ?? Roles.Customer;

        if (string.IsNullOrWhiteSpace(roleRequest) ||
            roleRequest.Equals(Roles.Customer, StringComparison.OrdinalIgnoreCase))
            return Roles.Customer;

        var allowed = new HashSet<string>(new[]
        {
            Roles.Driver, Roles.WarehouseOperator, Roles.WarehouseManager,
            Roles.Purchasing, Roles.CrmAgent, Roles.Supplier, Roles.Admin
        }, StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(roleRequest)) return def;

        var codes = _cfg.GetSection("Registration:InviteCodes");
        var must = codes[roleRequest];
        if (!string.IsNullOrEmpty(must) && string.Equals(must, inviteCode, StringComparison.Ordinal))
            return roleRequest;

        return def;
    }

    private async Task SignInCookieAsync(User u, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.Username),
            new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(u.Role) ? Roles.Customer : u.Role),
            new Claim("sub", u.Username),
            new Claim("full_name", u.FullName ?? "")
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
    }

    private string GenerateJwt(User u)
    {
        var keyStr = _cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _cfg["Jwt:Issuer"] ?? "ml-issuer";
        var audience = _cfg["Jwt:Audience"] ?? "ml-audience";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.Username),
            new Claim(ClaimTypes.Name, u.Username),
            new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(u.Role) ? Roles.Customer : u.Role),
            new Claim("full_name", u.FullName ?? "")
        };

        var hours = int.TryParse(_cfg["Jwt:ExpireHours"], out var h) ? h : 8;
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(hours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void WriteJwtCookie(string token, bool rememberMe)
    {
        Response.Cookies.Append("jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.Add(rememberMe ? TimeSpan.FromDays(7) : TimeSpan.FromHours(8))
        });
    }

    private void DeleteJwtCookie()
        => Response.Cookies.Delete("jwt", new CookieOptions { Path = "/" });
}
