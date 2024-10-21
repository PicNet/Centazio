using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;

namespace Centazio.E2E.Tests;

[IgnoreNamingConventions]
public class EpochTracker(int epoch, SimulationCtx ctx) {
  public int Epoch { get; } = epoch;
  
  private readonly Dictionary<(Type, string), ICoreEntity> added = new();
  private readonly Dictionary<(Type, string), ICoreEntity> updated = new();
  
  public async Task ValidateAdded<T>(params (SystemName, IEnumerable<ISystemEntity>)[] expected) where T : ICoreEntity {
    var ascore = await SysEntsToCore(CoreEntityTypeName.From<T>(), expected);
    var actual = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"Expected {typeof(T).Name} Created({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Created({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
    
    Assert.That(actual.All(e => e.DateUpdated == UtcDate.UtcNow));
    Assert.That(actual.All(e => e.DateCreated == UtcDate.UtcNow));
  }

  public async Task ValidateUpdated<T>(params (SystemName, IEnumerable<ISystemEntity>)[] expected) where T : ICoreEntity {
    var eadded = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    var ascore = (await SysEntsToCore(CoreEntityTypeName.From<T>(), expected))
        .DistinctBy(e => (e.GetType(), e.CoreId))
        // remove from validation if these entities were added in this epoch, as
        // they will be validated in the `ValidateAdded` method
        .Where(e => !eadded.Contains(e)) 
        .ToList();
    var actual = updated.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"Expected {typeof(T).Name} Updated({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Updated({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
    Assert.That(actual.All(e => e.DateUpdated == UtcDate.UtcNow));
    Assert.That(actual.All(e => e.DateCreated < UtcDate.UtcNow));
  }
  
  private async Task<List<ICoreEntity>> SysEntsToCore(CoreEntityTypeName coretype, params (SystemName, IEnumerable<ISystemEntity>)[] expected) {
    var cores = new List<ICoreEntity>();
    var allsums = new Dictionary<string, bool>();
    foreach (var sysents in expected) {
      var (system, sysentlst) = (sysents.Item1, sysents.Item2.ToList());
      if (!sysentlst.Any()) continue;
      var idmap = (await ctx.CtlRepo.GetMapsFromSystemIds(system, coretype, sysentlst.Select(e => e.SystemId).ToList())).ToDictionary(m => m.SystemId, m => m.CoreId);
      var syscores = await sysentlst.Select(e => ToCore(e, idmap)).Synchronous();
      var sums = syscores.Select(c => ctx.ChecksumAlg.Checksum(c)).Distinct().ToList();
      if (syscores.Count != sums.Count) throw new Exception($"Expected all core entities from a system to be unique.  Found some entities that resulted in the same ICoreEntity checksum");
      syscores.ForEach((c, idx) => {
        if (allsums.ContainsKey(sums[idx])) return;
        
        allsums.Add(sums[idx], true);
        cores.Add(c);
      });
    }
    return cores;

    async Task<ICoreEntity> ToCore(ISystemEntity e, IDictionary<SystemEntityId, CoreEntityId> idmap) {
      idmap.TryGetValue(e.SystemId, out var exid);
      // todo: these queries should run once for whole batch not for every single entity
      return e switch {
        CrmMembershipType type => ctx.Converter.CrmMembershipTypeToCoreMembershipType(type, await ctx.CoreStore.GetMembershipType(exid)), 
        CrmCustomer customer => ctx.Converter.CrmCustomerToCoreCustomer(customer, await ctx.CoreStore.GetCustomer(exid)), 
        CrmInvoice invoice => await ctx.Converter.CrmInvoiceToCoreInvoice(invoice, await ctx.CoreStore.GetInvoice(exid)), 
        FinAccount account => ctx.Converter.FinAccountToCoreCustomer(account, await ctx.CoreStore.GetCustomer(exid)), 
        FinInvoice fininv => await ctx.Converter.FinInvoiceToCoreInvoice(fininv, await ctx.CoreStore.GetInvoice(exid)), 
        _ => throw new NotSupportedException(e.GetType().Name)
      };
    }
  }

  public void Add(ICoreEntity coreent) {
    if (!added.TryAdd((coreent.GetType(), coreent.CoreId), coreent)) throw new Exception($"entity appears to have already been added: {coreent}");
  }
  
  public void Update(ICoreEntity coreent) {
    // ignore entities that have already been added in this epoch, they will be validated as part of the added validation
    if (added.ContainsKey((coreent.GetType(), coreent.CoreId))) return; 
    updated[(coreent.GetType(), coreent.CoreId)] = coreent;
  }

}