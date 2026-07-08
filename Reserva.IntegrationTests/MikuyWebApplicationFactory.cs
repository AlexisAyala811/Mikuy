using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reserva.Infrastructure.Persistence;

namespace Reserva.IntegrationTests;

internal sealed class MikuyWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReservationDbContext>>();
            services.RemoveAll<ReservationDbContext>();
            services.AddDbContext<ReservationDbContext>(options =>
                options.UseNpgsql(connectionString));
        });
    }
}
