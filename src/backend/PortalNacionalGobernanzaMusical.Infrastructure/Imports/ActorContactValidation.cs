namespace PortalNacionalGobernanzaMusical.Infrastructure.Imports;

/// <summary>
/// Réplica de las validaciones de contacto del Actor definidas en el Excel:
/// <list type="bullet">
/// <item>Número de contacto: <c>longitud de texto entre min y max</c> (Actores!F, 7–20).</item>
/// <item>Correo electrónico: <c>AND(ISNUMBER(SEARCH("@",G)),ISNUMBER(SEARCH(".",G)),LEN(G)&gt;=min)</c> (Actores!G, min 5).</item>
/// </list>
/// Los límites provienen del Blueprint; aquí solo vive la lógica pura (fácil de probar).
/// Un valor vacío no incumple (ambos campos son opcionales).
/// </summary>
public static class ActorContactValidation
{
    public static bool IsPhoneLengthValid(string? phone, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return true;
        }

        var length = phone.Trim().Length;
        return length >= minLength && length <= maxLength;
    }

    public static bool IsEmailValid(string? email, int minLength)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var trimmed = email.Trim();
        return trimmed.Contains('@') && trimmed.Contains('.') && trimmed.Length >= minLength;
    }
}
