using PortalNacionalGobernanzaMusical.Application.Common;
using PortalNacionalGobernanzaMusical.Application.Governance;

namespace PortalNacionalGobernanzaMusical.Tests.Governance;

/// <summary>
/// El backend valida los tipos de captura de la ficha con las mismas reglas del formulario web y
/// devuelve mensajes ya redactados para el usuario, con la etiqueta del campo tal como aparece en
/// la Ficha Departamental.
/// </summary>
public sealed class GovernanceRequestValidationTests
{
    private static UpdateGovernanceFichaRequest Ficha(
        DateOnly? fecha = null,
        int departmentId = 1,
        string responsable = "Gestor Departamental") =>
        new(fecha ?? new DateOnly(2026, 3, 15), departmentId, null, responsable, null, null, []);

    private static GovernanceActorDto Actor(string? phone = "3001234567", string? email = "gestor@entidad.gov.co") =>
        new(null, "Casa de la Cultura", null, [], [], phone, email, null);

    [Fact]
    public void EnsureValid_ValidFicha_ShouldNotThrow()
    {
        GovernanceRequestValidation.EnsureValid(Ficha());
    }

    [Fact]
    public void EnsureValid_MissingResponsable_ShouldReportTheFieldLabel()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid(Ficha(responsable: "   ")));

        var error = Assert.Single(exception.Errors);
        Assert.Equal("Responsable del registro (Gestor)", error.FieldLabel);
        Assert.Contains("Escribe el nombre", error.Message);
    }

    [Fact]
    public void EnsureValid_DateOutOfRange_ShouldReportTheAllowedRange()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid(Ficha(fecha: new DateOnly(1990, 1, 1))));

        Assert.Contains(exception.Errors, error => error.FieldLabel == "Fecha de levantamiento");
    }

    [Fact]
    public void EnsureValid_MissingDepartment_ShouldAskToSelectFromTheList()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid(Ficha(departmentId: 0)));

        var error = Assert.Single(exception.Errors);
        Assert.Equal("Departamento", error.FieldLabel);
    }

    [Theory]
    [InlineData("300123456")]      // 9 dígitos
    [InlineData("30012345678")]    // 11 dígitos
    [InlineData("300 123 4567")]   // con espacios
    [InlineData("(300)1234567")]   // con separadores
    public void EnsureValid_InvalidMobilePhone_ShouldRequireTenDigits(string phone)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid([Actor(phone: phone)]));

        var error = Assert.Single(exception.Errors);
        Assert.Contains("Número de contacto", error.FieldLabel);
        Assert.Contains("10 dígitos", error.Message);
    }

    [Theory]
    [InlineData("juan.gmail.com")]
    [InlineData("juan@gmail")]
    [InlineData("juan@@gmail.com")]
    public void EnsureValid_InvalidEmail_ShouldExplainTheExpectedFormat(string email)
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid([Actor(email: email)]));

        var error = Assert.Single(exception.Errors);
        Assert.Contains("Correo electrónico", error.FieldLabel);
        Assert.Contains("usuario@dominio.com", error.Message);
    }

    [Fact]
    public void EnsureValid_EmptyContactData_ShouldBeAllowed()
    {
        GovernanceRequestValidation.EnsureValid([Actor(phone: null, email: null)]);
    }

    [Fact]
    public void EnsureValid_NegativeCopAmount_ShouldBeRejected()
    {
        var axis = new GovernancePnmcAxisDto(
            null, null, null, null, null, null, null, null, null, null, null, null,
            -1000m, [], null, null, null, null);

        var exception = Assert.Throws<DomainValidationException>(() =>
            GovernanceRequestValidation.EnsureValid([axis]));

        var error = Assert.Single(exception.Errors);
        Assert.Contains("Valor de la propuesta (COP)", error.FieldLabel);
    }
}
