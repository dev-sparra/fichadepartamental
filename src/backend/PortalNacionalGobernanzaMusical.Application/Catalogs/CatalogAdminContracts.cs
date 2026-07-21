namespace PortalNacionalGobernanzaMusical.Application.Catalogs;

public sealed record CatalogDefinitionDto(string Key, string DisplayName, bool HasParent, string? ParentKey);

public sealed record CatalogItemDto(int Id, string Name, int DisplayOrder, bool IsActive, int? ParentId);

public sealed record UpsertCatalogItemRequest(string Name, int DisplayOrder, bool IsActive, int? ParentId);
