namespace PortalNacionalGobernanzaMusical.Application.Imports;

/// <summary>
/// Separa el contenido de una celda de selección múltiple del archivo oficial.
/// <para>La macro del Excel une las opciones con <c>", "</c>, pero varias opciones del catálogo
/// contienen ese mismo separador dentro del nombre (por ejemplo
/// <i>"Entidades de educación superior, formación técnica y tecnológica"</i>). Separar a ciegas por
/// <c>", "</c> partía esos valores en pedazos que no existen en el catálogo y la importación los
/// reportaba como error aunque el archivo estuviera bien diligenciado.</para>
/// <para>Por eso el parseo se hace contra el catálogo: en cada posición se toma la opción válida
/// más larga que coincida y solo cuando ninguna coincide se corta por el separador.</para>
/// </summary>
public static class MultiValueParser
{
    public const string Separator = ", ";

    /// <summary>
    /// Separa <paramref name="rawValue"/> reconociendo los valores de <paramref name="knownValues"/>.
    /// Los fragmentos que no correspondan a ninguna opción se devuelven tal cual para que la
    /// validación los reporte como valor inválido.
    /// </summary>
    public static IReadOnlyList<string> Split(string? rawValue, IReadOnlyCollection<string>? knownValues)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        var remaining = rawValue.Trim();
        if (knownValues is null || knownValues.Count == 0)
        {
            return SplitBySeparator(remaining);
        }

        // Las opciones más largas se prueban primero: si una contiene a otra, gana la completa.
        var candidates = knownValues
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .OrderByDescending(value => value.Length)
            .ToArray();

        var tokens = new List<string>();

        while (remaining.Length > 0)
        {
            var match = candidates.FirstOrDefault(candidate => MatchesAt(remaining, candidate));

            if (match is not null)
            {
                tokens.Add(match);
                remaining = Advance(remaining, match.Length);
                continue;
            }

            var separatorIndex = remaining.IndexOf(Separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                tokens.Add(remaining.Trim());
                break;
            }

            tokens.Add(remaining[..separatorIndex].Trim());
            remaining = remaining[(separatorIndex + Separator.Length)..].TrimStart();
        }

        return [.. tokens.Where(token => !string.IsNullOrWhiteSpace(token))];
    }

    /// <summary>Separación simple por <c>", "</c>, para cuando no hay catálogo de referencia.</summary>
    public static IReadOnlyList<string> SplitBySeparator(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue)
            ? []
            : rawValue.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// La opción coincide en la posición actual solo si termina donde acaba el texto o justo antes
    /// de un separador: así "Alcaldías" no coincide dentro de "Alcaldías Locales".
    /// </summary>
    private static bool MatchesAt(string remaining, string candidate)
    {
        if (!remaining.StartsWith(candidate, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var rest = remaining[candidate.Length..];
        return rest.Length == 0 || rest.StartsWith(Separator, StringComparison.Ordinal) || rest.Trim().Length == 0;
    }

    private static string Advance(string remaining, int consumed)
    {
        var rest = remaining[consumed..];
        return rest.StartsWith(Separator, StringComparison.Ordinal)
            ? rest[Separator.Length..].TrimStart()
            : rest.Trim();
    }
}
