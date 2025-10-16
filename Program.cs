using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MLYSO.Web.Data;
using MLYSO.Web.Models;
using MLYSO.Web.Services;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http; // <-- eklendi (SameSiteMode için)

var builder = WebApplication.CreateBuilder(args);

// --- Serilog ayný ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddScoped<AuditFilter>();
builder.Services.AddControllersWithViews(o => o.Filters.Add<AuditFilter>());

// *** Baðlantý dizesi: Default yoksa DefaultConnection'a, o da yoksa app.db'ye düþ ***
var connStr =
    builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=app.db";

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(connStr));

builder.Services.AddSingleton<CsvOptionService>();
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<EtlService>();
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<ChurnService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddSingleton<TrGeoService>();
builder.Services.AddScoped<RoutingService>();
builder.Services.AddHostedService<EtlSchedulerService>();

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });
    options.RejectionStatusCode = 429;
});

// ---- JWT config güvenli okuma (CS8602'yi bitirir) ----
var jwtSection = builder.Configuration.GetSection("Jwt");
string? jwtKey = jwtSection.GetValue<string>("Key");
string? jwtIssuer = jwtSection.GetValue<string>("Issuer");
string? jwtAudience = jwtSection.GetValue<string>("Audience");

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("App setting missing: Jwt:Key");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "JwtOrCookie";
        options.DefaultAuthenticateScheme = "JwtOrCookie";
        options.DefaultChallengeScheme = "JwtOrCookie";
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddPolicyScheme("JwtOrCookie", "Use JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var auth = context.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;

            if (context.Request.Cookies.ContainsKey("mlyso.auth") ||
                context.Request.Cookies.ContainsKey(".AspNetCore.Cookies") ||
                context.Request.Cookies.ContainsKey("jwt"))
                return CookieAuthenticationDefaults.AuthenticationScheme;

            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
    {
        o.LoginPath = "/account/login";
        o.LogoutPath = "/account/logout";
        o.AccessDeniedPath = "/account/denied";
        o.SlidingExpiration = true;
        o.Cookie.Name = "mlyso.auth";
        o.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var auth = ctx.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var cookieToken = ctx.Request.Cookies["jwt"];
                    if (!string.IsNullOrEmpty(cookieToken))
                        ctx.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

var auth = builder.Services.AddAuthorizationBuilder();
auth.AddPolicy("CanChangeStatus", p => p.RequireRole(Roles.Admin));
auth.AddPolicy("WarehouseManagerOrAdmin", p => p.RequireRole(Roles.WarehouseManager, Roles.Admin));
auth.AddPolicy("WarehouseOperatorOrAdmin", p => p.RequireRole(Roles.WarehouseOperator, Roles.Admin));
auth.AddPolicy("CustomerLimited", p => p.RequireRole(Roles.Customer, Roles.CustomerService, Roles.Admin));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MLYSO Logistics API", Version = "v1" });
    c.CustomSchemaIds(t => t.FullName);
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Sadece token girin. Örn: Bearer eyJhbGciOiJIUzI1NiIs..."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id="Bearer"} }, Array.Empty<string>() }
    });
});

var app = builder.Build();

// --- Açýlýþta migrate + temel seed ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();                 // <— tablolarý oluþtur (Twin dahil)
    await Seed.EnsureAsync(db);            // mevcut seed'in
    // Twin seed'ini TwinController.Index de çaðýrýyor; burada da çaðýrmak istersen:
    // await SeedTwin.EnsureAsync(db);
}

// ----- Pipeline -----
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseStaticFiles();
app.UseRouting();

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.Lax,  // <-- SameSitePolicy -> SameSiteMode
    HttpOnly = HttpOnlyPolicy.Always,
    Secure = CookieSecurePolicy.Always
});


app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MLYSO.Web.Middleware.EnsureRoleChosenMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MLYSO Logistics API v1");
    c.RoutePrefix = "swagger";
    c.EnableTryItOutByDefault();
});

// Açýlýþ route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.MapGet("/", ctx => { ctx.Response.Redirect("/home/landing", permanent: false); return Task.CompletedTask; });

app.Run();
