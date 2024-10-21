using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Serilog;

namespace Centazio.E2E.Tests.Infra;

public class CoreStorage(SimulationCtx ctx) : ICoreStorage {
  
  private readonly Dictionary<CoreEntityTypeName, Dictionary<CoreEntityId, string>> db = new();
  
  public CoreMembershipType? GetMembershipType(CoreEntityId? coreid) => GetSingle<CoreMembershipType, CoreMembershipType.Dto>(coreid);
  public List<CoreMembershipType> GetMembershipTypes() => GetList<CoreMembershipType, CoreMembershipType.Dto>();
  public CoreCustomer? GetCustomer(CoreEntityId? coreid) => GetSingle<CoreCustomer, CoreCustomer.Dto>(coreid);
  public List<CoreCustomer> GetCustomers() => GetList<CoreCustomer, CoreCustomer.Dto>();
  public CoreInvoice? GetInvoice(CoreEntityId? coreid) => GetSingle<CoreInvoice, CoreInvoice.Dto>(coreid);
  public List<CoreInvoice> GetInvoices() => GetList<CoreInvoice, CoreInvoice.Dto>();

  public Task<List<ICoreEntity>> Get(SystemName exclude, CoreEntityTypeName coretype, DateTime after) {
    var list = GetList(coretype).Where(e => e.LastUpdateSystem != exclude.Value && e.DateUpdated > after).ToList();
    return Task.FromResult(list);
  }

  public Task<List<ICoreEntity>> Get(CoreEntityTypeName coretype, List<CoreEntityId> coreids) {
    var full = GetList(coretype);
    var forcores = coreids.Select(id => full.Single(e => e.CoreId == id)).ToList();
    return Task.FromResult(forcores);
  }

  public async Task<Dictionary<CoreEntityId, CoreEntityChecksum>> GetChecksums(CoreEntityTypeName coretype, List<CoreEntityId> coreids) => 
      (await Get(new("ignore"), coretype, DateTime.MinValue))
      .Where(e => coreids.Contains(e.CoreId))
      .ToDictionary(e => e.CoreId, e => ctx.ChecksumAlg.Checksum(e));

  public Task<List<ICoreEntity>> Upsert(CoreEntityTypeName coretype, List<(ICoreEntity UpdatedCoreEntity, CoreEntityChecksum UpdatedCoreEntityChecksum)> entities) {
    var target = db[coretype];
    var updated = entities.Count(e => target.ContainsKey(e.UpdatedCoreEntity.CoreId));
    var upserted = entities.Select(e => {
      if (target.ContainsKey(e.UpdatedCoreEntity.CoreId)) { ctx.Epoch.Update(e.UpdatedCoreEntity); } 
      else { ctx.Epoch.Add(e.UpdatedCoreEntity); }
      
      target[e.UpdatedCoreEntity.CoreId] = Json.Serialize(e.UpdatedCoreEntity);
      return e.UpdatedCoreEntity;
    }).ToList();
    
    Log.Debug($"CoreStorage.Upsert[{coretype}] - Entities({entities.Count})[" + String.Join(",", entities.Select(e => $"{e.UpdatedCoreEntity.DisplayName}({e.UpdatedCoreEntity.CoreId})")) + $"] Created[{entities.Count - updated}] Updated[{updated}]");
    return Task.FromResult(upserted);
  }

  public ValueTask DisposeAsync() => ValueTask.CompletedTask;

  private List<ICoreEntity> GetList(CoreEntityTypeName coretype) {
    if (coretype.Value == nameof(CoreMembershipType)) return GetList<CoreMembershipType, CoreMembershipType.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreCustomer)) return GetList<CoreCustomer, CoreCustomer.Dto>().Cast<ICoreEntity>().ToList();
    if (coretype.Value == nameof(CoreInvoice)) return GetList<CoreInvoice, CoreInvoice.Dto>().Cast<ICoreEntity>().ToList();
    throw new NotSupportedException(coretype);
  }
  
  private List<E> GetList<E, D>() 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var coretype = CoreEntityTypeName.From<E>();
    if (!db.ContainsKey(coretype)) db[coretype] = new();
    return db[CoreEntityTypeName.From<E>()].Keys.Select(coreid => GetSingle<E, D>(coreid) ?? throw new Exception()).ToList();
  }

  private E? GetSingle<E, D>(CoreEntityId? coreid) 
      where E : CoreEntityBase 
      where D : CoreEntityBase.Dto<E> {
    var dict = db[CoreEntityTypeName.From<E>()];
    if (coreid is null || !dict.TryGetValue(coreid, out var json)) return default;

    return (Json.Deserialize<D>(json) ?? throw new Exception()).ToCoreEntity();
  }
}