using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Infrastructure.Audit;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Importación completa sobre el archivo oficial: se diligencian varias filas en cada hoja de la
/// ficha y se comprueba que todas lleguen a Gobernanza. Usa EF Core InMemory (no requiere MySQL).
/// </summary>
public sealed class WorkbookImportServiceTests
{
    private const string OfficialFile = "ficha_departamental_gobernanza.xlsm";

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public string? Email => "gestor@test.gov.co";
        public string? IpAddress => "127.0.0.1";
        public string? RequestMethod => "POST";
        public string? RequestPath => "/api/imports/excel";
        public IReadOnlyCollection<string> Roles => ["Gestor Departamental"];
        public bool HasAnyRole(params string[] roles) => roles.Any(r => Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Catálogos mínimos, con los mismos nombres del seed oficial.</summary>
    private static void SeedCatalogs(ApplicationDbContext context)
    {
        context.Departments.Add(new Department { Id = 2, Name = "Antioquia", DisplayOrder = 2, IsActive = true });
        context.Municipalities.Add(new Municipality { Id = 100, DepartmentId = 2, Name = "Medellín", DisplayOrder = 1, IsActive = true });
        context.RegionOcadOptions.Add(new RegionOcadOption { Id = 1, Name = "Eje Cafetero", DisplayOrder = 1, IsActive = true });
        context.InformationSourceOptions.Add(new InformationSourceOption { Id = 1, Name = "Ente territorial", DisplayOrder = 1, IsActive = true });
        context.CommitteeStatusOptions.Add(new CommitteeStatusOption { Id = 1, Name = "Creado", DisplayOrder = 1, IsActive = true });
        context.PriorityLevelOptions.AddRange(
            new PriorityLevelOption { Id = 1, Name = "Alto", DisplayOrder = 1, IsActive = true },
            new PriorityLevelOption { Id = 2, Name = "Medio", DisplayOrder = 2, IsActive = true });
        context.PnmcAxes.AddRange(
            new PnmcAxis { Id = 2, Name = "2. Fortalecimiento de las prácticas, expresiones y oficios de la música.", DisplayOrder = 2, IsActive = true },
            new PnmcAxis { Id = 3, Name = "3. Gobernanza musical e integración cultural e intersectorial.", DisplayOrder = 3, IsActive = true });
        context.PnmcComponents.AddRange(
            new PnmcComponent { Id = 3, PnmcAxisId = 2, Name = "Formación.", DisplayOrder = 1, IsActive = true },
            new PnmcComponent { Id = 9, PnmcAxisId = 3, Name = "Participación ciudadana, intersectorialidad y articulación territorial.", DisplayOrder = 1, IsActive = true });
        context.ApproachOptions.AddRange(
            new ApproachOption { Id = 1, Name = "Diferencial", DisplayOrder = 1, IsActive = true },
            new ApproachOption { Id = 4, Name = "Poblacional", DisplayOrder = 4, IsActive = true });
        context.AgentTypes.AddRange(
            new AgentType { Id = 2, Name = "Institucional - Agente Externo", DisplayOrder = 2, IsActive = true },
            new AgentType { Id = 3, Name = "Sectorial", DisplayOrder = 3, IsActive = true });
        context.EcosystemRoles.AddRange(
            new EcosystemRole { Id = 8, AgentTypeId = 2, Name = "Alcaldías", DisplayOrder = 1, IsActive = true },
            new EcosystemRole { Id = 10, AgentTypeId = 2, Name = "Secretarías Locales de Cultura", DisplayOrder = 3, IsActive = true },
            // Este rol contiene ", ", el mismo separador de la selección múltiple.
            new EcosystemRole { Id = 28, AgentTypeId = 3, Name = "Entidades de educación superior, formación técnica y tecnológica", DisplayOrder = 12, IsActive = true });
        context.TerritorialLevelOptions.Add(new TerritorialLevelOption { Id = 2, Name = "Municipal", DisplayOrder = 2, IsActive = true });
        context.SaveChanges();
    }

    private static WorkbookImportService NewService(ApplicationDbContext context)
    {
        var blueprint = new FichaBlueprintProvider();
        var currentUser = new FakeCurrentUser();
        return new WorkbookImportService(
            context,
            new CatalogLookupService(context),
            blueprint,
            new WorkbookStructureValidator(blueprint),
            new ImportIssueNarrator(new BlueprintFieldLocator(blueprint)),
            currentUser,
            new AuditService(context, currentUser));
    }

    /// <summary>
    /// Diligencia el archivo oficial: una Identificación y varias filas en las demás hojas.
    /// <paramref name="unknownSource"/> reproduce un valor de selección múltiple que no está en el
    /// catálogo, que es lo que antes tumbaba el guardado del resto de la ficha.
    /// </summary>
    private static MemoryStream BuildWorkbook(bool unknownSource = false)
    {
        using var workbook = new XLWorkbook(OfficialFile);

        // La hoja "Detalle Indicadores" repite un encabezado dentro de su tabla de Excel, y
        // ClosedXML no puede volver a guardar el libro con ella. Como la importación lee celdas
        // por posición y ya no mira esas hojas, se quitan las tablas antes de guardar la copia de
        // prueba: el contenido y los encabezados quedan intactos.
        foreach (var sheet in workbook.Worksheets)
        {
            foreach (var table in sheet.Tables.ToArray())
            {
                sheet.Tables.Remove(table.Name);
            }
        }

        var identificacion = workbook.Worksheet("Identificación");
        identificacion.Cell(2, 1).Value = new DateTime(2026, 4, 10);
        identificacion.Cell(2, 2).Value = "Antioquia";
        identificacion.Cell(2, 3).Value = "Medellín";
        identificacion.Cell(2, 4).Value = "Gestor de prueba";
        identificacion.Cell(2, 5).Value = "Eje Cafetero";
        identificacion.Cell(2, 6).Value = unknownSource ? "Ente territorial, Fuente inventada" : "Ente territorial";
        identificacion.Cell(2, 7).Value = "Carga de prueba";

        var diagnostico = workbook.Worksheet("Diagnóstico ecosistema");
        diagnostico.Cell(2, 2).Value = "Caracterización del ecosistema";
        diagnostico.Cell(2, 7).Value = "Creado";

        var oportunidades = workbook.Worksheet("Oportunidades de cambio");
        foreach (var (row, indice) in new[] { (2, 1), (3, 2), (4, 3) })
        {
            oportunidades.Cell(row, 2).Value = $"Situación identificada {indice}";
            oportunidades.Cell(row, 4).Value = $"Aliados {indice}";
            oportunidades.Cell(row, 6).Value = indice == 1 ? "Alto" : "Medio";
        }

        var ejes = workbook.Worksheet("Ejes PNMC");
        ejes.Cell(2, 2).Value = "Hallazgo 1";
        ejes.Cell(2, 3).Value = "3. Gobernanza musical e integración cultural e intersectorial.";
        ejes.Cell(2, 4).Value = "Participación ciudadana, intersectorialidad y articulación territorial.";
        ejes.Cell(2, 14).Value = "Diferencial, Poblacional";
        ejes.Cell(3, 2).Value = "Hallazgo 2";
        ejes.Cell(3, 3).Value = "2. Fortalecimiento de las prácticas, expresiones y oficios de la música.";
        ejes.Cell(3, 4).Value = "Formación.";

        var actores = workbook.Worksheet("Actores");
        actores.Cell(2, 2).Value = "Alcaldía de Medellín";
        actores.Cell(2, 3).Value = "Institucional - Agente Externo";
        actores.Cell(2, 4).Value = "Alcaldías, Secretarías Locales de Cultura";
        actores.Cell(2, 5).Value = "Municipal";
        actores.Cell(3, 2).Value = "Universidad de Antioquia";
        actores.Cell(3, 3).Value = "Sectorial";
        actores.Cell(3, 4).Value = "Entidades de educación superior, formación técnica y tecnológica";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static async Task<ImportWorkbookResult> ImportAsync(ApplicationDbContext context, bool unknownSource = false)
    {
        using var stream = BuildWorkbook(unknownSource);
        return await NewService(context).ImportAsync(
            new ImportWorkbookCommand("ficha diligenciada.xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12", stream.Length, stream));
    }

    [Fact]
    public async Task ImportAsync_ShouldSaveEveryRowOfEverySheet()
    {
        using var context = NewContext();
        SeedCatalogs(context);

        var result = await ImportAsync(context);

        Assert.True(result.Accepted, $"El lote no se aceptó: {string.Join(" | ", result.Issues.Select(i => i.Message))}");

        var ficha = await context.FichasDepartamentales.SingleAsync();
        Assert.Equal(new DateOnly(2026, 4, 10), ficha.FechaLevantamiento);

        Assert.Equal(3, await context.OportunidadesCambio.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.Equal(2, await context.EjesPnmc.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.Equal(2, await context.Actores.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.NotNull(await context.DiagnosticosEcosistema.SingleOrDefaultAsync(x => x.FichaDepartamentalId == ficha.Id));
    }

    [Fact]
    public async Task ImportAsync_ShouldKeepTheValuesOfEachRow()
    {
        using var context = NewContext();
        SeedCatalogs(context);

        await ImportAsync(context);
        var ficha = await context.FichasDepartamentales.SingleAsync();

        var situaciones = await context.OportunidadesCambio
            .Where(x => x.FichaDepartamentalId == ficha.Id)
            .Select(x => x.SituacionIdentificada)
            .ToListAsync();
        Assert.Equal(
            ["Situación identificada 1", "Situación identificada 2", "Situación identificada 3"],
            situaciones.Order());

        // Cada eje conserva su componente, que se resuelve dentro del eje de su misma fila.
        var ejes = await context.EjesPnmc.Where(x => x.FichaDepartamentalId == ficha.Id).ToListAsync();
        Assert.Contains(ejes, x => x.PnmcAxisId == 3 && x.PnmcComponentId == 9);
        Assert.Contains(ejes, x => x.PnmcAxisId == 2 && x.PnmcComponentId == 3);

        // El rol que contiene ", " no debe partirse por el separador de selección múltiple.
        var universidad = await context.Actores
            .Include(x => x.RolesEcosistema)
            .SingleAsync(x => x.NombreAgente == "Universidad de Antioquia");
        Assert.Equal(28, Assert.Single(universidad.RolesEcosistema).EcosystemRoleId);

        var alcaldia = await context.Actores
            .Include(x => x.RolesEcosistema)
            .SingleAsync(x => x.NombreAgente == "Alcaldía de Medellín");
        Assert.Equal([8, 10], alcaldia.RolesEcosistema.Select(x => x.EcosystemRoleId).Order());
    }

    [Fact]
    public async Task ImportAsync_WithAnUnknownMultiSelectValue_ShouldStillSaveEverySection()
    {
        using var context = NewContext();
        SeedCatalogs(context);

        var result = await ImportAsync(context, unknownSource: true);
        var ficha = await context.FichasDepartamentales.SingleAsync();

        // El valor desconocido se reporta como observación...
        Assert.Contains(result.Issues, issue => issue.Severity == ImportIssueCodes.SeverityWarning);

        // ...pero no impide guardar el resto: las secciones siguientes llegan completas.
        Assert.Equal(3, await context.OportunidadesCambio.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.Equal(2, await context.EjesPnmc.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.Equal(2, await context.Actores.CountAsync(x => x.FichaDepartamentalId == ficha.Id));

        // La fuente que sí existe se guarda; la inventada simplemente se omite.
        var fuentes = await context.FichaFuentesInformacion.Where(x => x.FichaDepartamentalId == ficha.Id).ToListAsync();
        Assert.Equal(1, Assert.Single(fuentes).InformationSourceOptionId);
    }

    [Fact]
    public async Task ImportAsync_ShouldIgnoreTheIndicatorSheets()
    {
        using var context = NewContext();
        SeedCatalogs(context);

        var result = await ImportAsync(context);

        // No se leen ni se guardan filas de las hojas de indicadores...
        Assert.Empty(await context.ImportIndicatorStagingRows.ToListAsync());
        Assert.Empty(await context.ImportIndicatorDetailStagingRows.ToListAsync());
        Assert.Empty(await context.IndicatorRecords.ToListAsync());
        Assert.Empty(await context.IndicatorDetailRecords.ToListAsync());

        // ...ni se reportan incidencias sobre ellas.
        Assert.DoesNotContain(result.Issues, issue => issue.SheetName.Contains("Indicador", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ImportAsync_ReimportingTheSameFicha_ShouldNotDuplicateRows()
    {
        using var context = NewContext();
        SeedCatalogs(context);

        await ImportAsync(context);
        await ImportAsync(context);

        var ficha = await context.FichasDepartamentales.SingleAsync();
        Assert.Equal(3, await context.OportunidadesCambio.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
        Assert.Equal(2, await context.Actores.CountAsync(x => x.FichaDepartamentalId == ficha.Id));
    }
}
