using System.Collections;
using System.Globalization;

namespace PortalNacionalGobernanzaMusical.Application.Audit;

/// <summary>
/// Arma la lista de campos que cambiaron en una acción, con el valor anterior y el nuevo ya
/// formateados para leer. Solo se queda con lo que realmente cambió: así el historial muestra
/// "cambió el correo y el estado" en vez de volcar el registro completo.
/// </summary>
public sealed class AuditChangeSet
{
    /// <summary>
    /// Tope de longitud de un valor en el historial. Los campos de texto largo de la ficha pueden
    /// tener párrafos enteros; el detalle completo queda en los valores anterior/nuevo en JSON.
    /// </summary>
    private const int MaxValueLength = 600;

    private const string EmptyValue = "(vacío)";

    private readonly List<AuditChangeDto> changes = [];

    public bool HasChanges => changes.Count > 0;

    public IReadOnlyCollection<AuditChangeDto> Changes => changes;

    /// <summary>Registra un campo; si el valor no cambió, no se agrega nada.</summary>
    public AuditChangeSet Track(string field, string label, object? before, object? after)
    {
        var formattedBefore = Format(before);
        var formattedAfter = Format(after);

        if (string.Equals(formattedBefore, formattedAfter, StringComparison.Ordinal))
        {
            return this;
        }

        changes.Add(new AuditChangeDto(field, label, formattedBefore, formattedAfter));
        return this;
    }

    /// <summary>
    /// Registra un campo cuyo valor no se puede mostrar (una contraseña, por ejemplo). Deja
    /// constancia de que cambió sin exponer el contenido.
    /// </summary>
    public AuditChangeSet TrackSecret(string field, string label)
    {
        changes.Add(new AuditChangeDto(field, label, "(oculto)", "(oculto)"));
        return this;
    }

    /// <summary>Enumera en palabras los campos que cambiaron, para la descripción de la acción.</summary>
    public string DescribeChangedFields()
    {
        var labels = changes.Select(change => change.Label).ToArray();

        return labels.Length switch
        {
            0 => string.Empty,
            1 => labels[0],
            _ => $"{string.Join(", ", labels[..^1])} y {labels[^1]}"
        };
    }

    /// <summary>Convierte un valor a la forma en que se muestra en el historial.</summary>
    public static string Format(object? value)
    {
        var text = value switch
        {
            null => EmptyValue,
            string s => s.Trim(),
            bool b => b ? "Sí" : "No",
            DateOnly date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            DateTime dateTime => dateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
            decimal number => number.ToString("N2", CultureInfo.InvariantCulture),
            double number => number.ToString("N2", CultureInfo.InvariantCulture),
            IEnumerable list => string.Join(", ", list.Cast<object?>().Select(item => Format(item)).Where(item => item != EmptyValue)),
            _ => value.ToString()?.Trim() ?? EmptyValue
        };

        if (string.IsNullOrWhiteSpace(text))
        {
            return EmptyValue;
        }

        return text.Length > MaxValueLength ? $"{text[..MaxValueLength]}…" : text;
    }
}
