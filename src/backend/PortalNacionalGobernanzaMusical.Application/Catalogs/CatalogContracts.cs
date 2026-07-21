namespace PortalNacionalGobernanzaMusical.Application.Catalogs;

public record CatalogOptionDto(int Id, string Name, int DisplayOrder);

public sealed record DepartmentCatalogOptionDto(int Id, string Name, int DisplayOrder) : CatalogOptionDto(Id, Name, DisplayOrder);

public sealed record MunicipalityCatalogOptionDto(int Id, int DepartmentId, string Name, int DisplayOrder);

public sealed record IndicatorDefinitionCatalogDto(int Id, string ActionName, string IndicatorName, decimal TargetValue, int DisplayOrder);
