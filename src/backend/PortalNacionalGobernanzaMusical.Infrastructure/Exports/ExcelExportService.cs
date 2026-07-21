using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Exports;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Exports;

/// <summary>
/// Exporta una ficha a Excel <b>sobre la plantilla oficial <c>.xlsm</c></b>, preservando macros,
/// validaciones, estilos y estructura. Solo escribe las hojas del Gestor (Identificación → Actores);
/// las hojas del Líder (Indicadores, Detalle) se cablearán junto con ese módulo — el escritor ya es
/// genérico y las soporta.
/// </summary>
public sealed class ExcelExportService(ApplicationDbContext dbContext, IFichaWorkbookWriter workbookWriter) : IExcelExportService
{
    private const string MultiSeparator = ", ";

    // La tabla de cada hoja del Excel abarca 50 filas de datos (2..51).
    private const int MaxCollectionRows = 50;

    public async Task<byte[]> ExportFichaToExcelAsync(Guid fichaId, CancellationToken cancellationToken = default)
    {
        var ficha = await dbContext.FichasDepartamentales.AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.Municipality)
            .Include(x => x.RegionOcadOption)
            .Include(x => x.FuentesInformacion).ThenInclude(x => x.InformationSourceOption)
            .Include(x => x.DiagnosticoEcosistema).ThenInclude(d => d!.CommitteeStatusOption)
            .Include(x => x.DiagnosticoEcosistema).ThenInclude(d => d!.PlanDepartamentalCulturaStatus)
            .Include(x => x.DiagnosticoEcosistema).ThenInclude(d => d!.PlanDepartamentalMusicaStatus)
            .Include(x => x.OportunidadesCambio).ThenInclude(o => o.PriorityLevelOption)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.PnmcAxis)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.PnmcComponent)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.PriorityLevelOption)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.ScheduleOption)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.ProposalStatusOption)
            .Include(x => x.EjesPnmc).ThenInclude(e => e.Enfoques).ThenInclude(en => en.ApproachOption)
            .Include(x => x.Actores).ThenInclude(a => a.AgentType)
            .Include(x => x.Actores).ThenInclude(a => a.RolesEcosistema).ThenInclude(r => r.EcosystemRole)
            .Include(x => x.Actores).ThenInclude(a => a.NivelesTerritoriales).ThenInclude(n => n.TerritorialLevelOption)
            .SingleAsync(x => x.Id == fichaId, cancellationToken);

        var sheets = new List<FichaExportSheet>
        {
            BuildIdentificacion(ficha),
            BuildOportunidades(ficha),
            BuildEjes(ficha),
            BuildActores(ficha)
        };

        var diagnostico = BuildDiagnostico(ficha);
        if (diagnostico is not null)
        {
            sheets.Insert(1, diagnostico);
        }

        return workbookWriter.Write(sheets);
    }

    private static FichaExportSheet BuildIdentificacion(FichaDepartamental ficha)
    {
        var values = new Dictionary<string, object?>
        {
            ["fechaLevantamiento"] = ficha.FechaLevantamiento,
            ["departmentId"] = ficha.Department?.Name,
            ["municipalityId"] = ficha.Municipality?.Name,
            ["responsableRegistro"] = ficha.ResponsableRegistro,
            ["regionOcadOptionId"] = ficha.RegionOcadOption?.Name,
            ["informationSourceIds"] = JoinNames(ficha.FuentesInformacion.Select(x => x.InformationSourceOption?.Name)),
            ["observaciones"] = ficha.Observaciones
        };

        return new FichaExportSheet("identificacion", [new FichaExportRow(2, values)]);
    }

    private static FichaExportSheet? BuildDiagnostico(FichaDepartamental ficha)
    {
        var diagnostico = ficha.DiagnosticoEcosistema;
        if (diagnostico is null)
        {
            return null;
        }

        var values = new Dictionary<string, object?>
        {
            ["caracterizacionGeneral"] = diagnostico.CaracterizacionGeneral,
            ["fortalezasIdentificadas"] = diagnostico.FortalezasIdentificadas,
            ["politicasPriorizadas"] = diagnostico.PoliticasPriorizadas,
            ["debilidadesIdentificadas"] = diagnostico.DebilidadesIdentificadas,
            ["tensionesOConflictos"] = diagnostico.TensionesOConflictos,
            ["committeeStatusOptionId"] = diagnostico.CommitteeStatusOption?.Name,
            ["planDepartamentalCulturaStatusId"] = diagnostico.PlanDepartamentalCulturaStatus?.Name,
            ["consejoDepartamentalCultura"] = diagnostico.ConsejoDepartamentalCultura,
            ["planDepartamentalMusicaStatusId"] = diagnostico.PlanDepartamentalMusicaStatus?.Name,
            ["ordenanzasCulturales"] = diagnostico.OrdenanzasCulturales,
            ["consejoDepartamentalMusica"] = diagnostico.ConsejoDepartamentalMusica,
            ["mesaSectorialTerritorial"] = diagnostico.MesaSectorialTerritorial,
            ["observaciones"] = diagnostico.Observaciones
        };

        return new FichaExportSheet("diagnostico", [new FichaExportRow(2, values)]);
    }

    private static FichaExportSheet BuildOportunidades(FichaDepartamental ficha)
    {
        var rows = ficha.OportunidadesCambio.Take(MaxCollectionRows).Select((o, index) =>
            new FichaExportRow(2 + index, new Dictionary<string, object?>
            {
                ["situacionIdentificada"] = o.SituacionIdentificada,
                ["componenteOtrasDependenciasEntidades"] = o.ComponenteOtrasDependenciasEntidades,
                ["aliadosYCreyentes"] = o.AliadosYCreyentes,
                ["territorioInfluencia"] = o.TerritorioInfluencia,
                ["priorityLevelOptionId"] = o.PriorityLevelOption?.Name,
                ["descripcionAdicional"] = o.DescripcionAdicional
            })).ToArray();

        return new FichaExportSheet("oportunidades", rows);
    }

    private static FichaExportSheet BuildEjes(FichaDepartamental ficha)
    {
        var rows = ficha.EjesPnmc.Take(MaxCollectionRows).Select((e, index) =>
            new FichaExportRow(2 + index, new Dictionary<string, object?>
            {
                ["descripcionHallazgo"] = e.DescripcionHallazgo,
                ["pnmcAxisId"] = e.PnmcAxis?.Name,
                ["pnmcComponentId"] = e.PnmcComponent?.Name,
                ["accionEstrategica"] = e.AccionEstrategica,
                ["politicaPriorizada"] = e.PoliticaPriorizada,
                ["armonizacionPnc"] = e.ArmonizacionPnc,
                ["armonizacionPnd"] = e.ArmonizacionPnd,
                ["armonizacionInternacional"] = e.ArmonizacionInternacional,
                ["priorityLevelOptionId"] = e.PriorityLevelOption?.Name,
                ["aliadosResponsables"] = e.AliadosResponsables,
                ["fuentesFinanciacion"] = e.FuentesFinanciacion,
                ["valorPropuestaCop"] = e.ValorPropuestaCop,
                ["approachOptionIds"] = JoinNames(e.Enfoques.Select(x => x.ApproachOption?.Name)),
                ["descripcion"] = e.Descripcion,
                ["scheduleOptionId"] = e.ScheduleOption?.Name,
                ["proposalStatusOptionId"] = e.ProposalStatusOption?.Name,
                ["observaciones"] = e.Observaciones
            })).ToArray();

        return new FichaExportSheet("ejes-pnmc", rows);
    }

    private static FichaExportSheet BuildActores(FichaDepartamental ficha)
    {
        var rows = ficha.Actores.Take(MaxCollectionRows).Select((a, index) =>
            new FichaExportRow(2 + index, new Dictionary<string, object?>
            {
                ["nombreAgente"] = a.NombreAgente,
                ["agentTypeId"] = a.AgentType?.Name,
                ["ecosystemRoleIds"] = JoinNames(a.RolesEcosistema.Select(x => x.EcosystemRole?.Name)),
                ["territorialLevelOptionIds"] = JoinNames(a.NivelesTerritoriales.Select(x => x.TerritorialLevelOption?.Name)),
                ["numeroContacto"] = a.NumeroContacto,
                ["correoElectronico"] = a.CorreoElectronico,
                ["observaciones"] = a.Observaciones
            })).ToArray();

        return new FichaExportSheet("actores", rows);
    }

    private static string? JoinNames(IEnumerable<string?> names)
    {
        var joined = string.Join(MultiSeparator, names.Where(name => !string.IsNullOrWhiteSpace(name)));
        return string.IsNullOrEmpty(joined) ? null : joined;
    }
}
