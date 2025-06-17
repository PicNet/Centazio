using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public class TestingEfCoreStorageRepository(Func<CentazioDbContext> getdb, IDbFieldsHelper dbf) : AbstractCoreStorageEfRepository(getdb), ITestingCoreStorage {
  
  private static string CoreSchemaName => "dbo";
  private static string CtlSchemaName => nameof(Core.Ctl).ToLower();
  private static string CoreEntityName => nameof(CoreEntity).ToLower();
  private static string CoreStorageMetaName => nameof(CoreStorageMeta).ToLower();

  public async Task<ITestingCoreStorage> Initalise() {
    return await UseDb(async db => {
      await db.ExecSql(dbf.GenerateCreateTableScript(CtlSchemaName, CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreId)]));
      await db.ExecSql(dbf.GenerateCreateTableScript(CoreSchemaName, CoreEntityName, dbf.GetDbFields<CoreEntity>(), [nameof(CoreEntity.CoreId)]));
      return this;
    });
  }

  public override async ValueTask DisposeAsync() {
    await UseDb(async db => {
      await db.ExecSql(dbf.GenerateDropTableScript(CoreSchemaName, CoreEntityName));
      await db.ExecSql(dbf.GenerateDropTableScript(CtlSchemaName, CoreStorageMetaName));
      await base.DisposeAsync();
      return Task.CompletedTask;
    });
  }

  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    return await UseDb(async db => 
        (await db.Set<CoreEntity.Dto>().ToListAsync()).Select(dto => dto.ToBase()).ToList());
  }

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    if (coretype == CoreEntityTypeName.From<CoreEntity>()) return await Impl<CoreEntity, CoreEntity.Dto>();
    
    throw new NotSupportedException(coretype.Value);

    async Task<List<ICoreEntity>> Impl<E, D>() where E : ICoreEntity where D : class, ICoreEntityDto<E> => 
        await UseDb(async db => (await db.Set<D>().Where(e => strids.Contains(e.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList());
  }

  public static void CreateTestingCoreStorageEfModel(ModelBuilder builder) => builder
      .HasDefaultSchema(CoreSchemaName)
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(CoreStorageMetaName, CtlSchemaName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreEntity.Dto>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      });
}