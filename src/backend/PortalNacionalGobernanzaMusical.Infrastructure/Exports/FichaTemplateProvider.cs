using System.Reflection;

namespace PortalNacionalGobernanzaMusical.Infrastructure.Exports;

/// <summary>Provee copias frescas y editables de la plantilla oficial <c>.xlsm</c>.</summary>
public interface IFichaTemplateProvider
{
    /// <summary>Devuelve un <see cref="MemoryStream"/> expandible con la plantilla oficial lista para editar.</summary>
    MemoryStream OpenWritableTemplate();

    /// <summary>Devuelve una copia de los bytes de la plantilla oficial en blanco (para descarga tal cual).</summary>
    byte[] GetTemplateBytes();
}

/// <summary>
/// Carga la plantilla oficial <c>ficha_departamental_gobernanza.xlsm</c> embebida como recurso
/// en el ensamblado. Al ser recurso embebido, el archivo viaja dentro del DLL y no depende de
/// rutas externas en el servidor (compatible con IIS/Plesk).
/// </summary>
public sealed class FichaTemplateProvider : IFichaTemplateProvider
{
    internal const string ResourceName =
        "PortalNacionalGobernanzaMusical.Infrastructure.Exports.Templates.ficha_departamental_gobernanza.xlsm";

    private static readonly byte[] TemplateBytes = LoadTemplateBytes();

    public MemoryStream OpenWritableTemplate()
    {
        var stream = new MemoryStream(TemplateBytes.Length);
        stream.Write(TemplateBytes, 0, TemplateBytes.Length);
        stream.Position = 0;
        return stream;
    }

    public byte[] GetTemplateBytes() => (byte[])TemplateBytes.Clone();

    private static byte[] LoadTemplateBytes()
    {
        var assembly = typeof(FichaTemplateProvider).Assembly;
        using var resource = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"No se encontró el recurso embebido '{ResourceName}'. Verifica el <EmbeddedResource> en el .csproj.");

        using var buffer = new MemoryStream();
        resource.CopyTo(buffer);
        return buffer.ToArray();
    }
}
