using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Catalogs;
using PortalNacionalGobernanzaMusical.Domain.Common;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Catalogs;

public sealed class CatalogAdminService(ApplicationDbContext dbContext) : ICatalogAdminService
{
    private static readonly CatalogDefinitionDto[] Definitions =
    [
        new("departments", "Departamentos", false, null),
        new("municipalities", "Municipios", true, "departments"),
        new("region-ocad", "Regiones OCAD", false, null),
        new("committee-statuses", "Estados de comité", false, null),
        new("plan-statuses", "Estados de plan", false, null),
        new("priority-levels", "Niveles de prioridad", false, null),
        new("pnmc-axes", "Ejes PNMC", false, null),
        new("pnmc-components", "Componentes PNMC", true, "pnmc-axes"),
        new("approaches", "Enfoques", false, null),
        new("schedule-options", "Opciones de cronograma", false, null),
        new("proposal-statuses", "Estados de propuesta", false, null),
        new("agent-types", "Tipos de agente", false, null),
        new("ecosystem-roles", "Roles del ecosistema", true, "agent-types"),
        new("territorial-levels", "Niveles territoriales", false, null),
        new("information-sources", "Fuentes de información", false, null),
        new("months", "Meses", false, null)
    ];

    public IReadOnlyCollection<CatalogDefinitionDto> GetCatalogDefinitions() => Definitions;

    public Task<IReadOnlyCollection<CatalogItemDto>> GetItemsAsync(string catalogKey, int? parentId, CancellationToken cancellationToken = default)
    {
        return catalogKey switch
        {
            "departments" => ToItemsAsync(dbContext.Departments, cancellationToken),
            "municipalities" => ToItemsAsync(FilterByParent(dbContext.Municipalities, parentId, x => x.DepartmentId), x => x.DepartmentId, cancellationToken),
            "region-ocad" => ToItemsAsync(dbContext.RegionOcadOptions, cancellationToken),
            "committee-statuses" => ToItemsAsync(dbContext.CommitteeStatusOptions, cancellationToken),
            "plan-statuses" => ToItemsAsync(dbContext.PlanStatusOptions, cancellationToken),
            "priority-levels" => ToItemsAsync(dbContext.PriorityLevelOptions, cancellationToken),
            "pnmc-axes" => ToItemsAsync(dbContext.PnmcAxes, cancellationToken),
            "pnmc-components" => ToItemsAsync(FilterByParent(dbContext.PnmcComponents, parentId, x => x.PnmcAxisId), x => x.PnmcAxisId, cancellationToken),
            "approaches" => ToItemsAsync(dbContext.ApproachOptions, cancellationToken),
            "schedule-options" => ToItemsAsync(dbContext.ScheduleOptions, cancellationToken),
            "proposal-statuses" => ToItemsAsync(dbContext.ProposalStatusOptions, cancellationToken),
            "agent-types" => ToItemsAsync(dbContext.AgentTypes, cancellationToken),
            "ecosystem-roles" => ToItemsAsync(FilterByParent(dbContext.EcosystemRoles, parentId, x => x.AgentTypeId), x => x.AgentTypeId, cancellationToken),
            "territorial-levels" => ToItemsAsync(dbContext.TerritorialLevelOptions, cancellationToken),
            "information-sources" => ToItemsAsync(dbContext.InformationSourceOptions, cancellationToken),
            "months" => ToItemsAsync(dbContext.MonthOptions, cancellationToken),
            _ => throw new KeyNotFoundException($"El catálogo '{catalogKey}' no existe.")
        };
    }

