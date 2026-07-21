using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

public sealed class CatalogLookupService(ApplicationDbContext dbContext) : ICatalogLookupService
{
    public async Task<CatalogValidationSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var departments = await dbContext.Departments.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var municipalities = await dbContext.Municipalities.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var regionOcad = (await dbContext.RegionOcadOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var informationSources = (await dbContext.InformationSourceOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var committeeStatuses = (await dbContext.CommitteeStatusOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var planStatuses = (await dbContext.PlanStatusOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var priorityLevels = (await dbContext.PriorityLevelOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pnmcAxes = await dbContext.PnmcAxes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var pnmcComponents = await dbContext.PnmcComponents.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var approaches = (await dbContext.ApproachOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var schedules = (await dbContext.ScheduleOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var proposalStatuses = (await dbContext.ProposalStatusOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var agentTypes = await dbContext.AgentTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var ecosystemRoles = await dbContext.EcosystemRoles.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var territorialLevels = (await dbContext.TerritorialLevelOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var months = (await dbContext.MonthOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Name).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var years = await dbContext.YearOptions.AsNoTracking().Where(x => x.IsActive).Select(x => x.Value).ToHashSetAsync(cancellationToken);
        var indicatorNames = (await dbContext.IndicatorDefinitions.AsNoTracking().Where(x => x.IsActive).Select(x => x.IndicatorName).ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var departmentMap = departments.ToDictionary(x => x.Id, x => x.Name);
        var municipalitiesByDepartment = municipalities
            .Where(x => departmentMap.ContainsKey(x.DepartmentId))
            .GroupBy(x => departmentMap[x.DepartmentId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Name).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

        var axisMap = pnmcAxes.ToDictionary(x => x.Id, x => x.Name);
        var componentsByAxis = pnmcComponents
            .Where(x => axisMap.ContainsKey(x.PnmcAxisId))
            .GroupBy(x => axisMap[x.PnmcAxisId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Name).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

        var agentTypeMap = agentTypes.ToDictionary(x => x.Id, x => x.Name);
        var ecosystemRolesByAgentType = ecosystemRoles
            .Where(x => agentTypeMap.ContainsKey(x.AgentTypeId))
            .GroupBy(x => agentTypeMap[x.AgentTypeId], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Name).ToHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

        return new CatalogValidationSnapshot(
            departments.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
            municipalitiesByDepartment,
            regionOcad,
            informationSources,
            committeeStatuses,
            planStatuses,
            priorityLevels,
            pnmcAxes.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
            componentsByAxis,
            approaches,
            schedules,
            proposalStatuses,
            agentTypes.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase),
            ecosystemRolesByAgentType,
            territorialLevels,
            months,
            years,
            indicatorNames);
    }
}
