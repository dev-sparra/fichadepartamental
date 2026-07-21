using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.API.Middleware;

// Verifica conectividad real contra MySQL reportando Degradado/Unhealthy sin paquetes extra.
// Usa CanConnectAsync, que reutiliza el DbContext (y su EnableRetryOnFailure).
internal sealed class DatabaseHealthCheck(IServiceProvider serviceProvider) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext =
                scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("Conexión a MySQL disponible.")
                : HealthCheckResult.Unhealthy("CanConnectAsync reportó false contra MySQL.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se pudo conectar a MySQL.", ex);
        }
    }
}