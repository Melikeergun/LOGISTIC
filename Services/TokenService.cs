using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MLYSO.Web.Models;

namespace MLYSO.Web.Services;

public class TokenService
{
    private readonly IConfiguration _cfg;
    public TokenService(IConfiguration cfg) => _cfg = cfg;

    public string CreateToken(User user)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username ?? string.Empty),
            new Claim("uid", (user.Id).ToString()),
            new Claim(ClaimTypes.Role, user.Role ?? Roles.Customer),
            new Claim("name", user.FullName ?? user.Username ?? string.Empty)
        };

        var expireHours = int.TryParse(jwt["ExpireHours"], out var h) ? h : 24;
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expireHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
