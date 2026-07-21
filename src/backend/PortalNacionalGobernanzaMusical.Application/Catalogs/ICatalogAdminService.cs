namespace PortalNacionalGobernanzaMusical.Application.Catalogs;

public interface ICatalogAdminService
{
    IReadOnlyCollection<CatalogDefinitionDto> GetCatalogDefinitions();

    Task<IReadOnlyCollection<CatalogItemDto>> GetItemsAsync(string catalogKey, int? parentId, CancellationToken cancellationToken = default);

    Task<CatalogItemDto> CreateAsync(string catalogKey, UpsertCatalogItemRequest request, CancellationToken cancellationToken = default);

    Task<CatalogItemDto> UpdateAsync(string catalogKey, int id, UpsertCatalogItemRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(string catalogKey, int id, CancellationToken cancellationToken = default);
}
