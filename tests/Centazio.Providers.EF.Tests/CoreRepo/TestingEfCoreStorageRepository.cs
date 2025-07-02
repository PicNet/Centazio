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
    return await UseDb(async db => (await db.Set<CoreEntity>().ToListAsync()).ToList());
  }

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var strids = coreids.Select(id => id.Value).ToList();
    if (coretype == CoreEntityTypeName.From<CoreEntity>()) return await Impl<CoreEntity>();
    
    throw new NotSupportedException(coretype.Value);

    // todo GT: without Dtos do we still need ICoreEntity as the List return type?  Why not generics
    async Task<List<ICoreEntity>> Impl<E>() where E : class, ICoreEntity => await UseDb(async db => (await db.Set<E>().Where(e => strids.Contains(e.CoreId)).ToListAsync()).ToList<ICoreEntity>());
  }

  public static void CreateTestingCoreStorageEfModel(ModelBuilder builder) => builder
      .HasDefaultSchema(CoreSchemaName)
      .Entity<CoreStorageMeta>(e => {
        e.ToTable(CoreStorageMetaName, CtlSchemaName);
        e.HasKey(e2 => e2.CoreId);
      })
      .Entity<CoreEntity>(e => {
        e.ToTable(CoreEntityName);
        e.HasKey(e2 => e2.CoreId);
      });
}