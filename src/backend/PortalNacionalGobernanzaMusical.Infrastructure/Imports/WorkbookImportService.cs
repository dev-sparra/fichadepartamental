using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

public sealed class WorkbookImportService(
    ApplicationDbContext dbContext,
    ICatalogLookupService catalogLookupService,
    IFichaBlueprintProvider blueprintProvider,
    WorkbookStructureValidator structureValidator,
    IImportIssueNarrator issueNarrator,
    ICurrentUserService currentUserService) : IWorkbookImportService
{
    private static readonly string[] MultiSelectionSeparator = [", "];

    // Reglas de contacto del Actor tomadas del Blueprint (Actores!F número, Actores!G correo).
    private readonly BlueprintValidation? _phoneRule = ResolveActorRule(blueprintProvider, "numeroContacto");
    private readonly BlueprintValidation? _emailRule = ResolveActorRule(blueprintProvider, "correoElectronico");

    public async Task<ImportWorkbookResult> ImportAsync(ImportWorkbookCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Etapa 1-3 del flujo: el lote nace "en validación" y solo pasa a "procesando" cuando el
        // archivo demuestra ser la Ficha Departamental oficial (nombre, extensión y estructura).
        var batch = new ImportBatch
        {
            FileName = command.FileName,
            ContentType = command.ContentType,
            FileSizeBytes = command.FileSizeBytes,
            Status = ImportBatchStatuses.Validating
        };

        dbContext.ImportBatches.Add(batch);
        await dbContext.SaveChangesAsync(cancellationToken);

        var rejections = ValidateFile(command, out var workbook);

        using (workbook)
        {
            if (rejections.Count > 0)
            {
                return await RejectBatchAsync(batch, rejections, cancellationToken);
            }

            batch.Status = ImportBatchStatuses.Processing;
            return await ProcessWorkbookAsync(batch, workbook!, cancellationToken);
        }
    }

    /// <summary>
    /// Etapas 2 y 3 del flujo: formato del archivo (nombre, extensión, tamaño y legibilidad) y
    /// estructura del libro (hojas y encabezados del Blueprint). Devuelve el libro abierto solo si
    /// no hay motivos de rechazo.
    /// </summary>
    private IReadOnlyList<ImportFileRejection> ValidateFile(ImportWorkbookCommand command, out XLWorkbook? workbook)
    {
        workbook = null;

        var rejections = ImportFileRules.Validate(command.FileName, command.FileSizeBytes).ToList();
        if (rejections.Count > 0)
        {
            return rejections;
        }

        try
        {
            workbook = new XLWorkbook(command.FileStream);
        }
        catch (Exception ex)
        {
            // El detalle técnico se guarda como contexto de soporte, nunca como mensaje al usuario.
            rejections.Add(new ImportFileRejection(
                ImportIssueCodes.FileNotReadable,
                "El archivo no pudo abrirse: no es un libro de Excel válido o está dañado.",
                $"El archivo oficial {ImportFileRules.OfficialFileName} guardado desde Excel.",
                "Abra el archivo en Excel, guárdelo de nuevo como \"Libro de Excel habilitado para macros (*.xlsm)\" y vuelva a cargarlo.",
                command.FileName,
                TechnicalDetail: Describe(ex)));

            return rejections;
        }

        var structureRejections = structureValidator.Validate(workbook);
        if (structureRejections.Count > 0)
        {
            workbook.Dispose();
            workbook = null;
            rejections.AddRange(structureRejections);
        }

        return rejections;
    }

    /// <summary>Etapas 4 a 7: lectura a staging, validación de datos y materialización de la ficha.</summary>
    private async Task<ImportWorkbookResult> ProcessWorkbookAsync(ImportBatch batch, XLWorkbook workbook, CancellationToken cancellationToken)
    {
        var issues = new List<ImportValidationIssue>();
        var validRows = 0;
        var invalidRows = 0;
        var warningRows = 0;
        var persistedRecords = 0;

        try
        {
            var snapshot = await catalogLookupService.GetSnapshotAsync(cancellationToken);

            var identifications = ExtractIdentificationRows(workbook, batch.Id);
            var diagnostics = ExtractDiagnosticRows(workbook, batch.Id);
            var opportunities = ExtractOpportunityRows(workbook, batch.Id);
            var pnmcAxes = ExtractPnmcAxisRows(workbook, batch.Id);
            var actors = ExtractActorRows(workbook, batch.Id);
            var indicators = ExtractIndicatorRows(workbook, batch.Id);
            var indicatorDetails = ExtractIndicatorDetailRows(workbook, batch.Id);

            dbContext.ImportIdentificationStagingRows.AddRange(identifications);
            dbContext.ImportDiagnosticStagingRows.AddRange(diagnostics);
            dbContext.ImportOpportunityStagingRows.AddRange(opportunities);
            dbContext.ImportPnmcAxisStagingRows.AddRange(pnmcAxes);
            dbContext.ImportActorStagingRows.AddRange(actors);
            dbContext.ImportIndicatorStagingRows.AddRange(indicators);
            dbContext.ImportIndicatorDetailStagingRows.AddRange(indicatorDetails);

            ValidateIdentification(identifications, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidateDiagnostic(diagnostics, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidateOpportunities(opportunities, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidatePnmcAxes(pnmcAxes, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidateActors(actors, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidateIndicators(indicators, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);
            ValidateIndicatorDetails(indicatorDetails, snapshot, issues, ref validRows, ref invalidRows, ref warningRows, batch.Id);

            if (issues.All(x => x.Severity != ImportIssueCodes.SeverityError))
            {
                try
                {
                    persistedRecords = await PersistGovernanceDataAsync(
                        identifications,
                        diagnostics,
                        opportunities,
                        pnmcAxes,
                        actors,
                        indicators,
                        indicatorDetails,
                        issues,
                        batch.Id,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    // Si falla al guardar, el lote no se marca como rechazado: parte de la ficha
                    // puede haber quedado guardada y el usuario debe saber qué sección revisar.
                    issues.Add(CreateIssue(
                        batch.Id,
                        ImportIssueCodes.SeverityError,
                        "Identificación",
                        null,
                        null,
                        ImportIssueCodes.PersistSectionError,
                        "No fue posible guardar la información de la ficha departamental.",
                        null,
                        new ImportIssueContext { TechnicalDetail = Describe(ex) }));
                }
            }

            batch.Status = ImportStatusCatalog.ResolveFinalStatus(
                issues.Any(x => x.Severity == ImportIssueCodes.SeverityError),
                issues.Any(x => x.Severity == ImportIssueCodes.SeverityWarning));
            batch.ValidRowCount = validRows;
            batch.InvalidRowCount = invalidRows;
            batch.WarningCount = warningRows;
            batch.PersistedRecordCount = persistedRecords;
            batch.CompletedAtUtc = DateTime.UtcNow;
            batch.SummaryJson = JsonSerializer.Serialize(new
            {
                staging = new
                {
                    identification = identifications.Count,
                    diagnostic = diagnostics.Count,
                    opportunities = opportunities.Count,
                    pnmcAxes = pnmcAxes.Count,
                    actors = actors.Count,
                    indicators = indicators.Count,
                    indicatorDetails = indicatorDetails.Count
                },
                persistedRecords
            });
        }
        catch (Exception ex)
        {
            // Falla al leer o validar el libro: no se escribió ningún dato de gobernanza, así que
            // el lote se rechaza. El usuario recibe un mensaje funcional y el detalle de la
            // excepción queda en el contexto de la incidencia, solo para soporte.
            batch.Status = ImportBatchStatuses.Rejected;
            batch.CompletedAtUtc = DateTime.UtcNow;

            issues.Add(CreateIssue(
                batch.Id,
                ImportIssueCodes.SeverityError,
                "Archivo",
                null,
                null,
                ImportIssueCodes.ImportException,
                "No fue posible procesar el archivo. La información no se importó.",
                null,
                new ImportIssueContext { TechnicalDetail = Describe(ex) }));
        }

        if (issues.Count > 0)
        {
            dbContext.ImportValidationIssues.AddRange(issues);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildResult(batch, issues);
    }

    /// <summary>
    /// Cierra el lote como rechazado: no se escribe ningún dato de gobernanza y las incidencias se
    /// guardan ya redactadas para el usuario.
    /// </summary>
    private async Task<ImportWorkbookResult> RejectBatchAsync(
        ImportBatch batch,
        IReadOnlyList<ImportFileRejection> rejections,
        CancellationToken cancellationToken)
    {
        batch.Status = ImportBatchStatuses.Rejected;
        batch.CompletedAtUtc = DateTime.UtcNow;
        batch.ValidRowCount = 0;
        batch.InvalidRowCount = 0;
        batch.WarningCount = 0;
        batch.PersistedRecordCount = 0;

        var issues = rejections.Select(rejection => CreateIssue(
            batch.Id,
            ImportIssueCodes.SeverityError,
            rejection.SheetName,
            rejection.RowNumber,
            rejection.CellReference,
            rejection.Code,
            rejection.Message,
            rejection.RawValue,
            new ImportIssueContext
            {
                Expected = rejection.Expected,
                HowToFix = rejection.HowToFix,
                TechnicalDetail = rejection.TechnicalDetail
            })).ToList();

        dbContext.ImportValidationIssues.AddRange(issues);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildResult(batch, issues);
    }

    private ImportWorkbookResult BuildResult(ImportBatch batch, IReadOnlyCollection<ImportValidationIssue> issues)
    {
        var status = ImportStatusCatalog.Resolve(batch.Status);

        return new ImportWorkbookResult(
            batch.Id,
            batch.Status,
            status.Label,
            status.Description,
            status.NextStep,
            status.Tone,
            batch.Status != ImportBatchStatuses.Rejected && batch.Status != ImportBatchStatuses.Failed,
            batch.ValidRowCount,
            batch.InvalidRowCount,
            batch.WarningCount,
            batch.PersistedRecordCount,
            issues.Select(MapIssue).ToArray());
    }

    public async Task<IReadOnlyCollection<ImportBatchSummaryDto>> GetBatchesAsync(CancellationToken cancellationToken = default)
    {
        var batches = await dbContext.ImportBatches
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.FileName,
                x.Status,
                x.ValidRowCount,
                x.InvalidRowCount,
                x.WarningCount,
                x.PersistedRecordCount,
                x.StartedAtUtc,
                x.CompletedAtUtc
            })
            .ToArrayAsync(cancellationToken);

        return batches.Select(batch =>
        {
            var status = ImportStatusCatalog.Resolve(batch.Status);
            return new ImportBatchSummaryDto(
                batch.Id,
                batch.FileName,
                batch.Status,
                status.Label,
                status.Description,
                status.NextStep,
                status.Tone,
                batch.ValidRowCount,
                batch.InvalidRowCount,
                batch.WarningCount,
                batch.PersistedRecordCount,
                batch.StartedAtUtc,
                batch.CompletedAtUtc);
        }).ToArray();
    }

    public async Task<IReadOnlyCollection<ImportValidationIssueDto>> GetIssuesAsync(Guid importBatchId, CancellationToken cancellationToken = default)
    {
        var issues = await dbContext.ImportValidationIssues
            .AsNoTracking()
            .Where(x => x.ImportBatchId == importBatchId)
            // Primero lo que impide la importación, luego lo que solo hay que revisar.
            .OrderByDescending(x => x.Severity == ImportIssueCodes.SeverityError)
            .ThenBy(x => x.SheetName)
            .ThenBy(x => x.RowNumber)
            .Select(x => new ImportIssueSource(
                x.Id,
                x.Severity,
                x.SheetName,
                x.RowNumber,
                x.CellReference,
                x.ErrorCode,
                x.Message,
                x.RawValue,
                x.ContextJson))
            .ToArrayAsync(cancellationToken);

        return issues.Select(issueNarrator.Narrate).ToArray();
    }

    public async Task DeleteBatchAsync(Guid batchId, CancellationToken cancellationToken = default)
    {
        // Verifica que el lote exista antes de intentar borrarlo.
        var batch = await dbContext.ImportBatches.SingleOrDefaultAsync(x => x.Id == batchId, cancellationToken);
        if (batch is null)
        {
            throw new KeyNotFoundException($"No se encontró el lote de importación con ID {batchId}.");
        }

        // Elimina primero las incidencias y staging rows (FK → ImportBatch), luego el lote.
        dbContext.ImportValidationIssues.RemoveRange(
            dbContext.ImportValidationIssues.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportIdentificationStagingRows.RemoveRange(
            dbContext.ImportIdentificationStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportDiagnosticStagingRows.RemoveRange(
            dbContext.ImportDiagnosticStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportOpportunityStagingRows.RemoveRange(
            dbContext.ImportOpportunityStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportPnmcAxisStagingRows.RemoveRange(
            dbContext.ImportPnmcAxisStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportActorStagingRows.RemoveRange(
            dbContext.ImportActorStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportIndicatorStagingRows.RemoveRange(
            dbContext.ImportIndicatorStagingRows.Where(x => x.ImportBatchId == batchId));
        dbContext.ImportIndicatorDetailStagingRows.RemoveRange(
            dbContext.ImportIndicatorDetailStagingRows.Where(x => x.ImportBatchId == batchId));

        dbContext.ImportBatches.Remove(batch);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> PersistGovernanceDataAsync(
        IReadOnlyCollection<ImportIdentificationStagingRow> identifications,
        IReadOnlyCollection<ImportDiagnosticStagingRow> diagnostics,
        IReadOnlyCollection<ImportOpportunityStagingRow> opportunities,
        IReadOnlyCollection<ImportPnmcAxisStagingRow> pnmcAxes,
        IReadOnlyCollection<ImportActorStagingRow> actors,
        IReadOnlyCollection<ImportIndicatorStagingRow> indicators,
        IReadOnlyCollection<ImportIndicatorDetailStagingRow> indicatorDetails,
        ICollection<ImportValidationIssue> issues,
        Guid batchId,
        CancellationToken cancellationToken)
    {
        var persisted = 0;
        var identification = identifications.OrderBy(x => x.SourceRowNumber).FirstOrDefault();

        if (identification is null || identification.FechaLevantamiento is null || string.IsNullOrWhiteSpace(identification.DepartmentName))
        {
            issues.Add(CreateIssue(batchId, "Error", "Identificación", null, null, "PERSIST_IDENTIFICATION_REQUIRED", "No existe una fila válida de Identificación para materializar la ficha departamental.", null));
            return 0;
        }

        if (identifications.Count > 1)
        {
            issues.Add(CreateIssue(batchId, "Warning", "Identificación", null, null, "PERSIST_IDENTIFICATION_MULTIPLE", "Se encontraron múltiples filas en Identificación. Se usará la primera fila no vacía.", null));
        }

        var department = await dbContext.Departments.SingleAsync(x => x.Name == identification.DepartmentName, cancellationToken);
        var municipalityId = await ResolveOptionalLookupIdAsync(dbContext.Municipalities.Where(x => x.DepartmentId == department.Id), identification.MunicipalityName, cancellationToken);
        var regionId = await ResolveOptionalLookupIdAsync(dbContext.RegionOcadOptions, identification.RegionOcadName, cancellationToken);

        var ficha = await dbContext.FichasDepartamentales
            .Include(x => x.DiagnosticoEcosistema)
            .SingleOrDefaultAsync(
                x => x.DepartmentId == department.Id && x.FechaLevantamiento == identification.FechaLevantamiento.Value,
                cancellationToken);

        if (ficha is null)
        {
            ficha = new FichaDepartamental();
            dbContext.FichasDepartamentales.Add(ficha);
        }

        ficha.FechaLevantamiento = identification.FechaLevantamiento.Value;
        ficha.DepartmentId = department.Id;
        ficha.MunicipalityId = municipalityId;
        ficha.ResponsableRegistro = identification.ResponsableRegistro ?? string.Empty;
        ficha.RegionOcadOptionId = regionId;
        ficha.Observaciones = identification.Observaciones;
        ficha.CreatedByEmail = currentUserService.Email;

        await dbContext.SaveChangesAsync(cancellationToken);
        persisted++;

        dbContext.FichaFuentesInformacion.RemoveRange(dbContext.FichaFuentesInformacion.Where(x => x.FichaDepartamentalId == ficha.Id));
        foreach (var sourceName in SplitMultiValue(identification.InformationSourcesRaw))
        {
            var sourceId = await ResolveRequiredLookupIdAsync(dbContext.InformationSourceOptions, sourceName, cancellationToken);
            dbContext.FichaFuentesInformacion.Add(new FichaFuenteInformacion
            {
                FichaDepartamentalId = ficha.Id,
                InformationSourceOptionId = sourceId
            });
            persisted++;
        }

        // Cada sección se persiste en su propio try-catch: si una falla, se reporta como
        // incidencia pero las anteriores ya quedaron commiteadas y el lote no se marca como
        // "Failed" perdiendo todo el progreso. El usuario verá exactamente qué sección falló.
        var sectionPersisted = await TryPersistSectionAsync("Diagnóstico ecosistema", batchId, issues, () => ReplaceDiagnosticAsync(ficha, diagnostics, issues, batchId, cancellationToken));
        if (sectionPersisted && diagnostics.Count > 0) persisted++;

        sectionPersisted = await TryPersistSectionAsync("Oportunidades de cambio", batchId, issues, () => ReplaceOpportunitiesAsync(ficha, opportunities, cancellationToken));
        if (sectionPersisted) persisted += opportunities.Count;

        sectionPersisted = await TryPersistSectionAsync("Ejes PNMC", batchId, issues, () => ReplacePnmcAxesAsync(ficha, pnmcAxes, cancellationToken));
        if (sectionPersisted) persisted += pnmcAxes.Count;

        sectionPersisted = await TryPersistSectionAsync("Actores", batchId, issues, () => ReplaceActorsAsync(ficha, actors, cancellationToken));
        if (sectionPersisted) persisted += actors.Count;

        sectionPersisted = await TryPersistSectionAsync("Indicadores", batchId, issues, () => ReplaceIndicatorsAsync(indicators, issues, batchId, cancellationToken));
        if (sectionPersisted) persisted += indicators.Count;

        sectionPersisted = await TryPersistSectionAsync("Detalle Indicadores", batchId, issues, () => ReplaceIndicatorDetailsAsync(indicatorDetails, issues, batchId, cancellationToken));
        if (sectionPersisted) persisted += indicatorDetails.Count;

        return persisted;
    }

    private async Task ReplaceDiagnosticAsync(FichaDepartamental ficha, IReadOnlyCollection<ImportDiagnosticStagingRow> diagnostics, ICollection<ImportValidationIssue> issues, Guid batchId, CancellationToken cancellationToken)
    {
        var diagnostic = diagnostics.OrderBy(x => x.SourceRowNumber).FirstOrDefault();
        if (diagnostic is null)
        {
            return;
        }

        if (diagnostics.Count > 1)
        {
            issues.Add(CreateIssue(batchId, "Warning", "Diagnóstico ecosistema", null, null, "PERSIST_DIAGNOSTIC_MULTIPLE", "Se encontraron múltiples filas en Diagnóstico ecosistema. Se usará la primera fila no vacía.", null));
        }

        var entity = ficha.DiagnosticoEcosistema ?? new DiagnosticoEcosistema { FichaDepartamentalId = ficha.Id };
        entity.CaracterizacionGeneral = diagnostic.CaracterizacionGeneral;
        entity.FortalezasIdentificadas = diagnostic.FortalezasIdentificadas;
        entity.PoliticasPriorizadas = diagnostic.PoliticasPriorizadas;
        entity.DebilidadesIdentificadas = diagnostic.DebilidadesIdentificadas;
        entity.TensionesOConflictos = diagnostic.TensionesOConflictos;
        entity.CommitteeStatusOptionId = await ResolveOptionalLookupIdAsync(dbContext.CommitteeStatusOptions, diagnostic.CommitteeStatusName, cancellationToken);
        entity.PlanDepartamentalCulturaStatusId = await ResolveOptionalLookupIdAsync(dbContext.PlanStatusOptions, diagnostic.PlanDepartamentalCulturaStatusName, cancellationToken);
        entity.ConsejoDepartamentalCultura = diagnostic.ConsejoDepartamentalCultura;
        entity.PlanDepartamentalMusicaStatusId = await ResolveOptionalLookupIdAsync(dbContext.PlanStatusOptions, diagnostic.PlanDepartamentalMusicaStatusName, cancellationToken);
        entity.OrdenanzasCulturales = diagnostic.OrdenanzasCulturales;
        entity.ConsejoDepartamentalMusica = diagnostic.ConsejoDepartamentalMusica;
        entity.MesaSectorialTerritorial = diagnostic.MesaSectorialTerritorial;
        entity.Observaciones = diagnostic.Observaciones;

        if (ficha.DiagnosticoEcosistema is null)
        {
            dbContext.DiagnosticosEcosistema.Add(entity);
            ficha.DiagnosticoEcosistema = entity;
        }
    }

    private async Task ReplaceOpportunitiesAsync(FichaDepartamental ficha, IReadOnlyCollection<ImportOpportunityStagingRow> opportunities, CancellationToken cancellationToken)
    {
        dbContext.OportunidadesCambio.RemoveRange(dbContext.OportunidadesCambio.Where(x => x.FichaDepartamentalId == ficha.Id));

        foreach (var row in opportunities)
        {
            dbContext.OportunidadesCambio.Add(new OportunidadCambio
            {
                FichaDepartamentalId = ficha.Id,
                SituacionIdentificada = row.SituacionIdentificada,
                ComponenteOtrasDependenciasEntidades = row.ComponenteOtrasDependenciasEntidades,
                AliadosYCreyentes = row.AliadosYCreyentes,
                TerritorioInfluencia = row.TerritorioInfluencia,
                PriorityLevelOptionId = await ResolveOptionalLookupIdAsync(dbContext.PriorityLevelOptions, row.PriorityLevelName, cancellationToken),
                DescripcionAdicional = row.DescripcionAdicional
            });
        }
    }

    private async Task ReplacePnmcAxesAsync(FichaDepartamental ficha, IReadOnlyCollection<ImportPnmcAxisStagingRow> pnmcAxes, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.EjesPnmc.Where(x => x.FichaDepartamentalId == ficha.Id).Select(x => x.Id).ToListAsync(cancellationToken);
        dbContext.EjesPnmcEnfoques.RemoveRange(dbContext.EjesPnmcEnfoques.Where(x => existingIds.Contains(x.EjePnmcRegistroId)));
        dbContext.EjesPnmc.RemoveRange(dbContext.EjesPnmc.Where(x => x.FichaDepartamentalId == ficha.Id));

        foreach (var row in pnmcAxes)
        {
            var axisId = await ResolveOptionalLookupIdAsync(dbContext.PnmcAxes, row.PnmcAxisName, cancellationToken);
            var componentId = await ResolveOptionalPnmcComponentIdAsync(axisId, row.PnmcComponentName, cancellationToken);

            var entity = new EjePnmcRegistro
            {
                FichaDepartamentalId = ficha.Id,
                DescripcionHallazgo = row.DescripcionHallazgo,
                PnmcAxisId = axisId,
                PnmcComponentId = componentId,
                AccionEstrategica = row.AccionEstrategica,
                PoliticaPriorizada = row.PoliticaPriorizada,
                ArmonizacionPnc = row.ArmonizacionPnc,
                ArmonizacionPnd = row.ArmonizacionPnd,
                ArmonizacionInternacional = row.ArmonizacionInternacional,
                PriorityLevelOptionId = await ResolveOptionalLookupIdAsync(dbContext.PriorityLevelOptions, row.PriorityLevelName, cancellationToken),
                AliadosResponsables = row.AliadosResponsables,
                FuentesFinanciacion = row.FuentesFinanciacion,
                ValorPropuestaCop = row.ValorPropuestaCop,
                Descripcion = row.Descripcion,
                ScheduleOptionId = await ResolveOptionalLookupIdAsync(dbContext.ScheduleOptions, row.ScheduleName, cancellationToken),
                ProposalStatusOptionId = await ResolveOptionalLookupIdAsync(dbContext.ProposalStatusOptions, row.ProposalStatusName, cancellationToken),
                Observaciones = row.Observaciones
            };

            dbContext.EjesPnmc.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var approachName in SplitMultiValue(row.EnfoquesRaw))
            {
                var approachId = await ResolveRequiredLookupIdAsync(dbContext.ApproachOptions, approachName, cancellationToken);
                dbContext.EjesPnmcEnfoques.Add(new EjePnmcRegistroEnfoque
                {
                    EjePnmcRegistroId = entity.Id,
                    ApproachOptionId = approachId
                });
            }
        }
    }

    private async Task ReplaceActorsAsync(FichaDepartamental ficha, IReadOnlyCollection<ImportActorStagingRow> actors, CancellationToken cancellationToken)
    {
        var existingIds = await dbContext.Actores.Where(x => x.FichaDepartamentalId == ficha.Id).Select(x => x.Id).ToListAsync(cancellationToken);
        dbContext.ActorRolesEcosistema.RemoveRange(dbContext.ActorRolesEcosistema.Where(x => existingIds.Contains(x.ActorId)));
        dbContext.ActorNivelesTerritoriales.RemoveRange(dbContext.ActorNivelesTerritoriales.Where(x => existingIds.Contains(x.ActorId)));
        dbContext.Actores.RemoveRange(dbContext.Actores.Where(x => x.FichaDepartamentalId == ficha.Id));

        foreach (var row in actors)
        {
            var agentTypeId = await ResolveOptionalLookupIdAsync(dbContext.AgentTypes, row.AgentTypeName, cancellationToken);
            var actor = new Actor
            {
                FichaDepartamentalId = ficha.Id,
                NombreAgente = row.NombreAgente ?? string.Empty,
                AgentTypeId = agentTypeId,
                NumeroContacto = row.NumeroContacto,
                CorreoElectronico = row.CorreoElectronico,
                Observaciones = row.Observaciones
            };

            dbContext.Actores.Add(actor);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var roleName in SplitMultiValue(row.EcosystemRolesRaw))
            {
                var roleId = await ResolveRequiredEcosystemRoleIdAsync(agentTypeId, roleName, cancellationToken);
                dbContext.ActorRolesEcosistema.Add(new ActorRolEcosistema
                {
                    ActorId = actor.Id,
                    EcosystemRoleId = roleId
                });
            }

            foreach (var levelName in SplitMultiValue(row.TerritorialLevelsRaw))
            {
                var levelId = await ResolveRequiredLookupIdAsync(dbContext.TerritorialLevelOptions, levelName, cancellationToken);
                dbContext.ActorNivelesTerritoriales.Add(new ActorNivelTerritorial
                {
                    ActorId = actor.Id,
                    TerritorialLevelOptionId = levelId
                });
            }
        }
    }

    private async Task ReplaceIndicatorsAsync(IReadOnlyCollection<ImportIndicatorStagingRow> indicators, ICollection<ImportValidationIssue> issues, Guid batchId, CancellationToken cancellationToken)
    {
        foreach (var row in indicators)
        {
            var departmentNames = SplitMultiValue(row.DepartmentsRaw);

            var indicatorDefinition = await dbContext.IndicatorDefinitions.SingleOrDefaultAsync(x => x.IndicatorName == row.IndicatorName, cancellationToken);
            if (indicatorDefinition is null)
            {
                issues.Add(CreateIssue(batchId, "Error", "Indicadores", row.SourceRowNumber, "C" + row.SourceRowNumber, "INDICATOR_NOT_FOUND", "Indicador no existe en el catálogo maestro de indicadores.", row.IndicatorName));
                continue;
            }

            var targetValue = indicatorDefinition.TargetValue;
            var year = int.TryParse(row.YearRaw, out var parsedYear) ? parsedYear : 0;
            var quantitativeValues = JsonSerializer.Deserialize<string[]>(row.MonthlyQuantitativeJson ?? "[]") ?? [];
            var detailValues = JsonSerializer.Deserialize<string[]>(row.MonthlyDetailJson ?? "[]") ?? [];
            var monthOptions = await dbContext.MonthOptions.OrderBy(x => x.DisplayOrder).ToListAsync(cancellationToken);

            foreach (var departmentName in departmentNames)
            {
                var departmentId = await ResolveRequiredLookupIdAsync(dbContext.Departments, departmentName, cancellationToken);
                var existing = await dbContext.IndicatorRecords.SingleOrDefaultAsync(
                    x => x.DepartmentId == departmentId && x.IndicatorDefinitionId == indicatorDefinition.Id && x.Year == year,
                    cancellationToken);

                if (existing is not null)
                {
                    dbContext.IndicatorMonthlyProgresses.RemoveRange(dbContext.IndicatorMonthlyProgresses.Where(x => x.IndicatorRecordId == existing.Id));
                    dbContext.IndicatorRecords.Remove(existing);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var parsedQuantitative = quantitativeValues.Select(ParseDecimalString).ToArray();
                var currentValue = targetValue <= 1
                    ? parsedQuantitative.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0m).Max()
                    : parsedQuantitative.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0m).Sum();
                var compliance = targetValue == 0 ? 0 : currentValue / targetValue;

                var record = new IndicatorRecord
                {
                    DepartmentId = departmentId,
                    IndicatorDefinitionId = indicatorDefinition.Id,
                    Year = year,
                    Source = row.Fuente,
                    GeneralObservations = row.ObservacionesGenerales,
                    CurrentValueCalculated = currentValue,
                    CompliancePercentageCalculated = compliance
                };

                dbContext.IndicatorRecords.Add(record);
                await dbContext.SaveChangesAsync(cancellationToken);

                for (var index = 0; index < monthOptions.Count; index++)
                {
                    dbContext.IndicatorMonthlyProgresses.Add(new IndicatorMonthlyProgress
                    {
                        IndicatorRecordId = record.Id,
                        MonthOptionId = monthOptions[index].Id,
                        QuantitativeAdvance = index < parsedQuantitative.Length ? parsedQuantitative[index] : null,
                        Detail = index < detailValues.Length ? detailValues[index] : null
                    });
                }
            }
        }
    }

    private async Task ReplaceIndicatorDetailsAsync(IReadOnlyCollection<ImportIndicatorDetailStagingRow> indicatorDetails, ICollection<ImportValidationIssue> issues, Guid batchId, CancellationToken cancellationToken)
    {
        foreach (var row in indicatorDetails)
        {
            var departmentNames = SplitMultiValue(row.DepartmentsRaw);
            var monthNames = SplitMultiValue(row.MonthsRaw);

            // Si el indicador no existe en el catálogo, SingleAsync lanza InvalidOperationException,
            // que se captura en el catch general del ImportAsync como IMPORT_EXCEPTION.
            // Pero si el detalle (fórmula/descripción) no coincide con ninguna plantilla del
            // catálogo, antes se descartaba en silencio. Ahora se reporta como error para que
            // el usuario sepa por qué su fila no se importó.
            var indicatorDefinition = await dbContext.IndicatorDefinitions.SingleOrDefaultAsync(x => x.IndicatorName == row.IndicatorName, cancellationToken);
            if (indicatorDefinition is null)
            {
                issues.Add(CreateIssue(batchId, "Error", "Detalle Indicadores", row.SourceRowNumber, "C" + row.SourceRowNumber, "DETAIL_INDICATOR_NOT_FOUND", "Indicador no existe en el catálogo maestro de indicadores.", row.IndicatorName));
                continue;
            }

            var detailTemplate = await dbContext.IndicatorDetailTemplates.SingleOrDefaultAsync(
                x => x.IndicatorDefinitionId == indicatorDefinition.Id && x.FormulaLabel == row.FormulaLabel && x.DetailDescription == row.DetailDescription,
                cancellationToken);

            if (detailTemplate is null)
            {
                issues.Add(CreateIssue(batchId, "Error", "Detalle Indicadores", row.SourceRowNumber, "E" + row.SourceRowNumber, "DETAIL_TEMPLATE_NOT_FOUND", $"No existe plantilla de detalle con fórmula '{row.FormulaLabel}' y descripción '{row.DetailDescription}' para el indicador '{row.IndicatorName}'.", $"{row.FormulaLabel} | {row.DetailDescription}"));
                continue;
            }

            var year = int.TryParse(row.YearRaw, out var parsedYear) ? parsedYear : 0;
            var currentValue = ParseDecimalString(row.CurrentValueRaw);

            foreach (var departmentName in departmentNames)
            {
                var departmentId = await ResolveRequiredLookupIdAsync(dbContext.Departments, departmentName, cancellationToken);
                var existing = await dbContext.IndicatorDetailRecords.SingleOrDefaultAsync(
                    x => x.DepartmentId == departmentId && x.IndicatorDefinitionId == indicatorDefinition.Id && x.IndicatorDetailTemplateId == detailTemplate.Id && x.Year == year,
                    cancellationToken);

                if (existing is not null)
                {
                    dbContext.IndicatorDetailRecordMonths.RemoveRange(dbContext.IndicatorDetailRecordMonths.Where(x => x.IndicatorDetailRecordId == existing.Id));
                    dbContext.IndicatorDetailRecords.Remove(existing);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                var record = new IndicatorDetailRecord
                {
                    DepartmentId = departmentId,
                    IndicatorDefinitionId = indicatorDefinition.Id,
                    IndicatorDetailTemplateId = detailTemplate.Id,
                    Year = year,
                    Source = row.Fuente,
                    Observations = row.Observaciones,
                    CurrentValueCalculated = currentValue
                };

                dbContext.IndicatorDetailRecords.Add(record);
                await dbContext.SaveChangesAsync(cancellationToken);

                foreach (var monthName in monthNames)
                {
                    var monthId = await ResolveRequiredLookupIdAsync(dbContext.MonthOptions, monthName, cancellationToken);
                    dbContext.IndicatorDetailRecordMonths.Add(new IndicatorDetailRecordMonth
                    {
                        IndicatorDetailRecordId = record.Id,
                        MonthOptionId = monthId
                    });
                }
            }
        }
    }

    private static List<ImportIdentificationStagingRow> ExtractIdentificationRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportIdentificationStagingRow>();
        var ws = workbook.Worksheet("Identificación");

        for (var row = 2; row <= 51; row++)
        {
            if (!HasAnyValue(ws, row, 1, 7))
            {
                continue;
            }

            rows.Add(new ImportIdentificationStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                FechaLevantamiento = ParseDateOnly(ws.Cell(row, 1)),
                DepartmentName = GetString(ws, row, 2),
                MunicipalityName = GetString(ws, row, 3),
                ResponsableRegistro = GetString(ws, row, 4),
                RegionOcadName = GetString(ws, row, 5),
                InformationSourcesRaw = GetString(ws, row, 6),
                Observaciones = GetString(ws, row, 7)
            });
        }

        return rows;
    }

    private static List<ImportDiagnosticStagingRow> ExtractDiagnosticRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportDiagnosticStagingRow>();
        var ws = workbook.Worksheet("Diagnóstico ecosistema");

        for (var row = 2; row <= 51; row++)
        {
            if (!HasAnyValue(ws, row, 2, 14))
            {
                continue;
            }

            rows.Add(new ImportDiagnosticStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                CaracterizacionGeneral = GetString(ws, row, 2),
                FortalezasIdentificadas = GetString(ws, row, 3),
                PoliticasPriorizadas = GetString(ws, row, 4),
                DebilidadesIdentificadas = GetString(ws, row, 5),
                TensionesOConflictos = GetString(ws, row, 6),
                CommitteeStatusName = GetString(ws, row, 7),
                PlanDepartamentalCulturaStatusName = GetString(ws, row, 8),
                ConsejoDepartamentalCultura = GetString(ws, row, 9),
                PlanDepartamentalMusicaStatusName = GetString(ws, row, 10),
                OrdenanzasCulturales = GetString(ws, row, 11),
                ConsejoDepartamentalMusica = GetString(ws, row, 12),
                MesaSectorialTerritorial = GetString(ws, row, 13),
                Observaciones = GetString(ws, row, 14)
            });
        }

        return rows;
    }

    private static List<ImportOpportunityStagingRow> ExtractOpportunityRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportOpportunityStagingRow>();
        var ws = workbook.Worksheet("Oportunidades de cambio");

        for (var row = 2; row <= 51; row++)
        {
            if (!HasAnyValue(ws, row, 2, 7))
            {
                continue;
            }

            rows.Add(new ImportOpportunityStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                SituacionIdentificada = GetString(ws, row, 2),
                ComponenteOtrasDependenciasEntidades = GetString(ws, row, 3),
                AliadosYCreyentes = GetString(ws, row, 4),
                TerritorioInfluencia = GetString(ws, row, 5),
                PriorityLevelName = GetString(ws, row, 6),
                DescripcionAdicional = GetString(ws, row, 7)
            });
        }

        return rows;
    }

    private static List<ImportPnmcAxisStagingRow> ExtractPnmcAxisRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportPnmcAxisStagingRow>();
        var ws = workbook.Worksheet("Ejes PNMC");

        for (var row = 2; row <= 51; row++)
        {
            if (!HasAnyValue(ws, row, 2, 18))
            {
                continue;
            }

            rows.Add(new ImportPnmcAxisStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                DescripcionHallazgo = GetString(ws, row, 2),
                PnmcAxisName = GetString(ws, row, 3),
                PnmcComponentName = GetString(ws, row, 4),
                AccionEstrategica = GetString(ws, row, 5),
                PoliticaPriorizada = GetString(ws, row, 6),
                ArmonizacionPnc = GetString(ws, row, 7),
                ArmonizacionPnd = GetString(ws, row, 8),
                ArmonizacionInternacional = GetString(ws, row, 9),
                PriorityLevelName = GetString(ws, row, 10),
                AliadosResponsables = GetString(ws, row, 11),
                FuentesFinanciacion = GetString(ws, row, 12),
                ValorPropuestaCop = ParseDecimal(ws.Cell(row, 13)),
                EnfoquesRaw = GetString(ws, row, 14),
                Descripcion = GetString(ws, row, 15),
                ScheduleName = GetString(ws, row, 16),
                ProposalStatusName = GetString(ws, row, 17),
                Observaciones = GetString(ws, row, 18)
            });
        }

        return rows;
    }

    private static List<ImportActorStagingRow> ExtractActorRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportActorStagingRow>();
        var ws = workbook.Worksheet("Actores");

        for (var row = 2; row <= 51; row++)
        {
            if (!HasAnyValue(ws, row, 2, 8))
            {
                continue;
            }

            rows.Add(new ImportActorStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                NombreAgente = GetString(ws, row, 2),
                AgentTypeName = GetString(ws, row, 3),
                EcosystemRolesRaw = GetString(ws, row, 4),
                TerritorialLevelsRaw = GetString(ws, row, 5),
                NumeroContacto = GetString(ws, row, 6),
                CorreoElectronico = GetString(ws, row, 7),
                Observaciones = GetString(ws, row, 8)
            });
        }

        return rows;
    }

    private static List<ImportIndicatorStagingRow> ExtractIndicatorRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportIndicatorStagingRow>();
        var ws = workbook.Worksheet("Indicadores");

        for (var row = 3; row <= 9; row++)
        {
            if (!HasAnyValue(ws, row, 1, 33))
            {
                continue;
            }

            rows.Add(new ImportIndicatorStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                DepartmentsRaw = GetString(ws, row, 1),
                ActionName = GetString(ws, row, 2),
                IndicatorName = GetString(ws, row, 3),
                TargetRaw = GetString(ws, row, 4),
                MonthlyQuantitativeJson = JsonSerializer.Serialize(Enumerable.Range(0, 12).Select(index => GetString(ws, row, 5 + (index * 2))).ToArray()),
                MonthlyDetailJson = JsonSerializer.Serialize(Enumerable.Range(0, 12).Select(index => GetString(ws, row, 6 + (index * 2))).ToArray()),
                Fuente = GetString(ws, row, 31),
                YearRaw = GetString(ws, row, 32),
                ObservacionesGenerales = GetString(ws, row, 33)
            });
        }

        return rows;
    }

    private static List<ImportIndicatorDetailStagingRow> ExtractIndicatorDetailRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportIndicatorDetailStagingRow>();
        var ws = workbook.Worksheet("Detalle Indicadores");

        for (var row = 2; row <= 14; row++)
        {
            if (!HasAnyValue(ws, row, 1, 11))
            {
                continue;
            }

            rows.Add(new ImportIndicatorDetailStagingRow
            {
                ImportBatchId = batchId,
                SourceRowNumber = row,
                DepartmentsRaw = GetString(ws, row, 1),
                ActionName = GetString(ws, row, 2),
                IndicatorName = GetString(ws, row, 3),
                TargetRaw = GetString(ws, row, 4),
                FormulaLabel = GetString(ws, row, 5),
                DetailDescription = GetString(ws, row, 6),
                MonthsRaw = GetString(ws, row, 7),
                CurrentValueRaw = GetString(ws, row, 8),
                Fuente = GetString(ws, row, 9),
                YearRaw = GetString(ws, row, 10),
                Observaciones = GetString(ws, row, 11)
            });
        }

        return rows;
    }

    private static void ValidateIdentification(IReadOnlyCollection<ImportIdentificationStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;

            if (row.FechaLevantamiento is null)
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Identificación", row.SourceRowNumber, "A" + row.SourceRowNumber, "IDENT_DATE_REQUIRED", "Fecha de levantamiento inválida o vacía.", null));
            }

            if (!string.IsNullOrWhiteSpace(row.DepartmentName) && !snapshot.Departments.Contains(row.DepartmentName))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Identificación", row.SourceRowNumber, "B" + row.SourceRowNumber, "IDENT_DEPARTMENT_INVALID", "Departamento no existe en catálogo maestro.", row.DepartmentName));
            }

            if (!string.IsNullOrWhiteSpace(row.DepartmentName) && !string.IsNullOrWhiteSpace(row.MunicipalityName))
            {
                if (!snapshot.MunicipalitiesByDepartment.TryGetValue(row.DepartmentName, out var municipalities) || !municipalities.Contains(row.MunicipalityName))
                {
                    rowHasError = true;
                    issues.Add(CreateIssue(batchId, "Error", "Identificación", row.SourceRowNumber, "C" + row.SourceRowNumber, "IDENT_CITY_INVALID", "Ciudad no corresponde al departamento seleccionado.", row.MunicipalityName));
                }
            }

            if (!string.IsNullOrWhiteSpace(row.RegionOcadName) && !snapshot.RegionOcad.Contains(row.RegionOcadName))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Identificación", row.SourceRowNumber, "E" + row.SourceRowNumber, "IDENT_REGION_INVALID", "Región OCAD no existe en catálogo maestro.", row.RegionOcadName));
            }

            warningRows += ValidateMultiSelection(batchId, "Identificación", row.SourceRowNumber, "F", row.InformationSourcesRaw, snapshot.InformationSources, issues, "IDENT_SOURCE_INVALID");
            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static void ValidateDiagnostic(IReadOnlyCollection<ImportDiagnosticStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;
            rowHasError |= ValidateSingleLookup(batchId, "Diagnóstico ecosistema", row.SourceRowNumber, "G", row.CommitteeStatusName, snapshot.CommitteeStatuses, issues, "DIAG_COMMITTEE_INVALID");
            rowHasError |= ValidateSingleLookup(batchId, "Diagnóstico ecosistema", row.SourceRowNumber, "H", row.PlanDepartamentalCulturaStatusName, snapshot.PlanStatuses, issues, "DIAG_PLAN_CULTURE_INVALID");
            rowHasError |= ValidateInline(batchId, "Diagnóstico ecosistema", row.SourceRowNumber, "I", row.ConsejoDepartamentalCultura, ["Existe", "No existe", "Por renovar"], issues, "DIAG_COUNCIL_INVALID");
            rowHasError |= ValidateSingleLookup(batchId, "Diagnóstico ecosistema", row.SourceRowNumber, "J", row.PlanDepartamentalMusicaStatusName, snapshot.PlanStatuses, issues, "DIAG_PLAN_MUSIC_INVALID");
            rowHasError |= ValidateInline(batchId, "Diagnóstico ecosistema", row.SourceRowNumber, "K", row.OrdenanzasCulturales, ["Existe", "No existe", "Por activar"], issues, "DIAG_ORDINANCE_INVALID");
            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static void ValidateOpportunities(IReadOnlyCollection<ImportOpportunityStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = ValidateSingleLookup(batchId, "Oportunidades de cambio", row.SourceRowNumber, "F", row.PriorityLevelName, snapshot.PriorityLevels, issues, "OPP_PRIORITY_INVALID");
            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static void ValidatePnmcAxes(IReadOnlyCollection<ImportPnmcAxisStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;
            rowHasError |= ValidateSingleLookup(batchId, "Ejes PNMC", row.SourceRowNumber, "C", row.PnmcAxisName, snapshot.PnmcAxes, issues, "EJES_AXIS_INVALID");

            if (!string.IsNullOrWhiteSpace(row.PnmcAxisName) && !string.IsNullOrWhiteSpace(row.PnmcComponentName))
            {
                if (!snapshot.ComponentsByAxis.TryGetValue(row.PnmcAxisName, out var components) || !components.Contains(row.PnmcComponentName))
                {
                    rowHasError = true;
                    issues.Add(CreateIssue(batchId, "Error", "Ejes PNMC", row.SourceRowNumber, "D" + row.SourceRowNumber, "EJES_COMPONENT_INVALID", "Componente PNMC no corresponde al eje seleccionado.", row.PnmcComponentName));
                }
            }

            rowHasError |= ValidateSingleLookup(batchId, "Ejes PNMC", row.SourceRowNumber, "J", row.PriorityLevelName, snapshot.PriorityLevels, issues, "EJES_PRIORITY_INVALID");
            warningRows += ValidateMultiSelection(batchId, "Ejes PNMC", row.SourceRowNumber, "N", row.EnfoquesRaw, snapshot.Approaches, issues, "EJES_APPROACH_INVALID");
            rowHasError |= ValidateSingleLookup(batchId, "Ejes PNMC", row.SourceRowNumber, "P", row.ScheduleName, snapshot.ScheduleOptions, issues, "EJES_SCHEDULE_INVALID");
            rowHasError |= ValidateSingleLookup(batchId, "Ejes PNMC", row.SourceRowNumber, "Q", row.ProposalStatusName, snapshot.ProposalStatuses, issues, "EJES_STATUS_INVALID");
            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private void ValidateActors(IReadOnlyCollection<ImportActorStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;
            rowHasError |= ValidateSingleLookup(batchId, "Actores", row.SourceRowNumber, "C", row.AgentTypeName, snapshot.AgentTypes, issues, "ACTOR_AGENT_TYPE_INVALID");

            if (!string.IsNullOrWhiteSpace(row.AgentTypeName) && !string.IsNullOrWhiteSpace(row.EcosystemRolesRaw))
            {
                var selectedRoles = SplitMultiValue(row.EcosystemRolesRaw);
                if (!snapshot.EcosystemRolesByAgentType.TryGetValue(row.AgentTypeName, out var roles))
                {
                    rowHasError = true;
                    issues.Add(CreateIssue(batchId, "Error", "Actores", row.SourceRowNumber, "D" + row.SourceRowNumber, "ACTOR_ROLE_MAPPING_MISSING", "No existe mapeo de roles para el tipo de agente seleccionado.", row.EcosystemRolesRaw));
                }
                else
                {
                    foreach (var selectedRole in selectedRoles)
                    {
                        if (!roles.Contains(selectedRole))
                        {
                            rowHasError = true;
                            issues.Add(CreateIssue(batchId, "Error", "Actores", row.SourceRowNumber, "D" + row.SourceRowNumber, "ACTOR_ROLE_INVALID", "Rol en el ecosistema no corresponde al tipo de agente seleccionado.", selectedRole));
                        }
                    }
                }
            }

            warningRows += ValidateMultiSelection(batchId, "Actores", row.SourceRowNumber, "E", row.TerritorialLevelsRaw, snapshot.TerritorialLevels, issues, "ACTOR_TERRITORIAL_LEVEL_INVALID");

            var phoneMinLength = _phoneRule?.MinLength ?? 7;
            var phoneMaxLength = _phoneRule?.MaxLength ?? 20;
            if (!ActorContactValidation.IsPhoneLengthValid(row.NumeroContacto, phoneMinLength, phoneMaxLength))
            {
                issues.Add(CreateIssue(batchId, ImportIssueCodes.SeverityWarning, "Actores", row.SourceRowNumber, "F" + row.SourceRowNumber, ImportIssueCodes.ActorPhoneLength, $"El número de contacto debe tener entre {phoneMinLength} y {phoneMaxLength} caracteres.", row.NumeroContacto));
                warningRows++;
            }
            else if (!PortalFieldRules.IsMobilePhone(row.NumeroContacto))
            {
                // El portal captura celulares de 10 dígitos; el Excel solo valida longitud, así que
                // esto se reporta como observación y no bloquea la importación de la fila.
                issues.Add(CreateIssue(batchId, ImportIssueCodes.SeverityWarning, "Actores", row.SourceRowNumber, "F" + row.SourceRowNumber, ImportIssueCodes.ActorPhoneFormat, "El número de contacto no corresponde a un celular de 10 dígitos.", row.NumeroContacto));
                warningRows++;
            }

            var emailMinLength = _emailRule?.MinLength ?? 5;
            if (!ActorContactValidation.IsEmailValid(row.CorreoElectronico, emailMinLength) || !PortalFieldRules.IsEmail(row.CorreoElectronico))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, ImportIssueCodes.SeverityError, "Actores", row.SourceRowNumber, "G" + row.SourceRowNumber, ImportIssueCodes.ActorEmailInvalid, "El correo electrónico no tiene un formato válido.", row.CorreoElectronico));
            }

            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static void ValidateIndicators(IReadOnlyCollection<ImportIndicatorStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;
            warningRows += ValidateMultiSelection(batchId, "Indicadores", row.SourceRowNumber, "A", row.DepartmentsRaw, snapshot.Departments, issues, "INDICATORS_DEPARTMENT_INVALID");

            if (!string.IsNullOrWhiteSpace(row.IndicatorName) && !snapshot.IndicatorNames.Contains(row.IndicatorName))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Indicadores", row.SourceRowNumber, "C" + row.SourceRowNumber, "INDICATOR_NAME_INVALID", "Indicador no existe en el catálogo maestro de indicadores.", row.IndicatorName));
            }

            if (int.TryParse(row.YearRaw, out var yearValue))
            {
                if (!snapshot.Years.Contains(yearValue))
                {
                    rowHasError = true;
                    issues.Add(CreateIssue(batchId, "Error", "Indicadores", row.SourceRowNumber, "AF" + row.SourceRowNumber, "INDICATOR_YEAR_INVALID", "Año no existe en catálogo maestro.", row.YearRaw));
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.YearRaw))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Indicadores", row.SourceRowNumber, "AF" + row.SourceRowNumber, "INDICATOR_YEAR_FORMAT", "Año debe ser numérico entero.", row.YearRaw));
            }

            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static void ValidateIndicatorDetails(IReadOnlyCollection<ImportIndicatorDetailStagingRow> rows, CatalogValidationSnapshot snapshot, ICollection<ImportValidationIssue> issues, ref int validRows, ref int invalidRows, ref int warningRows, Guid batchId)
    {
        foreach (var row in rows)
        {
            var rowHasError = false;
            warningRows += ValidateMultiSelection(batchId, "Detalle Indicadores", row.SourceRowNumber, "A", row.DepartmentsRaw, snapshot.Departments, issues, "DETAIL_DEPARTMENT_INVALID");
            warningRows += ValidateMultiSelection(batchId, "Detalle Indicadores", row.SourceRowNumber, "G", row.MonthsRaw, snapshot.Months, issues, "DETAIL_MONTH_INVALID");

            if (int.TryParse(row.YearRaw, out var yearValue))
            {
                if (!snapshot.Years.Contains(yearValue))
                {
                    rowHasError = true;
                    issues.Add(CreateIssue(batchId, "Error", "Detalle Indicadores", row.SourceRowNumber, "J" + row.SourceRowNumber, "DETAIL_YEAR_INVALID", "Año no existe en catálogo maestro.", row.YearRaw));
                }
            }
            else if (!string.IsNullOrWhiteSpace(row.YearRaw))
            {
                rowHasError = true;
                issues.Add(CreateIssue(batchId, "Error", "Detalle Indicadores", row.SourceRowNumber, "J" + row.SourceRowNumber, "DETAIL_YEAR_FORMAT", "Año debe existir en la lista de valores permitidos.", row.YearRaw));
            }

            AccumulateRowResult(rowHasError, ref validRows, ref invalidRows);
        }
    }

    private static bool ValidateSingleLookup(Guid batchId, string sheetName, int row, string column, string? value, HashSet<string> validValues, ICollection<ImportValidationIssue> issues, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (validValues.Contains(value))
        {
            return false;
        }

        issues.Add(CreateIssue(batchId, "Error", sheetName, row, column + row, errorCode, "Valor no existe en catálogo maestro.", value));
        return true;
    }

    private static bool ValidateInline(Guid batchId, string sheetName, int row, string column, string? value, IReadOnlyCollection<string> validValues, ICollection<ImportValidationIssue> issues, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value) || validValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        issues.Add(CreateIssue(batchId, "Error", sheetName, row, column + row, errorCode, "Valor no coincide con la lista definida en Excel.", value));
        return true;
    }

    private static int ValidateMultiSelection(Guid batchId, string sheetName, int row, string column, string? rawValue, HashSet<string> validValues, ICollection<ImportValidationIssue> issues, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return 0;
        }

        var warnings = 0;
        foreach (var token in SplitMultiValue(rawValue))
        {
            if (!validValues.Contains(token))
            {
                issues.Add(CreateIssue(batchId, "Warning", sheetName, row, column + row, errorCode, "Valor multiselección no existe en catálogo maestro.", token));
                warnings++;
            }
        }

        return warnings;
    }

    private static void AccumulateRowResult(bool rowHasError, ref int validRows, ref int invalidRows)
    {
        if (rowHasError)
        {
            invalidRows++;
            return;
        }

        validRows++;
    }

    private static string GetString(IXLWorksheet worksheet, int row, int column)
    {
        return worksheet.Cell(row, column).GetFormattedString().Trim();
    }

    private static bool HasAnyValue(IXLWorksheet worksheet, int row, int startColumn, int endColumn)
    {
        for (var column = startColumn; column <= endColumn; column++)
        {
            if (!string.IsNullOrWhiteSpace(GetString(worksheet, row, column)))
            {
                return true;
            }
        }

        return false;
    }

    private static DateOnly? ParseDateOnly(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<DateTime>(out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return DateOnly.TryParse(cell.GetFormattedString(), out var parsed) ? parsed : null;
    }

    private static decimal? ParseDecimal(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.TryGetValue<decimal>(out var decimalValue))
        {
            return decimalValue;
        }

        return decimal.TryParse(cell.GetFormattedString().Replace("%", string.Empty).Trim(), out var parsed) ? parsed : null;
    }

    private async Task<int?> ResolveOptionalLookupIdAsync<TEntity>(IQueryable<TEntity> queryable, string? name, CancellationToken cancellationToken)
        where TEntity : class
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return await queryable.Where(x => EF.Property<string>(x, "Name") == name).Select(x => (int?)EF.Property<int>(x, "Id")).SingleAsync(cancellationToken);
    }

    private async Task<int?> ResolveOptionalLookupIdAsync<TEntity>(DbSet<TEntity> set, string? name, CancellationToken cancellationToken)
        where TEntity : class
    {
        return await ResolveOptionalLookupIdAsync(set.AsQueryable(), name, cancellationToken);
    }

    private async Task<int> ResolveRequiredLookupIdAsync<TEntity>(DbSet<TEntity> set, string name, CancellationToken cancellationToken)
        where TEntity : class
    {
        return await set.Where(x => EF.Property<string>(x, "Name") == name).Select(x => EF.Property<int>(x, "Id")).SingleAsync(cancellationToken);
    }

    private async Task<int?> ResolveOptionalPnmcComponentIdAsync(int? axisId, string? componentName, CancellationToken cancellationToken)
    {
        if (axisId is null || string.IsNullOrWhiteSpace(componentName))
        {
            return null;
        }

        return await dbContext.PnmcComponents.Where(x => x.PnmcAxisId == axisId && x.Name == componentName).Select(x => (int?)x.Id).SingleAsync(cancellationToken);
    }

    private async Task<int> ResolveRequiredEcosystemRoleIdAsync(int? agentTypeId, string roleName, CancellationToken cancellationToken)
    {
        return await dbContext.EcosystemRoles.Where(x => x.AgentTypeId == agentTypeId && x.Name == roleName).Select(x => x.Id).SingleAsync(cancellationToken);
    }

    private static decimal? ParseDecimalString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Replace("%", string.Empty).Trim();
        return decimal.TryParse(normalized, out var parsed) ? parsed : null;
    }

    private static BlueprintValidation? ResolveActorRule(IFichaBlueprintProvider blueprintProvider, string fieldKey)
    {
        return blueprintProvider.GetBlueprint().Sheets
            .First(sheet => sheet.Key == "actores").Fields
            .First(field => field.Key == fieldKey).Validation;
    }

    private static string[] SplitMultiValue(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue)
            ? []
            : rawValue.Split(MultiSelectionSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static ImportValidationIssue CreateIssue(
        Guid batchId,
        string severity,
        string sheetName,
        int? rowNumber,
        string? cellReference,
        string errorCode,
        string message,
        string? rawValue,
        ImportIssueContext? context = null)
    {
        return new ImportValidationIssue
        {
            ImportBatchId = batchId,
            Severity = severity,
            SheetName = sheetName,
            RowNumber = rowNumber,
            CellReference = cellReference,
            ErrorCode = errorCode,
            Message = message,
            RawValue = rawValue,
            ContextJson = context?.ToJson()
        };
    }

    private ImportValidationIssueDto MapIssue(ImportValidationIssue issue)
    {
        return issueNarrator.Narrate(new ImportIssueSource(
            issue.Id,
            issue.Severity,
            issue.SheetName,
            issue.RowNumber,
            issue.CellReference,
            issue.ErrorCode,
            issue.Message,
            issue.RawValue,
            issue.ContextJson));
    }

    /// <summary>Descripción técnica de una excepción para el contexto de soporte.</summary>
    private static string Describe(Exception exception)
    {
        return exception.InnerException is not null
            ? $"{exception.GetType().Name}: {exception.Message} → {exception.InnerException.Message}"
            : $"{exception.GetType().Name}: {exception.Message}";
    }

    /// <summary>
    /// Ejecuta una sección de persistencia dentro de un try-catch aislado.
    /// Si la sección falla, se reporta una incidencia de Error con el nombre de la hoja
    /// y el detalle de la excepción (incluyendo InnerException), pero las secciones
    /// anteriores ya commiteadas permanecen en la base de datos. Devuelve false si falló.
    /// </summary>
    private async Task<bool> TryPersistSectionAsync(string sectionName, Guid batchId, ICollection<ImportValidationIssue> issues, Func<Task> section)
    {
        try
        {
            await section();
            return true;
        }
        catch (Exception ex)
        {
            issues.Add(CreateIssue(
                batchId,
                ImportIssueCodes.SeverityError,
                sectionName,
                null,
                null,
                ImportIssueCodes.PersistSectionError,
                $"No fue posible guardar la información de la sección \"{sectionName}\".",
                null,
                new ImportIssueContext { TechnicalDetail = Describe(ex) }));

            return false;
        }
    }
}
