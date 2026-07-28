using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Audit;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Infrastructure.Audit;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Tests.Audit;

/// <summary>
/// El historial tiene que responder quién hizo qué, sobre qué y desde dónde, y poder filtrarse.
/// Usa EF Core InMemory (no requiere MySQL).
/// </summary>
public sealed class AuditServiceTests
{
    private sealed class FakeCurrentUser(string? email, params string[] roles) : ICurrentUserService
    {
        public string? Email => email;
        public string? IpAddress => "10.0.0.7";
        public string? RequestMethod => "POST";
        public string? RequestPath => "/api/governance/fichas";
        public IReadOnlyCollection<string> Roles => roles;
        public bool HasAnyRole(params string[] wanted) => wanted.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AuditService NewService(ApplicationDbContext context, string? email = "ana@mincultura.gov.co", params string[] roles) =>
        new(context, new FakeCurrentUser(email, roles.Length > 0 ? roles : ["Administrador"]));

    private static AuditEntry SampleEntry(string module = AuditModules.Gobernanza, string operation = "Crear ficha") => new()
    {
        Module = module,
        EntityName = "FichaDepartamental",
        EntityId = Guid.NewGuid(),
        EntityLabel = "ficha de Antioquia · 15/03/2026",
        Operation = operation,
        Description = "Creó la ficha de Antioquia."
    };

    [Fact]
    public async Task RegistrarUnaAccion_GuardaQuienDondeYDesdeQueEquipo()
    {
        using var context = NewContext();
        var service = NewService(context, "ana@mincultura.gov.co", "Administrador", "Líder de Gobernanza");

        await service.LogAsync(SampleEntry());

        var log = await context.Set<AuditLog>().SingleAsync();
        Assert.Equal("ana@mincultura.gov.co", log.UserEmail);
        Assert.Equal("Administrador, Líder de Gobernanza", log.UserRoles);
        Assert.Equal("10.0.0.7", log.IpAddress);
        Assert.Equal("POST", log.RequestMethod);
        Assert.Equal("/api/governance/fichas", log.RequestPath);
        Assert.Equal(AuditModules.Gobernanza, log.Module);
        Assert.Equal("Exitoso", log.Result);
    }

    [Fact]
    public async Task ElNombreDelUsuario_SeTomaDelCatalogoDeUsuarios()
    {
        using var context = NewContext();
        context.UserAccounts.Add(new UserAccount
        {
            Email = "ana@mincultura.gov.co",
            NormalizedEmail = "ANA@MINCULTURA.GOV.CO",
            DisplayName = "Ana López",
            IsActive = true
        });
        await context.SaveChangesAsync();

        await NewService(context).LogAsync(SampleEntry());

        var log = await context.Set<AuditLog>().SingleAsync();
        Assert.Equal("Ana López", log.UserDisplayName);
    }

    [Fact]
    public async Task SinSesion_LaAccionQuedaANombreDelCorreoIndicado()
    {
        using var context = NewContext();
        var service = NewService(context, email: null);

        await service.LogAsync(new AuditEntry
        {
            Module = AuditModules.Autenticacion,
            EntityName = "UserAccount",
            Operation = "Intento de ingreso fallido",
            Result = AuditResults.Fallido,
            UserEmailOverride = "intruso@ejemplo.com"
        });

        var log = await context.Set<AuditLog>().SingleAsync();
        Assert.Equal("intruso@ejemplo.com", log.UserEmail);
        Assert.Equal(AuditResults.Fallido, log.Result);
        Assert.Null(log.EntityId);
    }

    [Fact]
    public async Task LosCambiosCampoACampo_SeRecuperanIgualQueSeGuardaron()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry() with
        {
            Changes = new AuditChangeSet()
                .Track("email", "Correo", "viejo@x.com", "nuevo@x.com")
                .Track("isActive", "Activo", true, false)
                .Changes
        });

        var page = await service.GetLogsAsync(new AuditLogQuery());