    public Task<CatalogItemDto> CreateAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken cancellationToken = default)
    {
        return catalogKey switch
        {
            "departments" => AddAsync(dbContext.Departments, new Department(), request, cancellationToken),
            "municipalities" => AddChildAsync(dbContext.Municipalities, new Municipality(), (e, id) => e.DepartmentId = id, e => e.DepartmentId, request, cancellationToken),
            "region-ocad" => AddAsync(dbContext.RegionOcadOptions, new RegionOcadOption(), request, cancellationToken),
            "committee-statuses" => AddAsync(dbContext.CommitteeStatusOptions, new CommitteeStatusOption(), request, cancellationToken),
            "plan-statuses" => AddAsync(dbContext.PlanStatusOptions, new PlanStatusOption(), request, cancellationToken),
            "priority-levels" => AddAsync(dbContext.PriorityLevelOptions, new PriorityLevelOption(), request, cancellationToken),
            "pnmc-axes" => AddAsync(dbContext.PnmcAxes, new PnmcAxis(), request, cancellationToken),
            "pnmc-components" => AddChildAsync(dbContext.PnmcComponents, new PnmcComponent(), (e, id) => e.PnmcAxisId = id, e => e.PnmcAxisId, request, cancellationToken),
            "approaches" => AddAsync(dbContext.ApproachOptions, new ApproachOption(), request, cancellationToken),
            "schedule-options" => AddAsync(dbContext.ScheduleOptions, new ScheduleOption(), request, cancellationToken),
            "proposal-statuses" => AddAsync(dbContext.ProposalStatusOptions, new ProposalStatusOption(), request, cancellationToken),
            "agent-types" => AddAsync(dbContext.AgentTypes, new AgentType(), request, cancellationToken),
            "ecosystem-roles" => AddChildAsync(dbContext.EcosystemRoles, new EcosystemRole(), (e, id) => e.AgentTypeId = id, e => e.AgentTypeId, request, cancellationToken),
            "territorial-levels" => AddAsync(dbContext.TerritorialLevelOptions, new TerritorialLevelOption(), request, cancellationToken),
            "information-sources" => AddAsync(dbContext.InformationSourceOptions, new InformationSourceOption(), request, cancellationToken),
            "months" => AddAsync(dbContext.MonthOptions, new MonthOption(), request, cancellationToken),
            _ => throw new KeyNotFoundException($"El catálogo '{catalogKey}' no existe.")
        };
    }

    public Task<CatalogItemDto> UpdateAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken cancellationToken = default)
    {
        return catalogKey switch
        {
            "departments" => EditAsync(dbContext.Departments, id, request, cancellationToken),
            "municipalities" => EditChildAsync(dbContext.Municipalities, id, (e, pid) => e.DepartmentId = pid, e => e.DepartmentId, request, cancellationToken),
            "region-ocad" => EditAsync(dbContext.RegionOcadOptions, id, request, cancellationToken),
            "committee-statuses" => EditAsync(dbContext.CommitteeStatusOptions, id, request, cancellationToken),
            "plan-statuses" => EditAsync(dbContext.PlanStatusOptions, id, request, cancellationToken),
            "priority-levels" => EditAsync(dbContext.PriorityLevelOptions, id, request, cancellationToken),
            "pnmc-axes" => EditAsync(dbContext.PnmcAxes, id, request, cancellationToken),
            "pnmc-components" => EditChildAsync(dbContext.PnmcComponents, id, (e, pid) => e.PnmcAxisId = pid, e => e.PnmcAxisId, request, cancellationToken),
            "approaches" => EditAsync(dbContext.ApproachOptions, id, request, cancellationToken),
            "schedule-options" => EditAsync(dbContext.ScheduleOptions, id, request, cancellationToken),
            "proposal-statuses" => EditAsync(dbContext.ProposalStatusOptions, id, request, cancellationToken),
            "agent-types" => EditAsync(dbContext.AgentTypes, id, request, cancellationToken),
            "ecosystem-roles" => EditChildAsync(dbContext.EcosystemRoles, id, (e, pid) => e.AgentTypeId = pid, e => e.AgentTypeId, request, cancellationToken),
            "territorial-levels" => EditAsync(dbContext.TerritorialLevelOptions, id, request, cancellationToken),
            "information-sources" => EditAsync(dbContext.InformationSourceOptions, id, request, cancellationToken),
            "months" => EditAsync(dbContext.MonthOptions, id, request, cancellationToken),
            _ => throw new KeyNotFoundException($"El catálogo '{catalogKey}' no existe.")
        };
    }

    public Task DeleteAsync(string catalogKey, int id, CancellationToken cancellationToken = default)
    {
        return catalogKey switch
        {
            "departments" => DeactivateAsync(dbContext.Departments, id, cancellationToken),
            "municipalities" => DeactivateAsync(dbContext.Municipalities, id, cancellationToken),
            "region-ocad" => DeactivateAsync(dbContext.RegionOcadOptions, id, cancellationToken),
            "committee-statuses" => DeactivateAsync(dbContext.CommitteeStatusOptions, id, cancellationToken),
            "plan-statuses" => DeactivateAsync(dbContext.PlanStatusOptions, id, cancellationToken),
            "priority-levels" => DeactivateAsync(dbContext.PriorityLevelOptions, id, cancellationToken),
            "pnmc-axes" => DeactivateAsync(dbContext.PnmcAxes, id, cancellationToken),
            "pnmc-components" => DeactivateAsync(dbContext.PnmcComponents, id, cancellationToken),
            "approaches" => DeactivateAsync(dbContext.ApproachOptions, id, cancellationToken),
            "schedule-options" => DeactivateAsync(dbContext.ScheduleOptions, id, cancellationToken),
            "proposal-statuses" => DeactivateAsync(dbContext.ProposalStatusOptions, id, cancellationToken),
            "agent-types" => DeactivateAsync(dbContext.AgentTypes, id, cancellationToken),
            "ecosystem-roles" => DeactivateAsync(dbContext.EcosystemRoles, id, cancellationToken),
            "territorial-levels" => DeactivateAsync(dbContext.TerritorialLevelOptions, id, cancellationToken),
            "information-sources" => DeactivateAsync(dbContext.InformationSourceOptions, id, cancellationToken),
            "months" => DeactivateAsync(dbContext.MonthOptions, id, cancellationToken),
            _ => throw new KeyNotFoundException($"El catálogo '{catalogKey}' no existe.")
        };
    }

    private static IQueryable<TEntity> FilterByParent<TEntity>(IQueryable<TEntity> query, int? parentId, Expression<Func<TEntity, int>> parentSelector)
    {
        if (parentId is null) return query;
        var parameter = parentSelector.Parameters[0];
        var predicate = Expression.Lambda<Func<TEntity, bool>>(
            Expression.Equal(parentSelector.Body, Expression.Constant(parentId.Value)),
            parameter);
        return query.Where(predicate);
    }

    private static async Task<IReadOnlyCollection<CatalogItemDto>> ToItemsAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        return await query.AsNoTracking()
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
            .Select(x => new CatalogItemDto(x.Id, x.Name, x.DisplayOrder, x.IsActive, null))
            .ToArrayAsync(cancellationToken);
    }

    private static async Task<IReadOnlyCollection<CatalogItemDto>> ToItemsAsync<TEntity>(IQueryable<TEntity> query, Func<TEntity, int> parentSelector, CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        var items = await query.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name).ToArrayAsync(cancellationToken);
        return items.Select(x => new CatalogItemDto(x.Id, x.Name, x.DisplayOrder, x.IsActive, parentSelector(x))).ToArray();
    }

    private async Task<CatalogItemDto> AddAsync<TEntity>(DbSet<TEntity> set, TEntity entity, UpsertCatalogItemRequest request, CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        ApplyRequest(entity, request);
        set.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(entity.Id, entity.Name, entity.DisplayOrder, entity.IsActive, null);
    }

    private async Task<CatalogItemDto> AddChildAsync<TEntity>(
        DbSet<TEntity> set,
        TEntity entity,
        Action<TEntity, int> setParent,
        Func<TEntity, int> getParent,
        UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        if (request.ParentId is null)
        {
            throw new ArgumentException("Este catálogo requiere seleccionar un elemento padre.");
        }

        ApplyRequest(entity, request);
        setParent(entity, request.ParentId.Value);
        set.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(entity.Id, entity.Name, entity.DisplayOrder, entity.IsActive, getParent(entity));
    }

    private async Task<CatalogItemDto> EditAsync<TEntity>(DbSet<TEntity> set, int id, UpsertCatalogItemRequest request, CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        var entity = await set.SingleAsync(x => x.Id == id, cancellationToken);
        ApplyRequest(entity, request);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(entity.Id, entity.Name, entity.DisplayOrder, entity.IsActive, null);
    }

    private async Task<CatalogItemDto> EditChildAsync<TEntity>(
        DbSet<TEntity> set,
        int id,
        Action<TEntity, int> setParent,
        Func<TEntity, int> getParent,
        UpsertCatalogItemRequest request,
        CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        var entity = await set.SingleAsync(x => x.Id == id, cancellationToken);
        ApplyRequest(entity, request);
        if (request.ParentId.HasValue)
        {
            setParent(entity, request.ParentId.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogItemDto(entity.Id, entity.Name, entity.DisplayOrder, entity.IsActive, getParent(entity));
    }

    private async Task DeactivateAsync<TEntity>(DbSet<TEntity> set, int id, CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        var entity = await set.SingleAsync(x => x.Id == id, cancellationToken);
        entity.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyRequest(CatalogEntityBase entity, UpsertCatalogItemRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
    }
}
