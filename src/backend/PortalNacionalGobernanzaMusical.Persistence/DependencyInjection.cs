using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace PortalNacionalGobernanzaMusical.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection' en la configuración.");

        // AutoDetect abre una conexión contra MySQL al resolver la versión. Si el servidor
        // no está disponible al arrancar, no debe tirar la API: se cae a una versión
        // conocida (configurable) y el health check reportará "unhealthy". Los queries
        // posteriores reintentan gracias a EnableRetryOnFailure.
        var serverVersion = ResolveServerVersion(connectionString, configuration);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseMySql(connectionString, serverVersion, mySql =>
            {
                // Resiliencia ante fallos transitorios de conexión (timeouts, deadlocks,
                // caídas breves de red o del servidor MySQL).
                mySql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);

                // Timeout por comando (segundos) para consultas largas. El default 30s puede
                // ser corto cuando el ecosistema musical crece.
                mySql.CommandTimeout(60);
            });
        });

        return services;
    }

    private static ServerVersion ResolveServerVersion(string connectionString, IConfiguration configuration)
    {
        var configuredVersion = configuration["ConnectionStrings:MySqlServerVersion"];
        if (!string.IsNullOrWhiteSpace(configuredVersion))
        {
            return ServerVersion.Parse(configuredVersion);
        }

        try
        {
            return ServerVersion.AutoDetect(connectionString);
        }
        catch (Exception ex)
        {
            // Registro best-effort: el logger de DI no está disponible todavía, por eso
            // se deja la traza en la consola. La API arranca contra una versión conocida.
            Console.WriteLine(
                $"[Persistence] No se pudo detectar la versión de MySQL ({ex.Message}). " +
                "Usando versión por defecto 8.0.42. Configure 'ConnectionStrings:MySqlServerVersion' para anular.");

            return ServerVersion.Parse("8.0.42-mysql");
        }
    }
}