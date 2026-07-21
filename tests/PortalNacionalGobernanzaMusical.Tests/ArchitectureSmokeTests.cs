using PortalNacionalGobernanzaMusical.Application;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Shared.Constants;

namespace PortalNacionalGobernanzaMusical.Tests;

public sealed class ArchitectureSmokeTests
{
    [Fact]
    public void DomainEntity_ShouldInitializeRequiredValues()
    {
        var ficha = new FichaDepartamental
        {
            FechaLevantamiento = new DateOnly(2026, 7, 6),
            DepartmentId = 2,
            ResponsableRegistro = "Gestor departamental"
        };

        Assert.Equal(new DateOnly(2026, 7, 6), ficha.FechaLevantamiento);
        Assert.Equal(2, ficha.DepartmentId);
        Assert.Equal("Gestor departamental", ficha.ResponsableRegistro);
        Assert.NotEqual(Guid.Empty, ficha.Id);
    }

    [Fact]
    public void CatalogEntity_ShouldAllowDependentRelationships()
    {
        var agentType = new AgentType { Name = "Sectorial", DisplayOrder = 1 };
        var role = new EcosystemRole { Name = "Docentes y formadores(as)", DisplayOrder = 1, AgentType = agentType };

        Assert.Equal("Sectorial", agentType.Name);
        Assert.Equal("Docentes y formadores(as)", role.Name);
        Assert.Same(agentType, role.AgentType);
    }

    [Fact]
    public void SharedRoles_ShouldExposeExpectedConstants()
    {
        Assert.Equal("Administrador", Roles.Administrador);
        Assert.Equal("LiderGobernanza", Roles.LiderGobernanza);
        Assert.Equal("GestorDepartamental", Roles.GestorDepartamental);
    }

    [Fact]
    public void ApplicationAssemblyMarker_ShouldBeResolvable()
    {
        var markerType = typeof(ApplicationAssemblyMarker);

        Assert.Equal("PortalNacionalGobernanzaMusical.Application", markerType.Namespace);
    }
}
