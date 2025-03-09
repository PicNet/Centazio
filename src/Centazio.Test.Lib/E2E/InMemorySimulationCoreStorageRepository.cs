using Serilog;

namespace Centazio.Test.Lib.E2E;

public class InMemorySimulationCoreStorageRepository(IEpochTracker tracker, Func<ICoreEntity, CoreEntityChecksum> checksum) : ISimulationCoreStorageRepository {
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetEntitiesToWrite<CoreMembershipType, CoreMembershipType.Dto>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetEntitiesToWrite<CoreCustomer, CoreCustomer.Dto>(exclude, after)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetEntitiesToWrite<CoreInvoice, CoreInvoice.Dto>(exclude, after)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<List<CoreEntityAndMeta>> GetExistingEntities(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    if (coretype.Value == nameof(CoreMembershipType)) return (await GetExistingEntities<CoreMembershipType, CoreMembershipType.Dto>(coreids)).ToList();
    if (coretype.Value == nameof(CoreCustomer)) return (await GetExistingEntities<CoreCustomer, CoreCustomer.Dto>(coreids)).ToList();
    if (coretype.Value == nameof(CoreInvoice)) return (await GetExistingEntities<CoreInvoice, CoreInvoice.Dto>(coreids)).ToList();
    throw new NotSupportedException(coretype);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => (await GetExistingEntities(coretype, coreids)).ToDictionary(e => e.CoreEntity.CoreId, e => checksum(e.CoreEntity));
  
  public async Task<List<CoreMembershipType>> GetMembershipTypes() => await GetAll<CoreMembershipType, CoreMembershipType.Dto>();
  public async Task<List<CoreCustomer>> GetCustomers() => await GetAll<CoreCustomer, CoreCustomer.Dto>();
  public async Task<List<CoreInvoice>> GetInvoices() => await GetAll<CoreInvoice, CoreInvoice.Dto>();
  
  public async Task<CoreMembershipType> GetMembershipType(CoreEntityId coreid) => await GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);

  public Task<List<CoreEntityAndMeta>> Upsert(CoreEntityTypeName coretype, List<CoreEntityAndMeta> entities) {
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.CoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.CoreEntity.CoreId)) { tracker.Update(e); } 
      else { tracker.Add(e); }
      
      target[e.CoreEntity.CoreId] = Json.Serialize(e.ToDtos());
      return e;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.CoreEntity.GetShortDisplayName()}")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() {
    db.Clear();
    return ValueTask.CompletedTask;
  }


  private Task<E> GetSingle<E, D>(CoreEntityId coreid) where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (!dict.TryGetValue(coreid, out var json)) throw new Exception();
    return Task.FromResult(CoreEntityAndMeta.FromJson<E, D>(json).As<E>());
  }
  
  private async Task<List<E>> GetAll<E, D>() where E : CoreEntityBase where D : class, ICoreEntityDto<E> => 
      (await ListImpl<E, D>()).Select(e => e.CoreEntityDto.ToBase()).ToList();

  private async Task<List<CoreEntityAndMeta>> GetExistingEntities<E, D>(List<CoreEntityId> coreids) where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    var strids = coreids.Select(id => id.Value).ToList();
    return (await ListImpl<E, D>())
        .Where(d => strids.Contains(d.MetaDto.CoreId))
        .Select(d => new CoreEntityAndMeta(d.CoreEntityDto.ToBase(), d.MetaDto.ToBase()))
        .ToList();
  }

  private async Task<List<CoreEntityAndMeta>> GetEntitiesToWrite<E, D>(SystemName exclude, DateTime after) where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    return (await ListImpl<E, D>())
        .Where(d => d.MetaDto.DateUpdated > after && d.MetaDto.LastUpdateSystem != exclude.Value)
        .Select(d => new CoreEntityAndMeta(d.CoreEntityDto.ToBase(), d.MetaDto.ToBase()))
        .ToList();
  }

  private Task<List<CoreEntityAndMetaDtos<D>>> ListImpl<E, D>() where E : CoreEntityBase where D : class, ICoreEntityDto<E> {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    return Task.FromResult(db[coretype].Values.Select(Json.Deserialize<CoreEntityAndMetaDtos<D>>).ToList());
  }
}