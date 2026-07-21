using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Catalogs;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Catalogs;

public sealed class CatalogQueryService(ApplicationDbContext dbContext) : ICatalogQueryService
{
    public async Task<IReadOnlyCollection<DepartmentCatalogOptionDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Departments.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new DepartmentCatalogOptionDto(x.Id, x.Name, x.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MunicipalityCatalogOptionDto>> GetMunicipalitiesAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Municipalities.AsNoTracking()
            .Where(x => x.IsActive && x.DepartmentId == departmentId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new MunicipalityCatalogOptionDto(x.Id, x.DepartmentId, x.Name, x.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogOptionDto>> GetLookupAsync(string lookupName, CancellationToken cancellationToken = default)
    {
        return lookupName.ToLowerInvariant() switch
        {
            "region-ocad" => await ToLookupAsync(dbContext.RegionOcadOptions, cancellationToken),
            "committee-statuses" => await ToLookupAsync(dbContext.CommitteeStatusOptions, cancellationToken),
            "plan-statuses" => await ToLookupAsync(dbContext.PlanStatusOptions, cancellationToken),
            "priority-levels" => await ToLookupAsync(dbContext.PriorityLevelOptions, cancellationToken),
            "pnmc-axes" => await ToLookupAsync(dbContext.PnmcAxes, cancellationToken),
            "approaches" => await ToLookupAsync(dbContext.ApproachOptions, cancellationToken),
            "schedule-options" => await ToLookupAsync(dbContext.ScheduleOptions, cancellationToken),
            "proposal-statuses" => await ToLookupAsync(dbContext.ProposalStatusOptions, cancellationToken),
            "agent-types" => await ToLookupAsync(dbContext.AgentTypes, cancellationToken),
            "territorial-levels" => await ToLookupAsync(dbContext.TerritorialLevelOptions, cancellationToken),
            "information-sources" => await ToLookupAsync(dbContext.InformationSourceOptions, cancellationToken),
            "months" => await ToLookupAsync(dbContext.MonthOptions, cancellationToken),
            "years" => await GetYearsAsync(cancellationToken),
            _ => Array.Empty<CatalogOptionDto>()
        };
    }

    private async Task<IReadOnlyCollection<CatalogOptionDto>> GetYearsAsync(CancellationToken cancellationToken)
    {
        // catalog_years no tiene Name ni DisplayOrder (solo Value). Se proyecta el año como
        // Id y Name para que el formulario almacene el valor entero del año (equivalente a
        // IndicatorDetailRecord.Year), reproduciendo la lista 'Años' del Excel.
        var values = await dbContext.YearOptions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Value)
            .Select(x => x.Value)
            .ToArrayAsync(cancellationToken);

        return values
            .Select(value => new CatalogOptionDto(value, value.ToString(CultureInfo.InvariantCulture), value))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<CatalogOptionDto>> GetPnmcComponentsAsync(int pnmcAxisId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PnmcComponents.AsNoTracking()
            .Where(x => x.IsActive && x.PnmcAxisId == pnmcAxisId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CatalogOptionDto(x.Id, x.Name, x.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<CatalogOptionDto>> GetEcosystemRolesAsync(int agentTypeId, CancellationToken cancellationToken = default)
    {
        return await dbContext.EcosystemRoles.AsNoTracking()
            .Where(x => x.IsActive && x.AgentTypeId == agentTypeId)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new CatalogOptionDto(x.Id, x.Name, x.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<IndicatorDefinitionCatalogDto>> GetIndicatorDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.IndicatorDefinitions.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new IndicatorDefinitionCatalogDto(x.Id, x.ActionName, x.IndicatorName, x.TargetValue, x.DisplayOrder))
            .ToArrayAsync(cancellationToken);
    }

    private static Task<CatalogOptionDto[]> ToLookupAsync<TEntity>(DbSet<TEntity> set, CancellationToken cancellationToken)
        where TEntity : class
    {
        return set.AsNoTracking()
            .Where(x => EF.Property<bool>(x, "IsActive"))
            .OrderBy(x => EF.Property<int>(x, "DisplayOrder"))
            .Select(x => new CatalogOptionDto(EF.Property<int>(x, "Id"), EF.Property<string>(x, "Name"), EF.Property<int>(x, "DisplayOrder")))
            .ToArrayAsync(cancellationToken);
    }
}
