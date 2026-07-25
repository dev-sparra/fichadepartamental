namespace PortalNacionalGobernanzaMusical.Application.Common;

/// <summary>Error de captura de un campo, ya redactado para el usuario.</summary>
public sealed record FieldValidationError(string Field, string FieldLabel, string Message);

/// <summary>
/// Excepción de validación de negocio con mensajes ya redactados en lenguaje funcional. El
/// manejador de excepciones de la API la traduce a un 400 con el detalle por campo, de modo que el
/// usuario nunca vea trazas ni textos técnicos.
/// </summary>
public sealed class DomainValidationException(string summary, IReadOnlyCollection<FieldValidationError> errors)
    : Exception(summary)
{
    public string Summary { get; } = summary;

    public IReadOnlyCollection<FieldValidationError> Errors { get; } = errors;

    public static void ThrowIfAny(string summary, IReadOnlyCollection<FieldValidationError> errors)
    {
        if (errors.Count > 0)
        {
            throw new DomainValidationException(summary, errors);
        }
    }
}
