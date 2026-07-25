using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Governance;

public sealed class GovernanceFichaService(
    ApplicationDbContext dbContext,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IGovernanceFichaService
{
    private const string EntityName = "FichaDepartamental";
    private const string RoleGestorDepartamental = "Gestor Departamental";
    private const string RoleLiderGobernanza = "Líder de Gobernanza";
    private const string RoleAdministrador = "Administrador";

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
                x.Actores.Count))
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

        await auditService.LogAsync(EntityName, ficha.Id, "Crear", null, JsonSerializer.Serialize(created), cancellationToken);
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

        await auditService.LogAsync(EntityName, ficha.Id, "Actualizar identificación", JsonSerializer.Serialize(before), JsonSerializer.Serialize(after), cancellationToken);
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

        await auditService.LogAsync(EntityName, id, "Eliminar", JsonSerializer.Serialize(fichaSummary), null, cancellationToken);
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
        var beforeJson = existing is null ? null : JsonSerializer.Serialize(MapDiagnostic(existing));
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
        await auditService.LogAsync(EntityName, fichaId, "Actualizar diagnóstico", beforeJson, JsonSerializer.Serialize(diagnostic), cancellationToken);
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
        await auditService.LogAsync(EntityName, fichaId, "Actualizar oportunidades", JsonSerializer.Serialize(before), JsonSerializer.Serialize(opportunities), cancellationToken);
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
        await auditService.LogAsync(EntityName, fichaId, "Actualizar ejes PNMC", JsonSerializer.Serialize(before), JsonSerializer.Serialize(axes), cancellationToken);
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
        await auditService.LogAsync(EntityName, fichaId, "Actualizar actores", JsonSerializer.Serialize(before), JsonSerializer.Serialize(actors), cancellationToken);
        return actors;
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
}
