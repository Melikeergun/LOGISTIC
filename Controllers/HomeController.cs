using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MLYSO.Web.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    public IActionResult Landing(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            var target = string.IsNullOrWhiteSpace(returnUrl) ? "/ui/hub" : returnUrl;
            var role = User.FindFirstValue(ClaimTypes.Role);
            if (string.IsNullOrWhiteSpace(role))
                return Redirect($"/roles/choose?returnUrl={Uri.EscapeDataString(target)}");
            return Redirect(target);
        }

        ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/ui/hub" : returnUrl;
        return View();
    }

    [Authorize]
    public IActionResult Index(string? returnUrl = null)
    {
        var target = string.IsNullOrWhiteSpace(returnUrl) ? "/ui/hub" : returnUrl;
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (string.IsNullOrWhiteSpace(role))
            return Redirect($"/roles/choose?returnUrl={Uri.EscapeDataString(target)}");
        return Redirect(target);
    }

    [AllowAnonymous]
    [HttpGet("privacy")]
    public IActionResult Privacy() => View();
}
