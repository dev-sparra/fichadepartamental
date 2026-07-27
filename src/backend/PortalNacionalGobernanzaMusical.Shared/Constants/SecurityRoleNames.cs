namespace PortalNacionalGobernanzaMusical.Shared.Constants;

/// <summary>
/// Nombres de rol tal como están en <c>security_roles</c> y, por tanto, tal como viajan en el
/// claim de rol del token. Son los que hay que comparar al decidir qué ve o qué puede hacer un
/// usuario.
/// <para>No confundir con <see cref="Roles"/>, cuyos valores sin espacios ni tildes se usan como
/// etiqueta del Blueprint para indicar qué rol diligencia cada hoja.</para>
/// </summary>
public static class SecurityRoleNames
{
    public const string Administrador = "Administrador";
    public const string LiderGobernanza = "Líder de Gobernanza";
    public const string GestorDepartamental = "Gestor Departamental";
}
