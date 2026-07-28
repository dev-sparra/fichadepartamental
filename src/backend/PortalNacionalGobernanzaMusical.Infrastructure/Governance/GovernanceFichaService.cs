using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance;
using PortalNacionalGobernanzaMusical.Domain.Common;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;
using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Governance;

public sealed class GovernanceFichaService(
    ApplicationDbContext dbContext,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IGovernanceFichaService
{
    private const string EntityName = "FichaDepartamental";

    /// <summary>Estado de una ficha que todavía no ha pasado por revisión del líder.</summary>
    private const string DraftApprovalStatus = "Borrador";

    private const string RoleGestorDepartamental = SecurityRoleNames.GestorDepartamental;
    private const string RoleLiderGobernanza = SecurityRoleNames.LiderGobernanza;
    private const string RoleAdministrador = SecurityRoleNames.Administrador;

    public async Task<IReadOnlyCollection<GovernanceFichaSummaryDto>> GetFichasAsync(CancellationToken cancellationToken = default)
    {
        IQueryable<FichaDepartamental> query = dbContext.FichasDepartamentales.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Municipality)
            .Include(x => x.RegionOcadOption)
            .Include(x => x.OportunidadesCambio)
            .Include(x => x.EjesPnmc)
            .Include(x => x.Actores);

        // Filtrar por usuario si es Gestor Departamental (solo ve sus propias fichas)
        // Líder de Gobernanza y Administrador ven todas las fichas
        if (currentUserService.HasAnyRole(RoleGestorDepartamental) &&
            !currentUserService.HasAnyRole(RoleLiderGobernanza, RoleAdministrador))
        {
            var userEmail = currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(x => x.CreatedByEmail == userEmail);
            }
        }

        return await query
            .OrderByDescending(x => x.FechaLevantamiento)
            .Select(x => new GovernanceFichaSummaryDto(
                x.Id,
                x.Department!.Name,
                x.FechaLevantamiento,
                x.ResponsableRegistro,
                x.RegionOcadOption != null ? x.RegionOcadOption.Name : null,
                x.Municipality != null ? x.Municipality.Name : null,
                x.OportunidadesCambio.Count,
                x.EjesPnmc.Count,
                x.Actores.Count,
                // Una ficha sin revisión (por ejemplo, recién importada) es un borrador.
                dbContext.Set<ApprovalRecord>()
                    .Where(record => record.FichaDepartamentalId == x.Id)
                    .OrderByDescending(record => record.CreatedAtUtc)
                    .Select(record => record.Status)
                    .FirstOrDefault() ?? DraftApprovalStatus))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<GovernanceFichaDetailDto?> GetFichaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = dbContext.FichasDepartamentales.AsNoTracking()
            .Include(x => x.FuentesInformacion)
            .Where(x => x.Id == id);

        // Filtrar por usuario si es Gestor Departamental (solo ve sus propias fichas)
        // Líder de Gobernanza y Administrador ven todas las fichas
        if (currentUserService.HasAnyRole(RoleGestorDepartamental) &&
            !currentUserService.HasAnyRole(RoleLiderGobernanza, RoleAdministrador))
        {
            var userEmail = currentUserService.Email;
            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                query = query.Where(x => x.CreatedByEmail == userEmail);
            }
        }

        return await query
            .Select(x => new GovernanceFichaDetailDto(
                x.Id,
                x.FechaLevantamiento,
                x.DepartmentId,
                x.MunicipalityId,
                x.ResponsableRegistro,
                x.RegionOcadOptionId,
                x.Observaciones,
                x.FuentesInformacion.Select(f => f.InformationSourceOptionId).ToArray()))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<GovernanceFichaDetailDto> CreateFichaAsync(UpdateGovernanceFichaRequest request, CancellationToken cancellationToken = default)
    {
        GovernanceRequestValidation.EnsureValid(request);

        var ficha = new FichaDepartamental
        {
            FechaLevantamiento = request.FechaLevantamiento,
            DepartmentId = request.DepartmentId,
            MunicipalityId = request.MunicipalityId,
            ResponsableRegistro = request.ResponsableRegistro,
            RegionOcadOptionId = request.RegionOcadOptionId,
            Observaciones = request.Observaciones,
            CreatedByEmail = currentUserService.Email
        };

        dbContext.FichasDepartamentales.Add(ficha);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var sourceId in request.InformationSourceIds.Distinct())
        {
            dbContext.FichaFuentesInformacion.Add(new FichaFuenteInformacion
            {
                FichaDepartamentalId = ficha.Id,
                InformationSourceOptionId = sourceId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var created = new GovernanceFichaDetailDto(
            ficha.Id, ficha.FechaLevantamiento, ficha.DepartmentId, ficha.MunicipalityId,
            ficha.ResponsableRegistro, ficha.RegionOcadOptionId, ficha.Observaciones,
            request.InformationSourceIds.Distinct().ToArray());

        var label = await DescribeFichaAsync(ficha.Id, cancellationToken);
        var changes = await BuildIdentificationChangesAsync(null, created, cancellationToken);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Gobernanza,
            EntityName = EntityName,
            EntityId = ficha.Id,
            EntityLabel = label,
            Operation = "Crear ficha",
            Description = $"Creó la {label}. Queda en borrador, a la espera de revisión del Líder de Gobernanza.",
            Changes = changes.Changes,
            NewValuesJson = JsonSerializer.Serialize(created)
        }, cancellationToken);

        return created;
    }

    public async Task<GovernanceFichaDetailDto> UpdateFichaAsync(Guid id, UpdateGovernanceFichaRequest request, CancellationToken cancellationToken = default)
    {
        GovernanceRequestValidation.EnsureValid(request);

        var ficha = await dbContext.FichasDepartamentales
            .Include(x => x.FuentesInformacion)
            .SingleAsync(x => x.Id == id, cancellationToken);

        if (!CanAccessFicha(ficha))
        {
            throw new UnauthorizedAccessException("No tiene permisos para modificar esta ficha.");
        }

        var before = new GovernanceFichaDetailDto(
            ficha.Id, ficha.FechaLevantamiento, ficha.DepartmentId, ficha.MunicipalityId,
            ficha.ResponsableRegistro, ficha.RegionOcadOptionId, ficha.Observaciones,
            ficha.FuentesInformacion.Select(f => f.InformationSourceOptionId).ToArray());

        ficha.FechaLevantamiento = request.FechaLevantamiento;
        ficha.DepartmentId = request.DepartmentId;
        ficha.MunicipalityId = request.MunicipalityId;
        ficha.ResponsableRegistro = request.ResponsableRegistro;
        ficha.RegionOcadOptionId = request.RegionOcadOptionId;
        ficha.Observaciones = request.Observaciones;

        dbContext.FichaFuentesInformacion.RemoveRange(ficha.FuentesInformacion);
        foreach (var sourceId in request.InformationSourceIds.Distinct())
        {
            dbContext.FichaFuentesInformacion.Add(new FichaFuenteInformacion
            {
                FichaDepartamentalId = ficha.Id,
                InformationSourceOptionId = sourceId
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var after = new GovernanceFichaDetailDto(
            ficha.Id,
            ficha.FechaLevantamiento,
            ficha.DepartmentId,
            ficha.MunicipalityId,
            ficha.ResponsableRegistro,
            ficha.RegionOcadOptionId,
            ficha.Observaciones,
            request.InformationSourceIds.Distinct().ToArray());

        var label = await DescribeFichaAsync(ficha.Id, cancellationToken);
        var changes = await BuildIdentificationChangesAsync(before, after, cancellationToken);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Gobernanza,
            EntityName = EntityName,
            EntityId = ficha.Id,
            EntityLabel = label,
            Operation = "Actualizar identificación",
            Description = changes.HasChanges
                ? $"Modificó {changes.DescribeChangedFields()} en la identificación de la {label}."
                : $"Guardó la identificación de la {label} sin cambios.",
            Changes = changes.Changes,
            OldValuesJson = JsonSerializer.Serialize(before),
            NewValuesJson = JsonSerializer.Serialize(after)
        }, cancellationToken);

        return after;
    }

    public async Task DeleteFichaAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!currentUserService.HasAnyRole(RoleAdministrador))
        {
            throw new UnauthorizedAccessException("Solo los administradores pueden eliminar fichas.");
        }

        var ficha = await dbContext.FichasDepartamentales
            .Include(x => x.FuentesInformacion)
            .Include(x => x.DiagnosticoEcosistema)
            .Include(x => x.OportunidadesCambio)
            .Include(x => x.EjesPnmc)
            .ThenInclude(e => e.Enfoques)
            .Include(x => x.Actores)
            .ThenInclude(a => a.RolesEcosistema)
            .Include(x => x.Actores)
            .ThenInclude(a => a.NivelesTerritoriales)
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (ficha is null)
        {
            throw new KeyNotFoundException($"No se encontró la ficha con ID {id}.");
        }

        var fichaSummary = new { ficha.Id, ficha.DepartmentId, ficha.FechaLevantamiento };
        var label = await DescribeFichaAsync(id, cancellationToken);
        var borrado = new AuditChangeSet()
            .Track("oportunidades", "Oportunidades de cambio", ficha.OportunidadesCambio.Count, 0)
            .Track("ejes", "Ejes PNMC", ficha.EjesPnmc.Count, 0)
            .Track("actores", "Actores", ficha.Actores.Count, 0)
            .Track("diagnostico", "Diagnóstico del ecosistema", ficha.DiagnosticoEcosistema is not null, false);

        dbContext.EjesPnmcEnfoques.RemoveRange(ficha.EjesPnmc.SelectMany(e => e.Enfoques));
        dbContext.EjesPnmc.RemoveRange(ficha.EjesPnmc);
        dbContext.ActorRolesEcosistema.RemoveRange(ficha.Actores.SelectMany(a => a.RolesEcosistema));
        dbContext.ActorNivelesTerritoriales.RemoveRange(ficha.Actores.SelectMany(a => a.NivelesTerritoriales));
        dbContext.Actores.RemoveRange(ficha.Actores);
        dbContext.OportunidadesCambio.RemoveRange(ficha.OportunidadesCambio);
        dbContext.FichaFuentesInformacion.RemoveRange(ficha.FuentesInformacion);
        if (ficha.DiagnosticoEcosistema is not null)
        {
            dbContext.DiagnosticosEcosistema.Remove(ficha.DiagnosticoEcosistema);
        }

        // Los registros de aprobación (workflow) tienen FK a la ficha. Si no se eliminan
        // aquí, SaveChanges falla con DbUpdateException por violación de FK, impidiendo
        // borrar la ficha cuando tiene historial de aprobación/rechazo.
        dbContext.Set<ApprovalRecord>().RemoveRange(
            dbContext.Set<ApprovalRecord>().Where(x => x.FichaDepartamentalId == id));

        dbContext.FichasDepartamentales.Remove(ficha);

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Gobernanza,
            EntityName = EntityName,
            EntityId = id,
            EntityLabel = label,
            Operation = "Eliminar ficha",
            Description = $"Eliminó la {label} y todo su contenido, incluido su historial de aprobación.",
            Changes = borrado.Changes,
            OldValuesJson = JsonSerializer.Serialize(fichaSummary)
        }, cancellationToken);
    }

    public async Task<GovernanceDiagnosticDto?> GetDiagnosticAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.DiagnosticosEcosistema.AsNoTracking()
            .Where(x => x.FichaDepartamentalId == fichaId)
            .SingleOrDefaultAsync(cancellationToken);
        return entity is null ? null : MapDiagnostic(entity);
    }

    public async Task<GovernanceDiagnosticDto> UpdateDiagnosticAsync(Guid fichaId, GovernanceDiagnosticDto request, CancellationToken cancellationToken = default)
    {
        await GetFichaWithAccessCheckAsync(fichaId, cancellationToken);
        var existing = await dbContext.DiagnosticosEcosistema.SingleOrDefaultAsync(x => x.FichaDepartamentalId == fichaId, cancellationToken);
        var before = existing is null ? null : MapDiagnostic(existing);
        var beforeJson = before is null ? null : JsonSerializer.Serialize(before);
        var entity = existing ?? new DiagnosticoEcosistema { FichaDepartamentalId = fichaId };

        entity.CaracterizacionGeneral = request.CaracterizacionGeneral;
        entity.FortalezasIdentificadas = request.FortalezasIdentificadas;
        entity.PoliticasPriorizadas = request.PoliticasPriorizadas;
        entity.DebilidadesIdentificadas = request.DebilidadesIdentificadas;
        entity.TensionesOConflictos = request.TensionesOConflictos;
        entity.CommitteeStatusOptionId = request.CommitteeStatusOptionId;
        entity.PlanDepartamentalCulturaStatusId = request.PlanDepartamentalCulturaStatusId;
        entity.ConsejoDepartamentalCultura = request.ConsejoDepartamentalCultura;
        entity.PlanDepartamentalMusicaStatusId = request.PlanDepartamentalMusicaStatusId;
        entity.OrdenanzasCulturales = request.OrdenanzasCulturales;
        entity.ConsejoDepartamentalMusica = request.ConsejoDepartamentalMusica;
        entity.MesaSectorialTerritorial = request.MesaSectorialTerritorial;
        entity.Observaciones = request.Observaciones;

        if (entity.Id == Guid.Empty)
        {
            dbContext.DiagnosticosEcosistema.Add(entity);
        }
        else if (dbContext.Entry(entity).State == EntityState.Detached)
        {
            dbContext.DiagnosticosEcosistema.Add(entity);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var diagnostic = MapDiagnostic(entity);
        var label = await DescribeFichaAsync(fichaId, cancellationToken);

        var committeeStatuses = await CatalogNamesAsync(dbContext.CommitteeStatusOptions, [before?.CommitteeStatusOptionId, diagnostic.CommitteeStatusOptionId], cancellationToken);
        var planStatuses = await CatalogNamesAsync(
            dbContext.PlanStatusOptions,
            [before?.PlanDepartamentalCulturaStatusId, diagnostic.PlanDepartamentalCulturaStatusId,
             before?.PlanDepartamentalMusicaStatusId, diagnostic.PlanDepartamentalMusicaStatusId],
            cancellationToken);

        static string? Name(Dictionary<int, string> names, int? id) =>
            id.HasValue ? names.GetValueOrDefault(id.Value, id.Value.ToString()) : null;

        var changes = new AuditChangeSet()
            .Track("caracterizacionGeneral", "Caracterización general", before?.CaracterizacionGeneral, diagnostic.CaracterizacionGeneral)
            .Track("fortalezasIdentificadas", "Fortalezas identificadas", before?.FortalezasIdentificadas, diagnostic.FortalezasIdentificadas)
            .Track("politicasPriorizadas", "Políticas priorizadas", before?.PoliticasPriorizadas, diagnostic.PoliticasPriorizadas)
            .Track("debilidadesIdentificadas", "Debilidades identificadas", before?.DebilidadesIdentificadas, diagnostic.DebilidadesIdentificadas)
            .Track("tensionesOConflictos", "Tensiones o conflictos", before?.TensionesOConflictos, diagnostic.TensionesOConflictos)
            .Track("committeeStatusOptionId", "Estado del comité", Name(committeeStatuses, before?.CommitteeStatusOptionId), Name(committeeStatuses, diagnostic.CommitteeStatusOptionId))
            .Track("planDepartamentalCulturaStatusId", "Plan departamental de cultura", Name(planStatuses, before?.PlanDepartamentalCulturaStatusId), Name(planStatuses, diagnostic.PlanDepartamentalCulturaStatusId))
            .Track("consejoDepartamentalCultura", "Consejo departamental de cultura", before?.ConsejoDepartamentalCultura, diagnostic.ConsejoDepartamentalCultura)
            .Track("planDepartamentalMusicaStatusId", "Plan departamental de música", Name(planStatuses, before?.PlanDepartamentalMusicaStatusId), Name(planStatuses, diagnostic.PlanDepartamentalMusicaStatusId))
            .Track("ordenanzasCulturales", "Ordenanzas culturales", before?.OrdenanzasCulturales, diagnostic.OrdenanzasCulturales)
            .Track("consejoDepartamentalMusica", "Consejo departamental de música", before?.ConsejoDepartamentalMusica, diagnostic.ConsejoDepartamentalMusica)
            .Track("mesaSectorialTerritorial", "Mesa sectorial territorial", before?.MesaSectorialTerritorial, diagnostic.MesaSectorialTerritorial)
            .Track("observaciones", "Observaciones", before?.Observaciones, diagnostic.Observaciones);

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Gobernanza,
            EntityName = EntityName,
            EntityId = fichaId,
            EntityLabel = label,
            Operation = "Actualizar diagnóstico del ecosistema",
            Description = changes.HasChanges
                ? $"Modificó {changes.DescribeChangedFields()} en el diagnóstico del ecosistema de la {label}."
                : $"Guardó el diagnóstico del ecosistema de la {label} sin cambios.",
            Changes = changes.Changes,
            OldValuesJson = beforeJson,
            NewValuesJson = JsonSerializer.Serialize(diagnostic)
        }, cancellationToken);

        return diagnostic;
    }

    public async Task<IReadOnlyCollection<GovernanceOpportunityDto>> GetOpportunitiesAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.OportunidadesCambio.AsNoTracking()
            .Where(x => x.FichaDepartamentalId == fichaId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return entities.Select(MapOpportunity).ToArray();
    }

    public async Task<IReadOnlyCollection<GovernanceOpportunityDto>> ReplaceOpportunitiesAsync(Guid fichaId, IReadOnlyCollection<GovernanceOpportunityDto> request, CancellationToken cancellationToken = default)
    {
        await GetFichaWithAccessCheckAsync(fichaId, cancellationToken);
        var before = await GetOpportunitiesAsync(fichaId, cancellationToken);
        dbContext.OportunidadesCambio.RemoveRange(dbContext.OportunidadesCambio.Where(x => x.FichaDepartamentalId == fichaId));

        foreach (var item in request)
        {
            dbContext.OportunidadesCambio.Add(new OportunidadCambio
            {
                FichaDepartamentalId = fichaId,
                SituacionIdentificada = item.SituacionIdentificada,
                ComponenteOtrasDependenciasEntidades = item.ComponenteOtrasDependenciasEntidades,
                AliadosYCreyentes = item.AliadosYCreyentes,
                TerritorioInfluencia = item.TerritorioInfluencia,
                PriorityLevelOptionId = item.PriorityLevelOptionId,
                DescripcionAdicional = item.DescripcionAdicional
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var opportunities = await GetOpportunitiesAsync(fichaId, cancellationToken);
        await LogSectionAsync(fichaId, "Actualizar oportunidades de cambio", "las oportunidades de cambio", before, opportunities, cancellationToken);
        return opportunities;
    }

    public async Task<IReadOnlyCollection<GovernancePnmcAxisDto>> GetPnmcAxesAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.EjesPnmc.AsNoTracking()
            .Include(x => x.Enfoques)
            .Where(x => x.FichaDepartamentalId == fichaId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return entities.Select(MapPnmcAxis).ToArray();
    }

    public async Task<IReadOnlyCollection<GovernancePnmcAxisDto>> ReplacePnmcAxesAsync(Guid fichaId, IReadOnlyCollection<GovernancePnmcAxisDto> request, CancellationToken cancellationToken = default)
    {
        GovernanceRequestValidation.EnsureValid(request);
        await EnsureComponentsBelongToTheirAxisAsync(request, cancellationToken);
        await GetFichaWithAccessCheckAsync(fichaId, cancellationToken);
        var before = await GetPnmcAxesAsync(fichaId, cancellationToken);
        var existingIds = await dbContext.EjesPnmc.Where(x => x.FichaDepartamentalId == fichaId).Select(x => x.Id).ToListAsync(cancellationToken);
        dbContext.EjesPnmcEnfoques.RemoveRange(dbContext.EjesPnmcEnfoques.Where(x => existingIds.Contains(x.EjePnmcRegistroId)));
        dbContext.EjesPnmc.RemoveRange(dbContext.EjesPnmc.Where(x => x.FichaDepartamentalId == fichaId));

        foreach (var item in request)
        {
            var entity = new EjePnmcRegistro
            {
                FichaDepartamentalId = fichaId,
                DescripcionHallazgo = item.DescripcionHallazgo,
                PnmcAxisId = item.PnmcAxisId,
                PnmcComponentId = item.PnmcComponentId,
                AccionEstrategica = item.AccionEstrategica,
                PoliticaPriorizada = item.PoliticaPriorizada,
                ArmonizacionPnc = item.ArmonizacionPnc,
                ArmonizacionPnd = item.ArmonizacionPnd,
                ArmonizacionInternacional = item.ArmonizacionInternacional,
                PriorityLevelOptionId = item.PriorityLevelOptionId,
                AliadosResponsables = item.AliadosResponsables,
                FuentesFinanciacion = item.FuentesFinanciacion,
                ValorPropuestaCop = item.ValorPropuestaCop,
                Descripcion = item.Descripcion,
                ScheduleOptionId = item.ScheduleOptionId,
                ProposalStatusOptionId = item.ProposalStatusOptionId,
                Observaciones = item.Observaciones
            };

            dbContext.EjesPnmc.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var approachId in item.ApproachOptionIds.Distinct())
            {
                dbContext.EjesPnmcEnfoques.Add(new EjePnmcRegistroEnfoque
                {
                    EjePnmcRegistroId = entity.Id,
                    ApproachOptionId = approachId
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var axes = await GetPnmcAxesAsync(fichaId, cancellationToken);
        await LogSectionAsync(fichaId, "Actualizar ejes PNMC", "los ejes PNMC", before, axes, cancellationToken);
        return axes;
    }

    public async Task<IReadOnlyCollection<GovernanceActorDto>> GetActorsAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Actores.AsNoTracking()
            .Include(x => x.RolesEcosistema)
            .Include(x => x.NivelesTerritoriales)
            .Where(x => x.FichaDepartamentalId == fichaId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        return entities.Select(MapActor).ToArray();
    }

    public async Task<IReadOnlyCollection<GovernanceActorDto>> ReplaceActorsAsync(Guid fichaId, IReadOnlyCollection<GovernanceActorDto> request, CancellationToken cancellationToken = default)
    {
        GovernanceRequestValidation.EnsureValid(request);
        await EnsureRolesBelongToTheirAgentTypeAsync(request, cancellationToken);
        await GetFichaWithAccessCheckAsync(fichaId, cancellationToken);
        var before = await GetActorsAsync(fichaId, cancellationToken);
        var existingIds = await dbContext.Actores.Where(x => x.FichaDepartamentalId == fichaId).Select(x => x.Id).ToListAsync(cancellationToken);
        dbContext.ActorRolesEcosistema.RemoveRange(dbContext.ActorRolesEcosistema.Where(x => existingIds.Contains(x.ActorId)));
        dbContext.ActorNivelesTerritoriales.RemoveRange(dbContext.ActorNivelesTerritoriales.Where(x => existingIds.Contains(x.ActorId)));
        dbContext.Actores.RemoveRange(dbContext.Actores.Where(x => x.FichaDepartamentalId == fichaId));

        foreach (var item in request)
        {
            var entity = new Actor
            {
                FichaDepartamentalId = fichaId,
                NombreAgente = item.NombreAgente,
                AgentTypeId = item.AgentTypeId,
                NumeroContacto = item.NumeroContacto,
                CorreoElectronico = item.CorreoElectronico,
                Observaciones = item.Observaciones
            };

            dbContext.Actores.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var roleId in item.EcosystemRoleIds.Distinct())
            {
                dbContext.ActorRolesEcosistema.Add(new ActorRolEcosistema
                {
                    ActorId = entity.Id,
                    EcosystemRoleId = roleId
                });
            }

            foreach (var territorialLevelId in item.TerritorialLevelOptionIds.Distinct())
            {
                dbContext.ActorNivelesTerritoriales.Add(new ActorNivelTerritorial
                {
                    ActorId = entity.Id,
                    TerritorialLevelOptionId = territorialLevelId
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var actors = await GetActorsAsync(fichaId, cancellationToken);
        await LogSectionAsync(fichaId, "Actualizar actores", "los actores del ecosistema", before, actors, cancellationToken);
        return actors;
    }

    /// <summary>
    /// Comprueba que cada Componente PNMC pertenezca al Eje elegido en su misma fila. Evita que una
    /// petición manual —o un formulario que quedó con la lista anterior en pantalla— guarde una
    /// combinación que no existe en el catálogo oficial.
    /// </summary>
    private async Task EnsureComponentsBelongToTheirAxisAsync(
        IReadOnlyCollection<GovernancePnmcAxisDto> axes,
        CancellationToken cancellationToken)
    {
        var componentIds = axes
            .Where(axis => axis.PnmcComponentId is > 0)
            .Select(axis => axis.PnmcComponentId!.Value)
            .Distinct()
            .ToArray();

        if (componentIds.Length == 0)
        {
            return;
        }

        var componentAxisById = await dbContext.PnmcComponents.AsNoTracking()
            .Where(component => componentIds.Contains(component.Id))
            .ToDictionaryAsync(component => component.Id, component => component.PnmcAxisId, cancellationToken);

        var errors = new List<FieldValidationError>();
        var position = 0;

        foreach (var axis in axes)
        {
            position++;

            if (axis.PnmcComponentId is not > 0 || axis.PnmcAxisId is not > 0)
            {
                continue;
            }

            if (!componentAxisById.TryGetValue(axis.PnmcComponentId.Value, out var ownerAxisId) ||
                ownerAxisId != axis.PnmcAxisId.Value)
            {
                errors.Add(new FieldValidationError(
                    $"pnmcAxes[{position - 1}].pnmcComponentId",
                    $"Eje PNMC {position} · Componente PNMC",
                    "El componente seleccionado no corresponde al eje PNMC elegido. Vuelve a elegirlo en la lista."));
            }
        }

        DomainValidationException.ThrowIfAny("Revisa la información de los ejes PNMC antes de guardar.", errors);
    }

    /// <summary>
    /// Comprueba que cada Rol en el ecosistema pertenezca al Tipo de agente elegido en su fila.
    /// </summary>
    private async Task EnsureRolesBelongToTheirAgentTypeAsync(
        IReadOnlyCollection<GovernanceActorDto> actors,
        CancellationToken cancellationToken)
    {
        var roleIds = actors.SelectMany(actor => actor.EcosystemRoleIds).Distinct().ToArray();

        if (roleIds.Length == 0)
        {
            return;
        }

        var agentTypeByRole = await dbContext.EcosystemRoles.AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .ToDictionaryAsync(role => role.Id, role => role.AgentTypeId, cancellationToken);

        var errors = new List<FieldValidationError>();
        var position = 0;

        foreach (var actor in actors)
        {
            position++;

            if (actor.AgentTypeId is not > 0)
            {
                continue;
            }

            var mismatched = actor.EcosystemRoleIds
                .Where(roleId => !agentTypeByRole.TryGetValue(roleId, out var ownerId) || ownerId != actor.AgentTypeId.Value)
                .ToArray();

            if (mismatched.Length > 0)
            {
                errors.Add(new FieldValidationError(
                    $"actors[{position - 1}].ecosystemRoleIds",
                    $"Actor {position} · Rol en el ecosistema",
                    "Uno de los roles seleccionados no corresponde al tipo de agente elegido. Vuelve a elegirlos en la lista."));
            }
        }

        DomainValidationException.ThrowIfAny("Revisa la información de los actores antes de guardar.", errors);
    }

    private static GovernanceDiagnosticDto MapDiagnostic(DiagnosticoEcosistema x)
        => new(
            x.Id,
            x.CaracterizacionGeneral,
            x.FortalezasIdentificadas,
            x.PoliticasPriorizadas,
            x.DebilidadesIdentificadas,
            x.TensionesOConflictos,
            x.CommitteeStatusOptionId,
            x.PlanDepartamentalCulturaStatusId,
            x.ConsejoDepartamentalCultura,
            x.PlanDepartamentalMusicaStatusId,
            x.OrdenanzasCulturales,
            x.ConsejoDepartamentalMusica,
            x.MesaSectorialTerritorial,
            x.Observaciones);

    private static GovernanceOpportunityDto MapOpportunity(OportunidadCambio x)
        => new(x.Id, x.SituacionIdentificada, x.ComponenteOtrasDependenciasEntidades, x.AliadosYCreyentes, x.TerritorioInfluencia, x.PriorityLevelOptionId, x.DescripcionAdicional);

    private static GovernancePnmcAxisDto MapPnmcAxis(EjePnmcRegistro x)
        => new(
            x.Id,
            x.DescripcionHallazgo,
            x.PnmcAxisId,
            x.PnmcComponentId,
            x.AccionEstrategica,
            x.PoliticaPriorizada,
            x.ArmonizacionPnc,
            x.ArmonizacionPnd,
            x.ArmonizacionInternacional,
            x.PriorityLevelOptionId,
            x.AliadosResponsables,
            x.FuentesFinanciacion,
            x.ValorPropuestaCop,
            x.Enfoques.Select(e => e.ApproachOptionId).ToArray(),
            x.Descripcion,
            x.ScheduleOptionId,
            x.ProposalStatusOptionId,
            x.Observaciones);

    private static GovernanceActorDto MapActor(Actor x)
        => new(
            x.Id,
            x.NombreAgente,
            x.AgentTypeId,
            x.RolesEcosistema.Select(r => r.EcosystemRoleId).ToArray(),
            x.NivelesTerritoriales.Select(n => n.TerritorialLevelOptionId).ToArray(),
            x.NumeroContacto,
            x.CorreoElectronico,
            x.Observaciones);

    /// <summary>
    /// Verifica si el usuario actual puede acceder a la ficha especificada.
    /// El Gestor Departamental solo puede acceder a sus propias fichas.
    /// El Líder de Gobernanza y el Administrador pueden acceder a todas las fichas.
    /// </summary>
    private bool CanAccessFicha(FichaDepartamental ficha)
    {
        // Líder de Gobernanza y Administrador pueden acceder a todas las fichas
        if (currentUserService.HasAnyRole(RoleLiderGobernanza, RoleAdministrador))
        {
            return true;
        }

        // Gestor Departamental solo puede acceder a sus propias fichas
        if (currentUserService.HasAnyRole(RoleGestorDepartamental))
        {
            var userEmail = currentUserService.Email;
            return !string.IsNullOrWhiteSpace(userEmail) && ficha.CreatedByEmail == userEmail;
        }

        // Si no tiene ningún rol conocido, no puede acceder
        return false;
    }

    /// <summary>
    /// Verifica si el usuario actual puede acceder a la ficha por ID.
    /// Lanza UnauthorizedAccessException si no tiene permisos.
    /// </summary>
    private async Task<FichaDepartamental> GetFichaWithAccessCheckAsync(Guid fichaId, CancellationToken cancellationToken)
    {
        var ficha = await dbContext.FichasDepartamentales
            .SingleAsync(x => x.Id == fichaId, cancellationToken);

        if (!CanAccessFicha(ficha))
        {
            throw new UnauthorizedAccessException("No tiene permisos para acceder a esta ficha.");
        }

        return ficha;
    }

    // ── Auditoría ────────────────────────────────────────────────────────────────────────────

    /// <summary>Nombre de la ficha en palabras, para leer el historial sin tener que abrirla.</summary>
    private async Task<string> DescribeFichaAsync(Guid fichaId, CancellationToken cancellationToken)
    {
        // El departamento se busca aparte del resto de la ficha: si el catálogo no lo tuviera, el
        // registro de auditoría igual debe decir de qué fecha era la ficha.
        var ficha = await dbContext.FichasDepartamentales.AsNoTracking()
            .Where(x => x.Id == fichaId)
            .Select(x => new { x.DepartmentId, x.FechaLevantamiento })
            .SingleOrDefaultAsync(cancellationToken);

        if (ficha is null)
        {
            return $"ficha {fichaId}";
        }

        var departmentName = await dbContext.Departments.AsNoTracking()
            .Where(x => x.Id == ficha.DepartmentId)
            .Select(x => x.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(departmentName)
            ? $"ficha del {ficha.FechaLevantamiento:dd/MM/yyyy}"
            : $"ficha de {departmentName} · {ficha.FechaLevantamiento:dd/MM/yyyy}";
    }

    /// <summary>
    /// Registra el guardado de una sección de lista (oportunidades, ejes o actores). Lo que
    /// interesa en el historial es cuántos registros quedaron; el contenido exacto de antes y
    /// después queda en los valores JSON del detalle.
    /// </summary>
    private async Task LogSectionAsync<TItem>(
        Guid fichaId,
        string operation,
        string sectionLabel,
        IReadOnlyCollection<TItem> before,
        IReadOnlyCollection<TItem> after,
        CancellationToken cancellationToken)
    {
        var label = await DescribeFichaAsync(fichaId, cancellationToken);
        var description = before.Count == after.Count
            ? $"Guardó {sectionLabel} de la {label}: quedaron {after.Count} registros."
            : $"Guardó {sectionLabel} de la {label}: pasó de {before.Count} a {after.Count} registros.";

        await auditService.LogAsync(new AuditEntry
        {
            Module = AuditModules.Gobernanza,
            EntityName = EntityName,
            EntityId = fichaId,
            EntityLabel = label,
            Operation = operation,
            Description = description,
            Changes = new AuditChangeSet()
                .Track("count", $"Registros en {sectionLabel}", before.Count, after.Count)
                .Changes,
            OldValuesJson = JsonSerializer.Serialize(before),
            NewValuesJson = JsonSerializer.Serialize(after)
        }, cancellationToken);
    }

    /// <summary>Nombres de los valores de catálogo indicados, para mostrarlos en vez del id.</summary>
    private static async Task<Dictionary<int, string>> CatalogNamesAsync<TEntity>(
        IQueryable<TEntity> catalog,
        IEnumerable<int?> ids,
        CancellationToken cancellationToken)
        where TEntity : CatalogEntityBase
    {
        var wanted = ids.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToArray();
        if (wanted.Length == 0)
        {
            return [];
        }

        return await catalog.AsNoTracking()
            .Where(x => wanted.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
    }

    /// <summary>Campos de la identificación con los catálogos resueltos a su nombre.</summary>
    private async Task<AuditChangeSet> BuildIdentificationChangesAsync(
        GovernanceFichaDetailDto? before,
        GovernanceFichaDetailDto after,
        CancellationToken cancellationToken)
    {
        var departments = await CatalogNamesAsync(dbContext.Departments, [before?.DepartmentId, after.DepartmentId], cancellationToken);
        var municipalities = await CatalogNamesAsync(dbContext.Municipalities, [before?.MunicipalityId, after.MunicipalityId], cancellationToken);
        var regions = await CatalogNamesAsync(dbContext.RegionOcadOptions, [before?.RegionOcadOptionId, after.RegionOcadOptionId], cancellationToken);
        var sources = await CatalogNamesAsync(
            dbContext.InformationSourceOptions,
            (before?.InformationSourceIds ?? []).Concat(after.InformationSourceIds).Select(id => (int?)id),
            cancellationToken);

        static string? Name(Dictionary<int, string> names, int? id) =>
            id.HasValue ? names.GetValueOrDefault(id.Value, id.Value.ToString()) : null;

        static string[] Names(Dictionary<int, string> names, IEnumerable<int>? ids) =>
            (ids ?? []).Select(id => names.GetValueOrDefault(id, id.ToString())).ToArray();

        return new AuditChangeSet()
            .Track("fechaLevantamiento", "Fecha de levantamiento", before?.FechaLevantamiento, after.FechaLevantamiento)
            .Track("departmentId", "Departamento", Name(departments, before?.DepartmentId), Name(departments, after.DepartmentId))
            .Track("municipalityId", "Municipio", Name(municipalities, before?.MunicipalityId), Name(municipalities, after.MunicipalityId))
            .Track("responsableRegistro", "Responsable del registro", before?.ResponsableRegistro, after.ResponsableRegistro)
            .Track("regionOcadOptionId", "Región OCAD", Name(regions, before?.RegionOcadOptionId), Name(regions, after.RegionOcadOptionId))
            .Track("informationSourceIds", "Fuentes de información", Names(sources, before?.InformationSourceIds), Names(sources, after.InformationSourceIds))
            .Track("observaciones", "Observaciones", before?.Observaciones, after.Observaciones);
    }
}
