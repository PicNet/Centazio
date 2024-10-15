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
  
  public async Task ValidateAdded<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var ascore = await SysEntsToCore(expected);
    var actual = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"Expected {typeof(T).Name} Created({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Created({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
    
    Assert.That(actual.All(e => e.DateUpdated == UtcDate.UtcNow));
    Assert.That(actual.All(e => e.DateCreated == UtcDate.UtcNow));
  }

  public async Task ValidateUpdated<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var eadded = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    var ascore = (await SysEntsToCore(expected))
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
  
  private async Task<List<ICoreEntity>> SysEntsToCore(params IEnumerable<ISystemEntity>[] expected) {
    var cores = new List<ICoreEntity>();
    var allsums = new Dictionary<string, bool>();
    foreach (var sysent in expected) {
      var syscores = await sysent.Select(ToCore).Synchronous();
      var sums = syscores.Select(c => ctx.ChecksumAlg.Checksum(c)).Distinct().ToList();
      if (syscores.Count != sums.Count) throw new Exception($"Expected all core entities from a system to be unique.  Found some entities that resulted in the same ICoreEntity checksum");
      syscores.ForEach((c, idx) => {
        if (allsums.ContainsKey(sums[idx])) return;
        
        allsums.Add(sums[idx], true);
        cores.Add(c);
      });
    }
    return cores;

    Task<ICoreEntity> ToCore(ISystemEntity e) {
      var exid = ctx.EntityMap.Db.Keys.SingleOrDefault(m => m.SystemId == e.SystemId)?.CoreId;
      ICoreEntity result = e switch {
        CrmMembershipType type => ctx.Converter.CrmMembershipTypeToCoreMembershipType(type, ctx.CoreStore.GetMembershipType(exid)), 
        CrmCustomer customer => ctx.Converter.CrmCustomerToCoreCustomer(customer, ctx.CoreStore.GetCustomer(exid)), 
        CrmInvoice invoice => ctx.Converter.CrmInvoiceToCoreInvoice(invoice, ctx.CoreStore.GetInvoice(exid)), 
        FinAccount account => ctx.Converter.FinAccountToCoreCustomer(account, ctx.CoreStore.GetCustomer(exid)), 
        FinInvoice fininv => ctx.Converter.FinInvoiceToCoreInvoice(fininv, ctx.CoreStore.GetInvoice(exid)), 
        _ => throw new NotSupportedException(e.GetType().Name)
      };
      return Task.FromResult(result);
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