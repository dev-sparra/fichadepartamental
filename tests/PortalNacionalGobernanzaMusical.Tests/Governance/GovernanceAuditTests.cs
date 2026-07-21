using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Infrastructure.Audit;
using PortalNacionalGobernanzaMusical.Infrastructure.Governance;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Tests.Governance;

/// <summary>
/// Verifica que las mutaciones de la ficha registren auditoría completa: usuario, IP,
/// operación y valores anterior/nuevo. Usa EF Core InMemory (no requiere MySQL).
/// </summary>
public sealed class GovernanceAuditTests
{
    private const string UserEmail = "lider@test.gov.co";
    private const string UserIp = "192.168.10.4";

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public string? Email => UserEmail;
        public string? IpAddress => UserIp;
        public IReadOnlyCollection<string> Roles => ["Administrador"];
        public bool HasAnyRole(params string[] roles) => roles.Any(r => Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static GovernanceFichaService NewService(ApplicationDbContext context)
    {
        var currentUser = new FakeCurrentUser();
        return new(context, new AuditService(context, currentUser), currentUser);
    }

    private static UpdateGovernanceFichaRequest FichaRequest() =>
        new(new DateOnly(2026, 3, 15), 1, null, "Gestor de prueba", null, "obs", Array.Empty<int>());

    private static GovernanceDiagnosticDto Diagnostic(string caracterizacion) =>
        new(null, caracterizacion, null, null, null, null, null, null, null, null, null, null, null, null);

    private static GovernanceOpportunityDto Opportunity(string situacion) =>
        new(null, situacion, null, null, null, null, null);

    [Fact]
    public async Task CreateFicha_ShouldWriteCreateAuditWithUserAndIp()
    {
        using var context = NewContext();
        var service = NewService(context);

        var ficha = await service.CreateFichaAsync(FichaRequest());

        var log = await context.Set<AuditLog>().SingleAsync(x => x.Operation == "Crear");
        Assert.Equal("FichaDepartamental", log.EntityName);
        Assert.Equal(ficha.Id, log.EntityId);
        Assert.Equal(UserEmail, log.UserEmail);
        Assert.Equal(UserIp, log.IpAddress);
        Assert.NotNull(log.NewValuesJson);
    }

    [Fact]
    public async Task UpdateDiagnostic_ShouldAuditPreviousAndNewValues()
    {
        using var context = NewContext();
        var service = NewService(context);
        var ficha = await service.CreateFichaAsync(FichaRequest());

        await service.UpdateDiagnosticAsync(ficha.Id, Diagnostic("Estado inicial"));
        await service.UpdateDiagnosticAsync(ficha.Id, Diagnostic("Estado nuevo"));

        var logs = await context.Set<AuditLog>()
            .Where(x => x.Operation == "Actualizar diagnóstico")
            .ToListAsync();

        Assert.Equal(2, logs.Count);
        // Primer diligenciamiento: sin valor anterior.
        Assert.Contains(logs, l => l.OldValuesJson == null && l.NewValuesJson!.Contains("Estado inicial"));
        // Segundo: valor anterior = A, valor nuevo = B.
        Assert.Contains(logs, l => l.OldValuesJson != null
            && l.OldValuesJson.Contains("Estado inicial")
            && l.NewValuesJson!.Contains("Estado nuevo"));
        Assert.All(logs, l => Assert.Equal(UserIp, l.IpAddress));
    }

    [Fact]
    public async Task ReplaceOpportunities_ShouldAuditPreviousList()
    {
        using var context = NewContext();
        var service = NewService(context);
        var ficha = await service.CreateFichaAsync(FichaRequest());

        await service.ReplaceOpportunitiesAsync(ficha.Id, [Opportunity("Oportunidad inicial")]);
        await service.ReplaceOpportunitiesAsync(ficha.Id, [Opportunity("Oportunidad nueva")]);

        var logs = await context.Set<AuditLog>()
            .Where(x => x.Operation == "Actualizar oportunidades")
            .ToListAsync();

        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.NewValuesJson!.Contains("Oportunidad inicial"));
        Assert.Contains(logs, l => l.OldValuesJson!.Contains("Oportunidad inicial")
            && l.NewValuesJson!.Contains("Oportunidad nueva"));
    }
}
