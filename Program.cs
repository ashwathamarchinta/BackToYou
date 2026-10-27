using BackToYou.Data;
using BackToYou.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database: PostgreSQL in production, SQLite in development ──────────────
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // Production: Render / Vercel — parse PostgreSQL connection string safely
    var normalizedUrl = databaseUrl.Replace("postgres://", "postgresql://");
    var uri = new Uri(normalizedUrl);
    var pgPort = uri.Port > 0 ? uri.Port : 5432;
    var user = Uri.UnescapeDataString(uri.UserInfo.Split(':')[0]);
    var pass = uri.UserInfo.Contains(':') 
        ? Uri.UnescapeDataString(uri.UserInfo.Substring(uri.UserInfo.IndexOf(':') + 1)) 
        : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');

    var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = pgPort,
        Database = database,
        Username = user,
        Password = pass,
        SslMode = Npgsql.SslMode.Require
    };

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(npgsqlBuilder.ConnectionString));
}
else
{
    // Development: SQLite
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=lostfound.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}

// ── Identity ───────────────────────────────────────────────────────────────
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings suitable for college demo
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

// ── Production CORS Configuration (Allow Vercel frontend) ───────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelAndProduction", policy =>
    {
        var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");

        string[] origins;

        if (!string.IsNullOrWhiteSpace(allowedOrigins))
        {
            origins = allowedOrigins.Split(
                new[] { ',', ';' },
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);
        }
        else
        {
            origins = new[]
            {
                "https://backtoyou-web-9sli.onrender.com",
                "https://backtoyou-iota.vercel.app"
            };
        }

        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// ── Listen on PORT env var (required by Render & Vercel containers) ────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// ── Seed Database ──────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// ── HTTP Pipeline ──────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseCors("AllowVercelAndProduction");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ── Health Check Endpoints ────────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName,
    service = "BackToYou"
}));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
