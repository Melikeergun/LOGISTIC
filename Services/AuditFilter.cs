using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Serilog;
using MLYSO.Web.Models;
using MLYSO.Web.Attributes;

namespace MLYSO.Web.Services
{
    // IAsyncActionFilter: DB’ye yazarken async bekleyebilelim
    public class AuditFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _db;
        private static readonly HashSet<string> _skipPrefixes = new(StringComparer.OrdinalIgnoreCase)
        { "/swagger", "/css", "/js", "/lib", "/img", "/favicon.ico", "/health" };

        public AuditFilter(AppDbContext db) => _db = db;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;

            // Controller/Action üzerinde [SkipAudit] varsa ya da static/swagger ise loglama
            var hasSkipAttr =
                context.ActionDescriptor.EndpointMetadata.OfType<SkipAuditAttribute>().Any() ||
                _skipPrefixes.Any(p => http.Request.Path.StartsWithSegments(p));
            if (hasSkipAttr)
            {
                await next();
                return;
            }

            var sw = Stopwatch.StartNew();
            string? reqBody = null;
            var method = http.Request.Method.ToUpperInvariant();
            var path = http.Request.Path + http.Request.QueryString;

            // Hassas endpoint’leri maskele
            var isSensitive = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase);

            // JSON gövdesini küçük/bounded şekilde oku (POST/PUT/PATCH)
            if (!isSensitive &&
                (method == "POST" || method == "PUT" || method == "PATCH") &&
                (http.Request.ContentType?.Contains("application/json", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                try
                {
                    http.Request.EnableBuffering();
                    using var reader = new StreamReader(http.Request.Body, leaveOpen: true);
                    var raw = await reader.ReadToEndAsync();
                    http.Request.Body.Position = 0;
                    if (!string.IsNullOrWhiteSpace(raw))
                        reqBody = raw.Length > 4096 ? raw[..4096] + "…(truncated)" : raw;
                }
                catch { /* body okunamazsa sessizce geç */ }
            }

            // Devam et (action çalışsın)
            Exception? error = null;
            var executed = await next();
            if (executed.Exception != null && !executed.ExceptionHandled)
                error = executed.Exception;

            sw.Stop();

            // Kimlik bilgileri
            string? userName = http.User?.Identity?.IsAuthenticated == true
                ? (http.User.Identity?.Name ?? http.User.FindFirst("name")?.Value ?? http.User.FindFirst(ClaimTypes.Name)?.Value)
                : null;

            string? role =
                http.User?.FindFirst(ClaimTypes.Role)?.Value ??
                http.User?.FindFirst("role")?.Value;

            // Basit alanlar
            http.Request.Headers.TryGetValue("User-Agent", out StringValues ua);
            var logRow = new AuditLog
            {
                UtcTs = DateTime.UtcNow,
                UserName = userName,
                UserRole = role,
                Method = method,
                Path = path,
                StatusCode = http.Response?.StatusCode ?? 0,
                DurationMs = (int)sw.Elapsed.TotalMilliseconds,
                Ip = http.Connection.RemoteIpAddress?.ToString(),
                UserAgent = ua.ToString(),
                CorrelationId = http.TraceIdentifier,
                RequestBody = reqBody,
                IsError = error != null,
                Error = error?.Message
            };

            try
            {
                _db.AuditLogs.Add(logRow);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // DB yazılamazsa en azından konsola düşsün
                Log.Warning(ex, "AUDIT DB write failed");
            }

            // Konsola kısa log
            Log.Information("AUDIT {Method} {Path} {Status} {Duration}ms {User}",
                logRow.Method, logRow.Path, logRow.StatusCode, logRow.DurationMs, logRow.UserName ?? "(anon)");
        }
    }
}
