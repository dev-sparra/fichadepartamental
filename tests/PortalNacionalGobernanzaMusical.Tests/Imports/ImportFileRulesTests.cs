using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Se admite cualquier archivo <c>.xlsm</c>: lo que importa es el formato y la estructura, no el
/// nombre (en territorio es común renombrar el archivo sin cambiar su contenido). Otras
/// extensiones se rechazan antes de leer el contenido, con un mensaje funcional.
/// </summary>
public sealed class ImportFileRulesTests
{
    [Theory]
    [InlineData("ficha_departamental_gobernanza.xlsm")]
    [InlineData("Ficha_Departamental_Gobernanza.XLSM")]
    [InlineData("ficha_departamental_gobernanza (1).xlsm")]
    [InlineData("ficha antioquia marzo 2026.xlsm")]
    [InlineData("FICHA GOBERNANZA VF final.xlsm")]
    [InlineData("copia de trabajo.xlsm")]
    public void Validate_ShouldAcceptAnyFileNameWithTheOfficialFormat(string fileName)
    {
        Assert.Empty(ImportFileRules.Validate(fileName, 512_000));
    }

    [Theory]
    [InlineData("ficha_departamental_gobernanza.xlsx")]
    [InlineData("ficha_departamental_gobernanza.csv")]
    [InlineData("datos.pdf")]
    [InlineData("ficha_sin_extension")]
    public void Validate_ShouldRejectFilesThatAreNotXlsm(string fileName)
    {
        var rejections = ImportFileRules.Validate(fileName, 512_000);

        Assert.Contains(rejections, rejection => rejection.Code == ImportIssueCodes.FileExtensionInvalid);
        Assert.All(rejections, rejection =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rejection.Message));
            Assert.False(string.IsNullOrWhiteSpace(rejection.HowToFix));
        });
    }

    [Fact]
    public void Validate_ShouldRejectEmptyFileWithoutCheckingAnythingElse()
    {
        var rejections = ImportFileRules.Validate(ImportFileRules.OfficialFileName, 0);

        Assert.Single(rejections);
        Assert.Equal(ImportIssueCodes.FileEmpty, rejections[0].Code);
    }

    [Fact]
    public void Validate_ShouldRejectFilesOverTheSizeLimit()
    {
        var rejections = ImportFileRules.Validate(
            ImportFileRules.OfficialFileName,
            ImportFileRules.MaxFileSizeBytes + 1);

        Assert.Contains(rejections, rejection => rejection.Code == ImportIssueCodes.FileTooLarge);
    }
}
