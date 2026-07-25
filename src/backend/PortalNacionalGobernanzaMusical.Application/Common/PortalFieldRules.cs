using System.Text.RegularExpressions;

namespace PortalNacionalGobernanzaMusical.Application.Common;

/// <summary>
/// Reglas de captura del portal por tipo de dato (correo, celular, número, porcentaje, moneda COP,
/// URL, texto). Son la contraparte en servidor de los validadores del formulario Angular: el mismo
/// dato se valida en el frontend para dar retroalimentación inmediata y aquí para garantizar la
/// integridad de la información, sin importar por dónde entre (web o importación de Excel).
/// </summary>
public static partial class PortalFieldRules
{
    /// <summary>Longitud exacta de un número de celular colombiano.</summary>
    public const int MobilePhoneDigits = 10;

    public const int ShortTextMaxLength = 200;
    public const int EmailMaxLength = 200;
    public const int LongTextMaxLength = 8000;

    /// <summary>Tope defensivo para valores en pesos (billón de pesos).</summary>
    public const decimal MaxCopAmount = 1_000_000_000_000m;

    public static readonly DateOnly MinCaptureDate = new(2000, 1, 1);
    public static readonly DateOnly MaxCaptureDate = new(2100, 12, 31);

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex DigitsPattern();

    /// <summary>Correo con formato usuario@dominio.com. Un valor vacío se considera válido.</summary>
    public static bool IsEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= EmailMaxLength && EmailPattern().IsMatch(trimmed);
    }

    /// <summary>Celular de exactamente 10 dígitos, sin espacios ni separadores.</summary>
    public static bool IsMobilePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        return trimmed.Length == MobilePhoneDigits && DigitsPattern().IsMatch(trimmed);
    }

    public static bool IsDigitsOnly(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || DigitsPattern().IsMatch(value.Trim());
    }

    /// <summary>Solo dígitos del valor recibido (útil para normalizar teléfonos).</summary>
    public static string DigitsOf(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    /// <summary>URL http/https bien formada. Un valor vacío se considera válido.</summary>
    public static bool IsUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>Porcentaje entre 0 y 100.</summary>
    public static bool IsPercentage(decimal? value)
    {
        return value is null || (value >= 0m && value <= 100m);
    }

    /// <summary>Valor en pesos: nunca negativo y dentro de un tope razonable.</summary>
    public static bool IsCopAmount(decimal? value)
    {
        return value is null || (value >= 0m && value <= MaxCopAmount);
    }

    public static bool IsWithinCaptureDateRange(DateOnly value)
    {
        return value >= MinCaptureDate && value <= MaxCaptureDate;
    }

    public static bool IsRequiredTextPresent(string? value)
    {
        return !string.IsNullOrWhiteSpace(value);
    }

    public static bool IsWithinLength(string? value, int maxLength)
    {
        return string.IsNullOrEmpty(value) || value.Trim().Length <= maxLength;
    }
}
