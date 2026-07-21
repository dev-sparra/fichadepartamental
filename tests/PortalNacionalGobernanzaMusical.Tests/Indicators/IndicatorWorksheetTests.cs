using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Infrastructure.Indicators;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Tests.Indicators;

/// <summary>
/// Verifica el diligenciamiento del Líder: provisionar la hoja de indicadores materializa los 7
/// indicadores fijos (con sus 12 meses) y los 13 detalles desde el catálogo, y es idempotente.
/// </summary>
public sealed class IndicatorWorksheetTests
{
    private static ApplicationDbContext NewSeededContext()
    {
        var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        context.Departments.Add(new Department { Id = 5, Name = "Antioquia", DisplayOrder = 1, IsActive = true });

        for (var i = 1; i <= 7; i++)
        {
            context.IndicatorDefinitions.Add(new IndicatorDefinition
            {
                Id = i,
                ActionName = $"Acción {i}",
                IndicatorName = $"Indicador {i}",
                TargetValue = 1,
                DisplayOrder = i,
                IsActive = true
            });
        }

        for (var i = 1; i <= 12; i++)
        {
            context.MonthOptions.Add(new MonthOption { Id = i, Name = $"Mes {i}", DisplayOrder = i, IsActive = true });
        }

        var templateId = 1;
        foreach (var (definitionId, count) in new[] { (4, 5), (5, 4), (6, 4) })
        {
            for (var sort = 1; sort <= count; sort++)
            {
                context.IndicatorDetailTemplates.Add(new IndicatorDetailTemplate
                {
                    Id = templateId++,
                    IndicatorDefinitionId = definitionId,
                    SortOrder = sort,
                    FormulaLabel = $"Fórmula {templateId}",
                    DetailDescription = $"Detalle {templateId}"
                });
            }
        }

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task Provision_ShouldMaterializeSevenIndicatorsWithTwelveMonthsAndThirteenDetails()
    {
        using var context = NewSeededContext();
        var service = new IndicatorQueryService(context);

        var worksheet = await service.ProvisionWorksheetAsync(departmentId: 5, year: 2026);

        Assert.Equal(7, worksheet.Records.Count);
        Assert.All(worksheet.Records, record => Assert.Equal(12, record.MonthlyProgresses.Count));
        Assert.Equal(13, worksheet.Details.Count);
    }

    [Fact]
    public async Task Provision_ShouldBeIdempotent()
    {
        using var context = NewSeededContext();
        var service = new IndicatorQueryService(context);

        await service.ProvisionWorksheetAsync(5, 2026);
        await service.ProvisionWorksheetAsync(5, 2026);

        Assert.Equal(7, await context.IndicatorRecords.CountAsync());
        Assert.Equal(13, await context.IndicatorDetailRecords.CountAsync());
    }
}
