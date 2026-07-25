using PortalNacionalGobernanzaMusical.Application.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Los estados que ve el usuario deben ser funcionales (nunca "Failed" ni "CompletedWithErrors")
/// y siempre traer descripción y siguiente paso.
/// </summary>
public sealed class ImportStatusCatalogTests
{
    [Theory]
    [InlineData(ImportBatchStatuses.Validating, "Archivo en validación")]
    [InlineData(ImportBatchStatuses.Processing, "Procesando archivo")]
    [InlineData(ImportBatchStatuses.Completed, "Importación exitosa")]
    [InlineData(ImportBatchStatuses.CompletedWithWarnings, "Importación completada con observaciones")]
    [InlineData(ImportBatchStatuses.CompletedWithErrors, "Importación completada con observaciones")]
    [InlineData(ImportBatchStatuses.Rejected, "Importación rechazada")]
    [InlineData(ImportBatchStatuses.Failed, "Importación rechazada")]
    public void Resolve_ShouldTranslateInternalCodesToFunctionalLabels(string status, string expectedLabel)
    {
        var presentation = ImportStatusCatalog.Resolve(status);

        Assert.Equal(expectedLabel, presentation.Label);
        Assert.False(string.IsNullOrWhiteSpace(presentation.Description));
        Assert.False(string.IsNullOrWhiteSpace(presentation.NextStep));
    }

    [Fact]
    public void Resolve_UnknownStatus_ShouldStillReturnSomethingUnderstandable()
    {
        var presentation = ImportStatusCatalog.Resolve("Whatever");

        Assert.Equal("Estado en revisión", presentation.Label);
        Assert.False(string.IsNullOrWhiteSpace(presentation.NextStep));
    }

    [Theory]
    [InlineData(true, true, ImportBatchStatuses.CompletedWithErrors)]
    [InlineData(true, false, ImportBatchStatuses.CompletedWithErrors)]
    [InlineData(false, true, ImportBatchStatuses.CompletedWithWarnings)]
    [InlineData(false, false, ImportBatchStatuses.Completed)]
    public void ResolveFinalStatus_ShouldDependOnErrorsAndWarnings(bool hasErrors, bool hasWarnings, string expected)
    {
        Assert.Equal(expected, ImportStatusCatalog.ResolveFinalStatus(hasErrors, hasWarnings));
    }
}
