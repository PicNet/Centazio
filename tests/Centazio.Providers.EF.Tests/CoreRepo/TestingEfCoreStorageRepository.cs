﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Test.Lib;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests.CoreRepo;

public class TestingEfCoreStorageRepository(Func<CentazioDbContext> getdb, IDbFieldsHelper dbf) : AbstractCoreStorageEfRepository(getdb), ITestingCoreStorage {
  
  private static string CoreSchemaName => "dbo";
  private static string CtlSchemaName => nameof(Core.Ctl);
  private static string CoreEntityName => nameof(CoreEntity).ToLower();
  private static string CoreStorageMetaName => nameof(CoreStorageMeta).ToLower();

  public async Task<ITestingCoreStorage> Initalise() {
    await using var db = Db();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CtlSchemaName, CoreStorageMetaName, dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(CoreSchemaName, CoreEntityName, dbf.GetDbFields<CoreEntity>(), [nameof(CoreEntity.CoreId)]));
    return this;
  }

  public override async ValueTask DisposeAsync() {
    await using var db = Db();
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CoreSchemaName, CoreEntityName));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateDropTableScript(CtlSchemaName, CoreStorageMetaName));
    await base.DisposeAsync();
  }

  public async Task<List<CoreEntity>> GetAllCoreEntities() {
    await using var db = Db();
    return (await db.Set<CoreEntity.Dto>().ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }

  protected override async Task<List<ICoreEntity>> GetCoreEntitiesWithIds(CoreEntityTypeName coretype, List<CoreEntityId> coreids, CentazioDbContext db) {
    var strids = coreids.Select(id => id.Value).ToList();
    if (coretype == CoreEntityTypeName.From<CoreEntity>()) return  await Impl<CoreEntity, CoreEntity.Dto>();
    
    throw new NotSupportedException(coretype.Value);

    async Task<List<ICoreEntity>> Impl<E, D>() where E : ICoreEntity where D : class, ICoreEntityDto<E> => 
        (await db.Set<D>().Where(e => strids.Contains(e.CoreId)).ToListAsync()).Select(e => e.ToBase() as ICoreEntity).ToList();
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