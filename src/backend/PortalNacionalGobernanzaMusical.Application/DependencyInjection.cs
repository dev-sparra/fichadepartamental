using Microsoft.Extensions.DependencyInjection;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;

namespace PortalNacionalGobernanzaMusical.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<IFichaBlueprintProvider, FichaBlueprintProvider>();
        return services;
    }
}
