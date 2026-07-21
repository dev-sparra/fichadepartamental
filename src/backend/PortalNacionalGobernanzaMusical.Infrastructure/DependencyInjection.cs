using Microsoft.Extensions.DependencyInjection;
using PortalNacionalGobernanzaMusical.Application.Administration;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Auth;
using PortalNacionalGobernanzaMusical.Application.Catalogs;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Exports;
using PortalNacionalGobernanzaMusical.Application.Governance;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Application.Indicators;
using PortalNacionalGobernanzaMusical.Application.Workflow;
using PortalNacionalGobernanzaMusical.Infrastructure.Administration;
using PortalNacionalGobernanzaMusical.Infrastructure.Audit;
using PortalNacionalGobernanzaMusical.Infrastructure.Auth;
using PortalNacionalGobernanzaMusical.Infrastructure.Catalogs;
using PortalNacionalGobernanzaMusical.Infrastructure.Common;
using PortalNacionalGobernanzaMusical.Infrastructure.Exports;
using PortalNacionalGobernanzaMusical.Infrastructure.Governance;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;
using PortalNacionalGobernanzaMusical.Infrastructure.Indicators;
using PortalNacionalGobernanzaMusical.Infrastructure.Workflow;

namespace PortalNacionalGobernanzaMusical.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICatalogQueryService, CatalogQueryService>();
        services.AddScoped<ICatalogAdminService, CatalogAdminService>();
        services.AddScoped<IGovernanceFichaService, GovernanceFichaService>();
        services.AddScoped<IIndicatorQueryService, IndicatorQueryService>();
        services.AddScoped<IUserAdministrationService, UserAdministrationService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IFichaTemplateProvider, FichaTemplateProvider>();
        services.AddScoped<IFichaWorkbookWriter, OpenXmlFichaWriter>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<ICatalogLookupService, CatalogLookupService>();
        services.AddScoped<IWorkbookImportService, WorkbookImportService>();
        services.AddScoped<IImportTemplateService, ImportTemplateService>();

        return services;
    }
}
