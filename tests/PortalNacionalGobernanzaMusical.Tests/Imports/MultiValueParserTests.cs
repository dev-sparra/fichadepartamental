using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Las celdas de selección múltiple se unen con ", " pero varias opciones del catálogo contienen
/// ese mismo separador. El parseo debe reconocer la opción completa y no partirla, para no
/// reportar como inválido un archivo bien diligenciado.
/// </summary>
public sealed class MultiValueParserTests
{
    /// <summary>Roles del ecosistema del tipo de agente "Sectorial" (uno de ellos lleva coma).</summary>
    private static readonly string[] SectorialRoles =
    [
        "Creadores(as) y compositores(as)",
        "Intérpretes y agrupaciones",
        "Entidades de educación superior, formación técnica y tecnológica",
        "Cámaras de comercio y redes de apoyo",
        "Organizaciones de carácter público-privado"
    ];

    [Fact]
    public void Split_RoleContainingTheSeparator_ShouldNotBeBrokenApart()
    {
        var raw = "Entidades de educación superior, formación técnica y tecnológica";

        var tokens = MultiValueParser.Split(raw, SectorialRoles);

        Assert.Equal([raw], tokens);
    }

    [Fact]
    public void Split_SeveralRolesIncludingOneWithComma_ShouldReturnEachCompleteRole()
    {
        var raw = "Intérpretes y agrupaciones, Entidades de educación superior, formación técnica y tecnológica, Cámaras de comercio y redes de apoyo";

        var tokens = MultiValueParser.Split(raw, SectorialRoles);

        Assert.Equal(
        [
            "Intérpretes y agrupaciones",
            "Entidades de educación superior, formación técnica y tecnológica",
            "Cámaras de comercio y redes de apoyo"
        ], tokens);
    }

    [Fact]
    public void Split_ValuesWithoutCommas_ShouldBehaveLikeASimpleSplit()
    {
        var tokens = MultiValueParser.Split("Municipal, Departamental, Nacional", ["Municipal", "Departamental", "Nacional"]);

        Assert.Equal(["Municipal", "Departamental", "Nacional"], tokens);
    }

    [Fact]
    public void Split_UnknownValue_ShouldBeReturnedSoValidationCanReportIt()
    {
        var tokens = MultiValueParser.Split("Intérpretes y agrupaciones, Rol inventado", SectorialRoles);

        Assert.Equal(["Intérpretes y agrupaciones", "Rol inventado"], tokens);
    }

    [Fact]
    public void Split_ShouldIgnoreCaseAndSurroundingSpaces()
    {
        var tokens = MultiValueParser.Split("  intérpretes y agrupaciones  ", SectorialRoles);

        Assert.Equal(["Intérpretes y agrupaciones"], tokens);
    }

    [Fact]
    public void Split_WithoutCatalog_ShouldFallBackToTheSeparator()
    {
        var tokens = MultiValueParser.Split("Uno, Dos", null);

        Assert.Equal(["Uno", "Dos"], tokens);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Split_EmptyCell_ShouldReturnNothing(string? raw)
    {
        Assert.Empty(MultiValueParser.Split(raw, SectorialRoles));
    }

    [Fact]
    public void Split_ShouldPreferTheLongestMatchingOption()
    {
        string[] catalog = ["Alcaldías", "Alcaldías Locales"];

        var tokens = MultiValueParser.Split("Alcaldías Locales, Alcaldías", catalog);

        Assert.Equal(["Alcaldías Locales", "Alcaldías"], tokens);
    }
}
