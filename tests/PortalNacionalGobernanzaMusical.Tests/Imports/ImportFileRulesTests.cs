using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Solo debe admitirse el archivo oficial <c>ficha_departamental_gobernanza.xlsm</c>: cualquier
/// otra extensión o nombre se rechaza antes de leer el contenido, con un mensaje funcional.
/// </summary>
public sealed class ImportFileRulesTests
{
    [Theory]
    [InlineData("ficha_departamental_gobernanza.xlsm")]
    [InlineData("Ficha_Departamental_Gobernanza.XLSM")]
    [InlineData("ficha_departamental_gobernanza (1).xlsm")]
    [InlineData("ficha_departamental_gobernanza_antioquia.xlsm")]
    [InlineData("ficha-departamental-gobernanza.xlsm")]
    public void Validate_ShouldAcceptOfficialWorkbookNames(string fileName)
    {
        Assert.Empty(ImportFileRules.Validate(fileName, 512_000));
    }

    [Theory]
    [InlineData("ficha_departamental_gobernanza.xlsx", ImportIssueCodes.FileExtensionInvalid)]
    [InlineData("ficha_departamental_gobernanza.csv", ImportIssueCodes.FileExtensionInvalid)]
    [InlineData("datos.pdf", ImportIssueCodes.FileExtensionInvalid)]
    [InlineData("consolidado_indicadores.xlsm", ImportIssueCodes.FileNameInvalid)]
    [InlineData("ficha.xlsm", ImportIssueCodes.FileNameInvalid)]
    public void Validate_ShouldRejectFilesThatAreNotTheOfficialWorkbook(string fileName, string expectedCode)
    {
        var rejections = ImportFileRules.Validate(fileName, 512_000);

        Assert.Contains(rejections, rejection => rejection.Code == expectedCode);
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
