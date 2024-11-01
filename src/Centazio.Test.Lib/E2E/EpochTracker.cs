using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E.Crm;
using Centazio.Test.Lib.E2E.Fin;

namespace Centazio.Test.Lib.E2E;

public interface IEpochTracker {
  int Epoch { get; }
  
  void SetEpoch(int epoch);
  void Update(CoreEntityAndMeta coreent);
  void Add(CoreEntityAndMeta coreent);
}

[IgnoreNamingConventions] public class EpochTracker(SimulationCtx ctx) : IEpochTracker {
  public int Epoch { get; private set; }
  
  private Dictionary<(Type, string), CoreEntityAndMeta> added = new();
  private Dictionary<(Type, string), CoreEntityAndMeta> updated = new();
  
  public void SetEpoch(int epoch) => (Epoch, added, updated) = (epoch, new(), new());

  public async Task ValidateAdded<T>(params (SystemName, IEnumerable<ISystemEntity>)[] expected) where T : ICoreEntity {
    var ascore = await SysEntsToCore(CoreEntityTypeName.From<T>(), expected);
    var actual = added.Values.Where(e => e.CoreEntity.GetType() == typeof(T)).ToList();
    if (actual.Count != ascore.Count) throw new E2ETestFailedException($"Expected {typeof(T).Name} Created({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Created({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")));
    
    if(actual.Any(e => e.Meta.DateUpdated != UtcDate.UtcNow)) throw new E2ETestFailedException("Found entities with invalid DateUpdated");
    if(actual.Any(e => e.Meta.DateCreated != UtcDate.UtcNow)) throw new E2ETestFailedException("Found entities with invalid DateCreated");
  }

  public async Task ValidateUpdated<T>(params (SystemName, IEnumerable<ISystemEntity>)[] expected) where T : ICoreEntity {
    var eadded = added.Values.Select(e => e.CoreEntity).Where(e => e.GetType() == typeof(T)).ToList();
    var ascore = (await SysEntsToCore(CoreEntityTypeName.From<T>(), expected))
        .DistinctBy(e => (e.GetType(), e.CoreId))
        // remove from validation if these entities were added in this epoch, as
        // they will be validated in the `ValidateAdded` method
        .Where(e => !eadded.Contains(e)) 
        .ToList();
    var actual = updated.Values.Where(e => e.CoreEntity.GetType() == typeof(T)).ToList();
    if (actual.Count != ascore.Count) throw new E2ETestFailedException($"Expected {typeof(T).Name} Updated({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Updated({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.CoreEntity.DisplayName}({e.CoreEntity.CoreId})")));
    if(actual.Any(e => e.Meta.DateUpdated != UtcDate.UtcNow)) throw new E2ETestFailedException("Found entities with invalid DateUpdated");
    if(actual.Any(e => e.Meta.DateCreated >= UtcDate.UtcNow)) throw new E2ETestFailedException("Found entities with invalid DateCreated");
  }
  
  private async Task<List<ICoreEntity>> SysEntsToCore(CoreEntityTypeName coretype, params (SystemName, IEnumerable<ISystemEntity>)[] expected) {
    var cores = new List<ICoreEntity>();
    var allsums = new Dictionary<string, bool>();
    foreach (var sysents in expected) {
      var (system, sysentlst) = (sysents.Item1, sysents.Item2.ToList());
      if (!sysentlst.Any()) continue;
      var idmap = (await ctx.CtlRepo.GetMapsFromSystemIds(system, coretype, sysentlst.Select(e => e.SystemId).ToList())).ToDictionary(m => m.SystemId, m => m.CoreId);
      var existings = await ctx.CoreStore.GetExistingEntities(coretype, idmap.Values.ToList());
      var syscores = await sysentlst.Select(e => ToCore(e, existings.Single(e2 => e2.CoreEntity.CoreId == idmap[e.SystemId]).CoreEntity)).Synchronous();
      var sums = syscores.Select(c => ctx.ChecksumAlg.Checksum(c)).Distinct().ToList();
      if (syscores.Count != sums.Count) throw new Exception($"Expected all core entities from a system to be unique.  Found some entities that resulted in the same ICoreEntity checksum");
      syscores.ForEach((c, idx) => {
        if (allsums.ContainsKey(sums[idx])) return;
        
        allsums.Add(sums[idx], true);
        cores.Add(c);
      });
    }
    return cores;

    async Task<ICoreEntity> ToCore(ISystemEntity e, ICoreEntity existing) {
      return e switch {
        CrmMembershipType type => ctx.Converter.CrmMembershipTypeToCoreMembershipType(type, (CoreMembershipType?) existing), 
        CrmCustomer customer => ctx.Converter.CrmCustomerToCoreCustomer(customer, (CoreCustomer?) existing), 
        CrmInvoice invoice => await ctx.Converter.CrmInvoiceToCoreInvoice(invoice, (CoreInvoice?) existing), 
        FinAccount account => ctx.Converter.FinAccountToCoreCustomer(account, (CoreCustomer?) existing), 
        FinInvoice fininv => await ctx.Converter.FinInvoiceToCoreInvoice(fininv, (CoreInvoice?) existing), 
        _ => throw new NotSupportedException(e.GetType().Name)
      };
    }
  }

  public void Add(CoreEntityAndMeta coreent) {
    if (!added.TryAdd((coreent.CoreEntity.GetType(), coreent.CoreEntity.CoreId), coreent)) throw new Exception($"entity appears to have already been added: {coreent}");
  }
  

  public void Update(CoreEntityAndMeta coreent) {
    // ignore entities that have already been added in this epoch, they will be validated as part of the added validation
    if (added.ContainsKey((coreent.CoreEntity.GetType(), coreent.CoreEntity.CoreId))) return; 
    updated[(coreent.CoreEntity.GetType(), coreent.CoreEntity.CoreId)] = coreent;
  }

}