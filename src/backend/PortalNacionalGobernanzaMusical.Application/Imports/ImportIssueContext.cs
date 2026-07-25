using System.Text.Json;

namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Datos adicionales de una incidencia que se guardan en <c>import_validation_issues.context_json</c>:
/// el valor esperado y la corrección cuando dependen del archivo cargado, y el detalle técnico
/// (excepciones) que solo se usa para soporte y jamás como mensaje al usuario.
/// </summary>
public sealed record ImportIssueContext
{
    public string? Expected { get; init; }
    public string? HowToFix { get; init; }
    public string? TechnicalDetail { get; init; }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Expected)
        && string.IsNullOrWhiteSpace(HowToFix)
        && string.IsNullOrWhiteSpace(TechnicalDetail);

    public string? ToJson()
    {
        return IsEmpty ? null : JsonSerializer.Serialize(this, SerializerOptions);
    }

    public static ImportIssueContext FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ImportIssueContext();
        }

        try
        {
            return JsonSerializer.Deserialize<ImportIssueContext>(json, SerializerOptions) ?? new ImportIssueContext();
        }
        catch (JsonException)
        {
            // El contexto es informativo: si viene corrupto la incidencia se sigue mostrando.
            return new ImportIssueContext();
        }
    }
}
