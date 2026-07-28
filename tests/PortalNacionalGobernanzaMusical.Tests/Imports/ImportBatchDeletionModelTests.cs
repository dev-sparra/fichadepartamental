using Microsoft.EntityFrameworkCore;
using PortalNacionalGobernanzaMusical.Domain.Entities;
using PortalNacionalGobernanzaMusical.Persistence;

namespace PortalNacionalGobernanzaMusical.Tests.Imports;

/// <summary>
/// Eliminar una carga del historial borra el lote y todo lo que cuelga de él. En la base de datos
/// cada tabla del lote tiene una llave foránea hacia <c>import_batches</c>, así que los DELETE
/// tienen que salir en orden: primero las filas, al final el lote.
///
/// EF Core solo respeta ese orden si el modelo declara la relación; cuando no la conoce ordena los
/// DELETE por nombre de tabla y el lote sale antes que sus filas. Eso rompía la eliminación de las
/// cargas sin incidencias registradas (una importación limpia), porque el lote quedaba de segundo
/// en ese orden alfabético.
/// </summary>
public sealed class ImportBatchDeletionModelTests
{
    /// <summary>Tablas de trabajo del lote, en el mismo orden en que se configuran.</summary>
    public static TheoryData<Type> StagingEntities =>
    [
        typeof(ImportIdentificationStagingRow),
        typeof(ImportDiagnosticStagingRow),
        typeof(ImportOpportunityStagingRow),
        typeof(ImportPnmcAxisStagingRow),
        typeof(ImportActorStagingRow),
        typeof(ImportIndicatorStagingRow),
        typeof(ImportIndicatorDetailStagingRow)
    ];

    private static ApplicationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Theory]
    [MemberData(nameof(StagingEntities))]
    public void CadaTablaDelLote_DebeDeclararSuRelacionConElLote(Type stagingEntity)
    {
        using var context = NewContext();

        var entityType = context.Model.FindEntityType(stagingEntity);
        Assert.NotNull(entityType);

        var foreignKey = entityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(ImportBatch));

        Assert.True(
            foreignKey is not null,
            $"{stagingEntity.Name} no declara su relación con ImportBatch: al eliminar una carga, " +
            "EF Core ordenaría el DELETE del lote antes que el de sus filas y la base de datos lo rechazaría.");

        Assert.Equal("ImportBatchId", Assert.Single(foreignKey.Properties).Name);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void LasIncidencias_DebenDeclararSuRelacionConElLote()
    {
        using var context = NewContext();

        var entityType = context.Model.FindEntityType(typeof(ImportValidationIssue));
        Assert.NotNull(entityType);

        var foreignKey = Assert.Single(
            entityType.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(ImportBatch));

        Assert.Equal("ImportBatchId", Assert.Single(foreignKey.Properties).Name);
    }
}
