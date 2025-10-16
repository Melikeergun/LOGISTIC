using System.Security.Claims;

namespace MLYSO.Web.Middleware
{
    public class EnsureRoleChosenMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string[] _safePrefixes = new[]
        {
            "/account/login", "/account/register", "/account/logout", "/account/denied",
            "/roles/choose",
            "/api/", "/swagger", "/favicon", "/robots.txt", "/sitemap", "/media/", "/css/", "/js/", "/img/", "/lib/"
        };

        public EnsureRoleChosenMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext ctx)
        {
            var path = ctx.Request.Path.ToString();
            if (_safePrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(ctx);
                return;
            }

            var isUi = path.StartsWith("/ui", StringComparison.OrdinalIgnoreCase);
            if (!isUi) { await _next(ctx); return; }

            if (ctx.User?.Identity?.IsAuthenticated == true)
            {
                var role = ctx.User.FindFirstValue(ClaimTypes.Role);
                if (string.IsNullOrWhiteSpace(role))
                {
                    ctx.Response.Redirect($"/roles/choose?returnUrl={Uri.EscapeDataString(path)}");
                    return;
                }
            }
            else
            {
                ctx.Response.Redirect($"/account/login?returnUrl={Uri.EscapeDataString(path)}");
                return;
            }

            await _next(ctx);
        }
    }
}
