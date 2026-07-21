using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Indicators;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Indicators;

public sealed class IndicatorQueryService(ApplicationDbContext dbContext) : IIndicatorQueryService
{
    public async Task<IReadOnlyCollection<IndicatorRecordDto>> GetIndicatorRecordsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.IndicatorRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.MonthlyProgresses).ThenInclude(x => x.MonthOption)
            .OrderByDescending(x => x.Year).ThenBy(x => x.IndicatorDefinition!.DisplayOrder)
            .ThenBy(x => x.Department!.DisplayOrder)
            .ToArrayAsync(cancellationToken);

        return entities.Select(MapRecord).ToArray();
    }

    public async Task<IndicatorRecordDto?> GetIndicatorRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.IndicatorRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.MonthlyProgresses).ThenInclude(x => x.MonthOption)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapRecord(entity);
    }

    public async Task<IndicatorRecordDto> UpdateIndicatorRecordAsync(Guid id, UpdateIndicatorRecordRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.IndicatorRecords
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.Department)
            .Include(x => x.MonthlyProgresses).ThenInclude(x => x.MonthOption)
            .SingleAsync(x => x.Id == id, cancellationToken);

        entity.Source = request.Source;
        entity.GeneralObservations = request.GeneralObservations;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var months = await dbContext.MonthOptions.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var monthsById = months.ToDictionary(x => x.Id);
        var existingByMonth = entity.MonthlyProgresses.ToDictionary(x => x.MonthOptionId);
        var requested = request.MonthlyProgresses.ToList();

        for (var i = 0; i < months.Count && i < requested.Count; i++)
        {
            if (existingByMonth.TryGetValue(months[i].Id, out var existing))
            {
                existing.QuantitativeAdvance = requested[i].QuantitativeAdvance;
                existing.Detail = requested[i].Detail;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entity.MonthlyProgresses.Add(new IndicatorMonthlyProgress
                {
                    IndicatorRecordId = entity.Id,
                    MonthOptionId = months[i].Id,
                    MonthOption = monthsById[months[i].Id],
                    QuantitativeAdvance = requested[i].QuantitativeAdvance,
                    Detail = requested[i].Detail
                });
            }
        }

        RecalculateIndicator(entity);

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapRecord(entity);
    }

    public async Task<IReadOnlyCollection<IndicatorDetailRecordDto>> GetDetailRecordsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.IndicatorDetailRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.IndicatorDetailTemplate)
            .Include(x => x.SelectedMonths).ThenInclude(x => x.MonthOption)
            .OrderByDescending(x => x.Year).ThenBy(x => x.IndicatorDefinition!.DisplayOrder)
            .ThenBy(x => x.IndicatorDetailTemplate!.SortOrder)
            .ToArrayAsync(cancellationToken);

        return entities.Select(MapDetail).ToArray();
    }

    public async Task<IndicatorDetailRecordDto?> GetDetailRecordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.IndicatorDetailRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.IndicatorDetailTemplate)
            .Include(x => x.SelectedMonths).ThenInclude(x => x.MonthOption)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : MapDetail(entity);
    }

    public async Task<IndicatorDetailRecordDto> UpdateDetailRecordAsync(Guid id, UpdateIndicatorDetailRecordRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.IndicatorDetailRecords
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.IndicatorDetailTemplate)
            .Include(x => x.SelectedMonths)
            .SingleAsync(x => x.Id == id, cancellationToken);

        entity.Source = request.Source;
        entity.Observations = request.Observations;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        var requestedMonthIds = request.SelectedMonthIds.Distinct().ToHashSet();
        var toRemove = entity.SelectedMonths.Where(x => !requestedMonthIds.Contains(x.MonthOptionId)).ToList();
        foreach (var item in toRemove)
        {
            dbContext.IndicatorDetailRecordMonths.Remove(item);
            entity.SelectedMonths.Remove(item);
        }

        // Commit deletions first — EF Core executes INSERTs before DELETEs in a single
        // SaveChanges call, which would violate the unique constraint on (record_id, month_id).
        await dbContext.SaveChangesAsync(cancellationToken);

        var existingMonthIds = entity.SelectedMonths.Select(x => x.MonthOptionId).ToHashSet();
        foreach (var monthId in requestedMonthIds.Except(existingMonthIds))
        {
            entity.SelectedMonths.Add(new IndicatorDetailRecordMonth
            {
                IndicatorDetailRecordId = entity.Id,
                MonthOptionId = monthId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapDetail(entity);
    }

    public async Task<IndicatorWorksheetDto> ProvisionWorksheetAsync(int departmentId, int year, CancellationToken cancellationToken = default)
    {
        var definitions = await dbContext.IndicatorDefinitions.Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var months = await dbContext.MonthOptions.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);
        var templates = await dbContext.IndicatorDetailTemplates
            .OrderBy(x => x.IndicatorDefinitionId).ThenBy(x => x.SortOrder).ToListAsync(cancellationToken);

        var existingDefinitionIds = await dbContext.IndicatorRecords
            .Where(x => x.DepartmentId == departmentId && x.Year == year)
            .Select(x => x.IndicatorDefinitionId).ToListAsync(cancellationToken);

        foreach (var definition in definitions.Where(d => !existingDefinitionIds.Contains(d.Id)))
        {
            var record = new IndicatorRecord
            {
                DepartmentId = departmentId,
                IndicatorDefinitionId = definition.Id,
                Year = year
            };

            foreach (var month in months)
            {
                record.MonthlyProgresses.Add(new IndicatorMonthlyProgress { MonthOptionId = month.Id });
            }

            dbContext.IndicatorRecords.Add(record);
        }

        var existingTemplateIds = await dbContext.IndicatorDetailRecords
            .Where(x => x.DepartmentId == departmentId && x.Year == year)
            .Select(x => x.IndicatorDetailTemplateId).ToListAsync(cancellationToken);

        foreach (var template in templates.Where(t => !existingTemplateIds.Contains(t.Id)))
        {
            dbContext.IndicatorDetailRecords.Add(new IndicatorDetailRecord
            {
                DepartmentId = departmentId,
                IndicatorDefinitionId = template.IndicatorDefinitionId,
                IndicatorDetailTemplateId = template.Id,
                Year = year
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildWorksheetAsync(departmentId, year, cancellationToken);
    }

    private async Task<IndicatorWorksheetDto> BuildWorksheetAsync(int departmentId, int year, CancellationToken cancellationToken)
    {
        var records = await dbContext.IndicatorRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.MonthlyProgresses).ThenInclude(x => x.MonthOption)
            .Where(x => x.DepartmentId == departmentId && x.Year == year)
            .ToArrayAsync(cancellationToken);

        var details = await dbContext.IndicatorDetailRecords.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.IndicatorDefinition)
            .Include(x => x.IndicatorDetailTemplate)
            .Include(x => x.SelectedMonths).ThenInclude(x => x.MonthOption)
            .Where(x => x.DepartmentId == departmentId && x.Year == year)
            .ToArrayAsync(cancellationToken);

        return new IndicatorWorksheetDto(
            records.OrderBy(x => x.IndicatorDefinition?.DisplayOrder ?? 0).Select(MapRecord).ToArray(),
            details.OrderBy(x => x.IndicatorDefinition?.DisplayOrder ?? 0)
                .ThenBy(x => x.IndicatorDetailTemplate?.SortOrder ?? 0)
                .Select(MapDetail).ToArray());
    }

    private static void RecalculateIndicator(IndicatorRecord entity)
    {
        var advances = entity.MonthlyProgresses
            .Select(x => x.QuantitativeAdvance)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToArray();

        var target = entity.IndicatorDefinition?.TargetValue ?? 1;

        entity.CurrentValueCalculated = target <= 1
            ? (advances.Length > 0 ? advances.Max() : 0m)
            : advances.Sum();

        entity.CompliancePercentageCalculated = target == 0
            ? 0
            : entity.CurrentValueCalculated / target;
    }

    private static IndicatorRecordDto MapRecord(IndicatorRecord x) => new(
        x.Id, x.DepartmentId,
        x.Department?.Name ?? string.Empty,
        x.IndicatorDefinitionId,
        x.IndicatorDefinition?.IndicatorName ?? string.Empty,
        x.IndicatorDefinition?.ActionName ?? string.Empty,
        x.IndicatorDefinition?.TargetValue ?? 0,
        x.Year, x.Source, x.GeneralObservations,
        x.CurrentValueCalculated, x.CompliancePercentageCalculated,
        x.MonthlyProgresses.OrderBy(m => m.MonthOption!.DisplayOrder)
            .Select(m => new IndicatorMonthlyProgressDto(
                m.Id, m.MonthOptionId,
                m.MonthOption?.Name ?? string.Empty,
                m.QuantitativeAdvance, m.Detail)).ToArray());

    private static IndicatorDetailRecordDto MapDetail(IndicatorDetailRecord x) => new(
        x.Id, x.DepartmentId,
        x.Department?.Name ?? string.Empty,
        x.IndicatorDefinitionId,
        x.IndicatorDefinition?.IndicatorName ?? string.Empty,
        x.IndicatorDetailTemplateId,
        x.IndicatorDetailTemplate?.FormulaLabel ?? string.Empty,
        x.IndicatorDetailTemplate?.DetailDescription ?? string.Empty,
        x.Year, x.Source, x.Observations,
        x.CurrentValueCalculated,
        x.SelectedMonths.Select(s => s.MonthOptionId).ToArray());
}
