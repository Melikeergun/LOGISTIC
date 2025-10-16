using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PasswordService _pwd;
    private readonly TokenService _token;
    private readonly IConfiguration _cfg;

    public AuthApiController(AppDbContext db, PasswordService pwd, TokenService token, IConfiguration cfg)
    { _db = db; _pwd = pwd; _token = token; _cfg = cfg; }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "username_password_required" });

        var username = dto.Username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Conflict(new { error = "username_exists" });

        var finalRole = DetermineRole(dto.RoleRequest, dto.InviteCode);
        var user = new User
        {
            Username = username,
            FullName = string.IsNullOrWhiteSpace(dto.FullName) ? username : dto.FullName!.Trim(),
            Role = finalRole,
            PasswordHash = _pwd.HashPassword(dto.Password),
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return Ok(new { message = "created", user.Id, user.Username, user.Role });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { error = "invalid_credentials" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username && u.IsActive);
        if (user == null) return Unauthorized(new { error = "invalid_credentials" });

        if (!_pwd.Verify(dto.Password, user.PasswordHash ?? "", out var upgraded))
            return Unauthorized(new { error = "invalid_credentials" });

        if (upgraded != null) { user.PasswordHash = upgraded; await _db.SaveChangesAsync(); }

        var jwt = _token.CreateToken(user);
        return Ok(new { token = jwt, user = new { user.Id, user.Username, user.FullName, user.Role } });
    }


    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var me = new
        {
            Username = User.Identity?.Name ?? User.FindFirst("sub")?.Value,
            Role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown",
            FullName = User.FindFirst("full_name")?.Value ?? (User.Identity?.Name ?? "")
        };
        return Ok(me);
    }

    [HttpGet("roles")]
    [AllowAnonymous]
    public IActionResult GetRoles() => Ok(new[]
    {
        Roles.Customer, Roles.Driver, Roles.WarehouseOperator, Roles.WarehouseManager,
        Roles.Purchasing, Roles.CrmAgent, Roles.Supplier, Roles.Logistics, Roles.Operations,
        Roles.Planning, Roles.CustomerService, Roles.WarehouseChief, Roles.Admin
    });

    private string DetermineRole(string? roleRequest, string? inviteCode)
    {
        var def = _cfg["Registration:DefaultRole"] ?? Roles.Customer;
        if (string.IsNullOrWhiteSpace(roleRequest) || roleRequest.Equals(Roles.Customer, StringComparison.OrdinalIgnoreCase))
            return Roles.Customer;

        var allowed = new HashSet<string>(new[]
        { Roles.Driver, Roles.WarehouseOperator, Roles.WarehouseManager, Roles.Purchasing, Roles.CrmAgent, Roles.Supplier },
        StringComparer.OrdinalIgnoreCase);

        if (!allowed.Contains(roleRequest)) return def;

        var codes = _cfg.GetSection("Registration:InviteCodes");
        var must = codes[roleRequest];
        if (!string.IsNullOrEmpty(must) && string.Equals(must, inviteCode, StringComparison.Ordinal))
            return roleRequest;

        return def;
    }
}
