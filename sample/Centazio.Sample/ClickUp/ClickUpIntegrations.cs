using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Types;
using Centazio.Providers.EF;
using Centazio.Providers.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Centazio.Sample.ClickUp;

public class ClickUpIntegrations : IntegrationBase<SampleSettings, SampleSecrets> {

  // todo: dynamically load functions from dll and apply filter
  public override List<Type> GetAllFunctionTypes() => [typeof(ClickUpReadFunction), typeof(ClickUpPromoteFunction)];

  protected override void RegisterIntegrationSpecificServices(IServiceCollection svcs) {
    svcs.AddSingleton<ClickUpApi>();
    svcs.AddSingleton<ICoreStorage>(new SampleCoreStorage(
        () => new SampleDbContext(),
        new SqliteDbFieldsHelper(), 
        new Sha256ChecksumAlgorithm().Checksum));
  }

  public override async Task Initialise(ServiceProvider prov) {
    var core = (SampleCoreStorage) prov.GetRequiredService<ICoreStorage>();
    await core.Initialise();
  }

}

public class SampleDbContext() : SqliteDbContext("Data Source=sample_core_storage.db") {

  protected override void CreateCentazioModel(ModelBuilder builder) => builder
      .HasDefaultSchema("dbo")
      .Entity<CoreStorageMeta.Dto>(e => {
        e.ToTable(nameof(CoreStorageMeta).ToLower(), nameof(Core.Ctl).ToLower());
        e.HasKey(e2 => new { e2.CoreEntityTypeName, e2.CoreId });
      })
      .Entity<CoreTask.Dto>(e => {
        e.ToTable(nameof(CoreTask).ToLower());
        e.HasKey(e2 => e2.CoreId);
      });

}

// todo: extract all shareable code from here, SimulationEfCoreStorageRepository and AbstractSimulationCoreStorageRepository into a proper Ef base class or reuseable component
public class SampleCoreStorage(Func<CentazioDbContext> getdb,  IDbFieldsHelper dbf, Func<ICoreEntity, CoreEntityChecksum> checksum) : ICoreStorage {
  
  public async Task Initialise() {
    await using var db = getdb();
    
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript(nameof(Core.Ctl).ToLower(), nameof(CoreStorageMeta).ToLower(), dbf.GetDbFields<CoreStorageMeta>(), [nameof(CoreStorageMeta.CoreEntityTypeName), nameof(CoreStorageMeta.CoreId)]));
    await db.Database.ExecuteSqlRawAsync(dbf.GenerateCreateTableScript("dbo", nameof(CoreTask).ToLower(), dbf.GetDbFields<CoreTask>(), [nameof(ICoreEntity.CoreId)]));
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    await using var db = getdb();
    // todo: this metas query should perhaps be in shared base code in Providers.Ef CoreStorage base class?
    var metas = await db.Set<CoreStorageMeta.Dto>()
        .Where(m => m.CoreEntityTypeName == coretype && m.LastUpdateSystem != exclude.Value && m.DateUpdated > after)
        .ToListAsync(); 
    return await GetCoreEntitiesFromCorrespondingMetas(metas, db);
  }
  
  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    await using var db = getdb();
    var cids = coreids.Select(cid => cid.Value).ToList();
    // todo: this metas query should perhaps be in shared base code in Providers.Ef CoreStorage base class?
    var metas = await db.Set<CoreStorageMeta.Dto>()
        .Where(m => m.CoreEntityTypeName == coretype && cids.Contains(m.CoreId))
        .ToListAsync(); 
    return await GetCoreEntitiesFromCorrespondingMetas(metas, db);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var cems = await  GetExistingEntities(coretype, coreids);
    return cems.ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));
  }
  
  public async Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    var existing = (await GetExistingEntities(coretype, entities.Select(e => e.CoreEntity.CoreId).ToList())).ToDictionary(e => e.CoreEntity.CoreId);
    await using var db = getdb();
    
    entities.ForEach(t => {
      var state = existing.ContainsKey(t.CoreEntity.CoreId) ? EntityState.Modified : EntityState.Added;
      var dtos = t.ToDtos();
      db.Attach(dtos.CoreEntityDto).State = state;
      db.Attach(dtos.MetaDto).State = state;
    });
    await db.SaveChangesAsync();

    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")) + $"] Created[{entities.Count - existing.Count}] Updated[{existing.Count}]");
    return entities;
  }

  private async Task<List<CoreEntityAndMeta>> GetCoreEntitiesFromCorrespondingMetas(List<CoreStorageMeta.Dto> metas, CentazioDbContext db) {
    var cids = metas.Select(m => m.CoreId).ToList();
    var tasks = await db.Set<CoreTask.Dto>().Where(t => cids.Contains(t.CoreId)).ToListAsync();
    return metas.Select(meta => {
      var task = tasks.Single(t => t.CoreId == meta.CoreId);
      return new CoreEntityAndMeta(task.ToBase(), meta.ToBase());
    }).ToList();
  }

}

