namespace PortalNacionalGobernanzaMusical.Domain.Common;

public abstract class CatalogEntityBase
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
