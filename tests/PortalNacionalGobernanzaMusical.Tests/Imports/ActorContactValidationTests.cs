using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Verifica que las validaciones de contacto del import repliquen exactamente las del Excel
/// (número 7–20, correo con @ y . y longitud ≥ 5) y que los límites vengan del Blueprint.
/// </summary>
public sealed class ActorContactValidationTests
{
    [Theory]
    [InlineData("1234567", true)]                 // 7 = mínimo
    [InlineData("3001234567", true)]              // 10 caracteres, válido
    [InlineData("12345678901234567890", true)]    // 20 = máximo
    [InlineData("123456", false)]                 // 6 < mínimo
    [InlineData("123456789012345678901", false)]  // 21 > máximo
    [InlineData("", true)]                         // opcional (vacío no incumple)
    [InlineData(null, true)]
    public void IsPhoneLengthValid_ShouldMatchExcelRange(string? phone, bool expected)
    {
        Assert.Equal(expected, ActorContactValidation.IsPhoneLengthValid(phone, 7, 20));
    }

    [Theory]
    [InlineData("juan@casa.gov.co", true)]
    [InlineData("a@b.c", true)]      // longitud 5 = mínimo
    [InlineData("@.co", false)]      // tiene @ y . pero longitud 4 < 5 (regla LEN>=5)
    [InlineData("a@bc", false)]      // sin punto
    [InlineData("abc.co", false)]    // sin arroba
    [InlineData("", true)]           // opcional
    [InlineData(null, true)]
    public void IsEmailValid_ShouldMatchExcelCustomRule(string? email, bool expected)
    {
        Assert.Equal(expected, ActorContactValidation.IsEmailValid(email, 5));
    }

    [Fact]
    public void Blueprint_ShouldProvideActorContactBounds()
    {
        var actores = new FichaBlueprintProvider().GetBlueprint().Sheets.First(s => s.Key == "actores");
        var phone = actores.Fields.First(f => f.Key == "numeroContacto").Validation!;
        var email = actores.Fields.First(f => f.Key == "correoElectronico").Validation!;

        Assert.Equal(7, phone.MinLength);
        Assert.Equal(20, phone.MaxLength);
        Assert.Equal(5, email.MinLength);
    }
}
