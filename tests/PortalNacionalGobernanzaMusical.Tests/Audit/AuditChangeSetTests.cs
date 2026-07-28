using PortalNacionalGobernanzaMusical.Application.Audit;

namespace PortalNacionalGobernanzaMusical.Tests.Audit;

/// <summary>
/// El detalle de auditoría solo tiene valor si dice exactamente qué cambió y muestra los valores
/// como los ve el usuario, no como los guarda la base de datos.
/// </summary>
public sealed class AuditChangeSetTests
{
    [Fact]
    public void UnCampoQueNoCambia_NoSeRegistra()
    {
        var changes = new AuditChangeSet().Track("name", "Nombre", "Antioquia", "Antioquia");

        Assert.False(changes.HasChanges);
        Assert.Empty(changes.Changes);
    }

    [Fact]
    public void UnCampoQueCambia_SeRegistraConSuValorAnteriorYElNuevo()
    {
        var changes = new AuditChangeSet().Track("name", "Nombre", "Medellin", "Medellín");

        var change = Assert.Single(changes.Changes);
        Assert.Equal("name", change.Field);
        Assert.Equal("Nombre", change.Label);
        Assert.Equal("Medellin", change.Before);
        Assert.Equal("Medellín", change.After);
    }

    [Fact]
    public void LosValoresVacios_SeMuestranComoVacio()
    {
        var changes = new AuditChangeSet().Track("observaciones", "Observaciones", null, "Sin novedad");

        var change = Assert.Single(changes.Changes);
        Assert.Equal("(vacío)", change.Before);
    }

    [Theory]
    [InlineData(true, "Sí")]
    [InlineData(false, "No")]
    public void LosBooleanos_SeMuestranEnPalabras(bool value, string expected)
    {
        Assert.Equal(expected, AuditChangeSet.Format(value));
    }

    [Fact]
    public void LasFechas_SeMuestranEnFormatoDiaMesAño()
    {
        Assert.Equal("15/03/2026", AuditChangeSet.Format(new DateOnly(2026, 3, 15)));
    }

    [Fact]
    public void LasListas_SeMuestranSeparadasPorComa()
    {
        var roles = new[] { "Administrador", "Gestor Departamental" };

        Assert.Equal("Administrador, Gestor Departamental", AuditChangeSet.Format(roles));
    }

    [Fact]
    public void UnaListaQueCambiaDeOrdenPeroNoDeContenido_SeRegistraComoCambio()
    {
        // El orden de los roles es el que se guardó: se deja constancia y quien revise decide.
        var changes = new AuditChangeSet()
            .Track("roles", "Roles", new[] { "A", "B" }, new[] { "B", "A" });

        Assert.True(changes.HasChanges);
    }

    [Fact]
    public void UnTextoMuyLargo_SeRecortaParaQueElHistorialSigaSiendoLegible()
    {
        var largo = new string('a', 900);

        var formatted = AuditChangeSet.Format(largo);

        Assert.EndsWith("…", formatted);
        Assert.Equal(601, formatted.Length);
    }

    [Fact]
    public void UnValorSecreto_SeRegistraSinExponerElContenido()
    {
        var changes = new AuditChangeSet().TrackSecret("password", "Contraseña");

        var change = Assert.Single(changes.Changes);
        Assert.Equal("(oculto)", change.Before);
        Assert.Equal("(oculto)", change.After);
    }

    [Fact]
    public void LosCamposCambiados_SeEnumeranEnPalabrasParaLaDescripcion()
    {
        var changes = new AuditChangeSet()
            .Track("email", "Correo", "a@b.com", "c@d.com")
            .Track("isActive", "Activo", true, false)
            .Track("roles", "Roles", new[] { "A" }, new[] { "B" });

        Assert.Equal("Correo, Activo y Roles", changes.DescribeChangedFields());
    }

    [Fact]
    public void UnSoloCampoCambiado_SeEnumeraSinConjuncion()
    {
        var changes = new AuditChangeSet().Track("email", "Correo", "a@b.com", "c@d.com");

        Assert.Equal("Correo", changes.DescribeChangedFields());
    }
}
