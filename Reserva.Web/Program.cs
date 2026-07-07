using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Reserva.Domain.Interfaces;
using Reserva.Infrastructure.Persistence;
using Reserva.Infrastructure.Repositories;
using Reserva.Web.Mapping;
using Reserva.Web.Models;
using Reserva.Web.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(railwayPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{railwayPort}");
}

var connectionString = GetPostgresConnectionString(builder.Configuration, builder.Environment);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.AccessDeniedPath = "/Cuenta/Login";
        options.Cookie.Name = "Mikuy.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("public-reservations", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.AddPolicy("admin-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddDbContext<ReservationDbContext>(options =>
    options.UseNpgsql(
        connectionString,
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IReservationNotificationService, ReservationNotificationService>();
builder.Services.AddScoped<IReservationReceiptService, ReservationReceiptService>();
builder.Services.AddAutoMapper(configuration => configuration.AddProfile<ReservationMappingProfile>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ReservationDbContext>();
    await dbContext.Database.MigrateAsync();
    await DataSeeder.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

static string GetPostgresConnectionString(IConfiguration configuration, IWebHostEnvironment environment)
{
    var databaseUrl = FirstEnvironmentValue(
        "DATABASE_URL",
        "DATABASE_PRIVATE_URL",
        "POSTGRES_URL",
        "POSTGRESQL_URL");

    var fromUrl = BuildPostgresConnectionString(databaseUrl);
    if (!string.IsNullOrWhiteSpace(fromUrl))
    {
        return fromUrl;
    }

    var fromVariables = BuildPostgresConnectionStringFromVariables();
    if (!string.IsNullOrWhiteSpace(fromVariables))
    {
        return fromVariables;
    }

    var configured = configuration.GetConnectionString("DefaultConnection");
    if (environment.IsProduction() && IsLocalhostConnection(configured))
    {
        throw new InvalidOperationException(
            "No se encontro una conexion PostgreSQL para produccion. En Railway agregue un servicio PostgreSQL y configure DATABASE_URL o las variables PGHOST, PGPORT, PGDATABASE, PGUSER y PGPASSWORD en el servicio web.");
    }

    return configured ?? throw new InvalidOperationException("No se encontro una cadena de conexion PostgreSQL.");
}

static string? FirstEnvironmentValue(params string[] names)
{
    foreach (var name in names)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

static string? BuildPostgresConnectionString(string? databaseUrl)
{
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        return null;
    }

    if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return databaseUrl;
    }

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
    var database = uri.AbsolutePath.TrimStart('/');

    return new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = database,
        Username = username,
        Password = password,
        SslMode = Npgsql.SslMode.Prefer
    }.ConnectionString;
}

static string? BuildPostgresConnectionStringFromVariables()
{
    var host = FirstEnvironmentValue("PGHOST", "POSTGRES_HOST");
    var database = FirstEnvironmentValue("PGDATABASE", "POSTGRES_DB", "POSTGRES_DATABASE");
    var username = FirstEnvironmentValue("PGUSER", "POSTGRES_USER");
    var password = FirstEnvironmentValue("PGPASSWORD", "POSTGRES_PASSWORD");

    if (string.IsNullOrWhiteSpace(host) ||
        string.IsNullOrWhiteSpace(database) ||
        string.IsNullOrWhiteSpace(username))
    {
        return null;
    }

    var portValue = FirstEnvironmentValue("PGPORT", "POSTGRES_PORT");
    var port = int.TryParse(portValue, out var parsedPort) ? parsedPort : 5432;

    return new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = database,
        Username = username,
        Password = password,
        SslMode = Npgsql.SslMode.Prefer
    }.ConnectionString;
}

static bool IsLocalhostConnection(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
    return string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(builder.Host, "::1", StringComparison.OrdinalIgnoreCase);
}
