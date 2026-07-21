namespace PortalNacionalGobernanzaMusical.Application.Catalogs;

public interface ICatalogQueryService
{
    Task<IReadOnlyCollection<DepartmentCatalogOptionDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<MunicipalityCatalogOptionDto>> GetMunicipalitiesAsync(int departmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CatalogOptionDto>> GetLookupAsync(string lookupName, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CatalogOptionDto>> GetPnmcComponentsAsync(int pnmcAxisId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CatalogOptionDto>> GetEcosystemRolesAsync(int agentTypeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IndicatorDefinitionCatalogDto>> GetIndicatorDefinitionsAsync(CancellationToken cancellationToken = default);
}