        var log = Assert.Single(page.Items);
        Assert.Collection(
            log.Changes,
            change =>
            {
                Assert.Equal("Correo", change.Label);
                Assert.Equal("viejo@x.com", change.Before);
                Assert.Equal("nuevo@x.com", change.After);
            },
            change =>
            {
                Assert.Equal("Activo", change.Label);
                Assert.Equal("Sí", change.Before);
                Assert.Equal("No", change.After);
            });
    }

    [Fact]
    public async Task ElHistorial_SeDevuelveDeLoMasRecienteALoMasAntiguo()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry(operation: "Primera"));
        await Task.Delay(10);
        await service.LogAsync(SampleEntry(operation: "Segunda"));

        var page = await service.GetLogsAsync(new AuditLogQuery());

        Assert.Equal("Segunda", page.Items.First().Operation);
    }

    [Fact]
    public async Task ElFiltroPorModulo_DevuelveSoloEseModulo()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry(AuditModules.Gobernanza));
        await service.LogAsync(SampleEntry(AuditModules.Catalogos));
        await service.LogAsync(SampleEntry(AuditModules.Catalogos));

        var page = await service.GetLogsAsync(new AuditLogQuery { Module = AuditModules.Catalogos });

        Assert.Equal(2, page.Total);
        Assert.All(page.Items, log => Assert.Equal(AuditModules.Catalogos, log.Module));
    }

    [Fact]
    public async Task LaBusquedaLibre_EncuentraPorObjetoAfectado()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry() with { EntityLabel = "ficha de Antioquia · 15/03/2026" });
        await service.LogAsync(SampleEntry() with { EntityLabel = "ficha de Boyacá · 15/03/2026" });

        var page = await service.GetLogsAsync(new AuditLogQuery { Search = "Boyacá" });

        var log = Assert.Single(page.Items);
        Assert.Contains("Boyacá", log.EntityLabel);
    }

    [Fact]
    public async Task ElFiltroPorFecha_DejaFueraLoAnteriorAlRango()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry());
        var antiguo = await context.Set<AuditLog>().SingleAsync();
        antiguo.CreatedAtUtc = DateTime.UtcNow.AddDays(-10);
        await context.SaveChangesAsync();

        await service.LogAsync(SampleEntry(operation: "Reciente"));

        var page = await service.GetLogsAsync(new AuditLogQuery { FromUtc = DateTime.UtcNow.AddDays(-1) });

        var log = Assert.Single(page.Items);
        Assert.Equal("Reciente", log.Operation);
    }

    [Fact]
    public async Task LaPaginacion_DevuelveElTotalCompletoYSoloLaPaginaPedida()
    {
        using var context = NewContext();
        var service = NewService(context);

        for (var i = 0; i < 7; i++)
        {
            await service.LogAsync(SampleEntry(operation: $"Acción {i}"));
        }

        var page = await service.GetLogsAsync(new AuditLogQuery { Page = 2, PageSize = 3 });

        Assert.Equal(7, page.Total);
        Assert.Equal(3, page.Items.Count);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task LasOpcionesDeFiltro_SalenDeLoQueYaHayEnElHistorial()
    {
        using var context = NewContext();
        var service = NewService(context);

        await service.LogAsync(SampleEntry(AuditModules.Gobernanza, "Crear ficha"));
        await service.LogAsync(SampleEntry(AuditModules.Catalogos, "Crear valor de catálogo"));

        var options = await service.GetFilterOptionsAsync();

        Assert.Equal([AuditModules.Catalogos, AuditModules.Gobernanza], options.Modules.Order());
        Assert.Contains("Crear ficha", options.Operations);
        var user = Assert.Single(options.Users);
        Assert.Equal("ana@mincultura.gov.co", user.Email);
    }

    [Fact]
    public async Task UnRegistroAntiguoSinModulo_SeMuestraComoSinClasificar()
    {
        using var context = NewContext();
        context.Set<AuditLog>().Add(new AuditLog
        {
            UserEmail = "ana@mincultura.gov.co",
            UserDisplayName = "Ana",
            EntityName = "FichaDepartamental",
            Operation = "Actualizar",
            Module = string.Empty,
            Result = string.Empty
        });
        await context.SaveChangesAsync();

        var page = await NewService(context).GetLogsAsync(new AuditLogQuery());

        var log = Assert.Single(page.Items);
        Assert.Equal("Sin clasificar", log.Module);
        Assert.Equal(AuditResults.Exitoso, log.Result);
    }
}
