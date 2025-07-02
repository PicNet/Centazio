using Centazio.Test.Lib.InMemRepos;
using Serilog;

namespace Centazio.Test.Lib.E2E;

public class InMemorySimulationCoreStorageRepository(IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum) : ISimulationCoreStorageRepository {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = [];
  public Task<IDbTransactionWrapper> BeginTransaction(IDbTransactionWrapper? reuse = null) => 
      Task.FromResult<IDbTransactionWrapper>(new EmptyTransactionWrapper());

  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetEntitiesToWrite<CoreMembershipType>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetEntitiesToWrite<CoreCustomer>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetEntitiesToWrite<CoreInvoice>(exclude, after)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetExistingEntities<CoreMembershipType>(coreids)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetExistingEntities<CoreCustomer>(coreids)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetExistingEntities<CoreInvoice>(coreids)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => (await GetExistingEntities(coretype, coreids)).ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));
  
  public async Task<List<CoreMembershipType>> GetMembershipTypes() => await GetAll<CoreMembershipType>();
  public async Task<List<CoreCustomer>> GetCustomers() => await GetAll<CoreCustomer>();
  public async Task<List<CoreInvoice>> GetInvoices() => await GetAll<CoreInvoice>();
  
  public async Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid) => await GetSingle<CoreMembershipType>(coreid);

  public Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = [];
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.CoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.CoreEntity.CoreId)) { tracker.Update(e); } 
      else { tracker.Add(e); }
      
      target[e.CoreEntity.CoreId] = Json.Serialize(e);
      return e;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.GetShortDisplayName()}")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }


  private Task<E> GetSingle<E>(CoreEntityId coreid) where E : CoreEntityBase {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (!dict.TryGetValue(coreid, out var json)) throw new Exception();
    return Task.FromResult(CoreEntityAndMeta.FromJson(json).As<E>());
  }
  
  private async Task<List<E>> GetAll<E>() where E : CoreEntityBase => 
      (await ListImpl<E>()).Select(e => (E) e.CoreEntity).ToList();

  private async Task<List<CoreEntityAndMeta>> GetExistingEntities<E>(List<CoreEntityId> coreids) where E : CoreEntityBase {
    var strids = coreids.Select(id => id.Value).ToList();
    return (await ListImpl<E>())
        .Where(d => strids.Contains(d.Meta.CoreId))
        .Select(d => new CoreEntityAndMeta(d.CoreEntity, d.Meta))
        .ToList();
  }

  private async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E>(SystemName exclude, DateTime after) where E : CoreEntityBase {
    return (await ListImpl<E>())
        .Where(d => d.Meta.DateUpdated > after && d.Meta.LastUpdateSystem != exclude.Value)
        .Select(d => new CoreEntityAndMeta(d.CoreEntity, d.Meta))
        .ToList();
  }

  private Task<List<CoreEntityAndMeta>> ListImpl<E>() where E : CoreEntityBase  {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = [];
    return Task.FromResult(db[coretype].Values.Select(Json.Deserialize<CoreEntityAndMeta>).ToList());
  }
}