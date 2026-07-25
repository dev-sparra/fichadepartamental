using PortalNacionalGobernanzaMusical.Application.Governance.Blueprint;
using PortalNacionalGobernanzaMusical.Application.Imports;
using PortalNacionalGobernanzaMusical.Infrastructure.Imports;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Las incidencias que ve el usuario deben decir hoja, fila, columna, nombre del campo (el de la
/// Ficha Departamental), valor recibido, valor esperado y cómo corregirlo, sin texto técnico.
/// </summary>
public sealed class ImportIssueNarratorTests
{
    private static readonly ImportIssueNarrator Narrator =
        new(new BlueprintFieldLocator(new FichaBlueprintProvider()));

    private static ImportIssueSource Issue(
        string sheet,
        string? cell,
        int? row,
        string code,
        string message = "mensaje almacenado",
        string? rawValue = null,
        string? contextJson = null)
    {
        return new ImportIssueSource(Guid.NewGuid(), ImportIssueCodes.SeverityError, sheet, row, cell, code, message, rawValue, contextJson);
    }

    [Fact]
    public void Narrate_InvalidEmail_ShouldExplainTheExpectedFormat()
    {
        var issue = Issue("Actores", "G18", 18, ImportIssueCodes.ActorEmailInvalid, rawValue: "juan.gmail.com");

        var result = Narrator.Narrate(issue);

        Assert.Equal("Fila 18 · Campo \"Correo electrónico\"", result.Title);
        Assert.Equal("Correo electrónico", result.FieldLabel);
        Assert.Equal("G", result.ColumnLetter);
        Assert.Equal("juan.gmail.com", result.RawValue);
        Assert.Contains("no corresponde a un correo electrónico válido", result.Message);
        Assert.Contains("usuario@dominio.com", result.ExpectedValue);
        Assert.Contains("G18", result.HowToFix);
        Assert.Equal("Debe corregirse", result.SeverityLabel);
    }

    [Fact]
    public void Narrate_ListValue_ShouldUseTheFieldLabelFromTheBlueprint()
    {
        var issue = Issue("Ejes PNMC", "P7", 7, ImportIssueCodes.AxisScheduleInvalid, rawValue: "Primer trimestre 2026");

        var result = Narrator.Narrate(issue);

        Assert.Equal("Cronograma", result.FieldLabel);
        Assert.Contains("\"Cronograma\"", result.Message);
        Assert.Contains("lista desplegable", result.HowToFix);
    }

    [Fact]
    public void Narrate_InlineListValue_ShouldListTheAllowedOptions()
    {
        var issue = Issue("Diagnóstico ecosistema", "I2", 2, ImportIssueCodes.DiagCouncilInvalid, rawValue: "Sí");

        var result = Narrator.Narrate(issue);

        Assert.Equal("Consejo Departamental de Cultura", result.FieldLabel);
        Assert.Contains("Existe", result.ExpectedValue);
        Assert.Contains("Por renovar", result.ExpectedValue);
    }

    [Fact]
    public void Narrate_MonthlyIndicatorColumn_ShouldNameTheMonthAndTheMeasure()
    {
        var issue = Issue("Indicadores", "G3", 3, "INDICATORS_VALUE_INVALID", rawValue: "abc");

        var result = Narrator.Narrate(issue);

        Assert.Equal("Febrero · Avance cuantitativo", result.FieldLabel);
    }

    [Fact]
    public void Narrate_UnexpectedException_ShouldHideTheTechnicalDetailFromTheMessage()
    {
        var context = new ImportIssueContext { TechnicalDetail = "InvalidOperationException: Sequence contains no elements" };
        var issue = Issue("Archivo", null, null, ImportIssueCodes.ImportException, contextJson: context.ToJson());

        var result = Narrator.Narrate(issue);

        Assert.Equal("Hoja \"Archivo\"", result.Title);
        Assert.DoesNotContain("Exception", result.Message);
        Assert.Contains("No fue posible procesar el archivo", result.Message);
        Assert.Contains("Sequence contains no elements", result.TechnicalDetail);
    }

    [Fact]
    public void Narrate_FileRejection_ShouldKeepTheStoredFunctionalTextAndContext()
    {
        var context = new ImportIssueContext
        {
            Expected = "Una hoja llamada exactamente \"Actores\".",
            HowToFix = "Utilice el archivo oficial sin eliminar hojas."
        };
        var issue = Issue(
            "Actores",
            null,
            null,
            ImportIssueCodes.SheetMissing,
            message: "El archivo no contiene la hoja \"Actores\", que es obligatoria en la Ficha Departamental de Gobernanza.",
            contextJson: context.ToJson());

        var result = Narrator.Narrate(issue);

        Assert.Contains("no contiene la hoja", result.Message);
        Assert.Equal(context.Expected, result.ExpectedValue);
        Assert.Equal(context.HowToFix, result.HowToFix);
    }

    [Fact]
    public void Narrate_PhoneFormat_ShouldAskForTenDigits()
    {
        var issue = Issue("Actores", "F4", 4, ImportIssueCodes.ActorPhoneFormat, rawValue: "300 123");

        var result = Narrator.Narrate(issue);

        Assert.Equal("Número de contacto", result.FieldLabel);
        Assert.Contains("10 dígitos", result.Message);
        Assert.Contains("3001234567", result.ExpectedValue);
    }
}
