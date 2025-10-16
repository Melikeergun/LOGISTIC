
using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models;

public class LoginDto
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class RegisterDto
{
    [Required] public string Username { get; set; } = "";
    [Required] public string FullName { get; set; } = "";
    [Required] public string Role { get; set; } = Roles.Customer;
    [Required] public string Password { get; set; } = "";
    [Required] public string? RoleRequest { get; set; }   
    [Required] public string? InviteCode { get; set; }    
}

public class OrderDto
{
    public string? Status { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
}

