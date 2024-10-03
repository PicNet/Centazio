using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;
using Serilog;
using Serilog.Events;

namespace Centazio.E2E.Tests;

public static class SimulationConstants {
  public static readonly SystemName CRM_SYSTEM = new(nameof(CrmSystem));
  public static readonly SystemName FIN_SYSTEM = new(nameof(FinSystem));
}

public class EpochTracker(int epoch, SimulationCtx ctx) {
  public int Epoch { get; } = epoch;
  
  private readonly Dictionary<(Type, string), ICoreEntity> added = new();
  private readonly Dictionary<(Type, string), ICoreEntity> updated = new();
  
  public void ValidateAdded<T>(params IEnumerable<object>[] expected) where T : ICoreEntity {
    var ascore = ExternalEntitiesToCore(expected);
    var actual = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateAdded Type[{typeof(T).Name}] Expected[{ascore.Count()}] Actual[{actual.Count}]" +
        $"\nExpected Items:\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.Id})")) + 
        "\nActual Items:\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.Id})")));
  }

  public void ValidateUpdated<T>(params IEnumerable<object>[] expected) where T : ICoreEntity {
    var ascore = ExternalEntitiesToCore(expected);
    var actual = updated.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateUpdated Type[{typeof(T).Name}] Expected[{ascore.Count}] Actual[{actual.Count}]" +
        $"\nExpected Items:\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.Id})")) + 
        "\nActual Items:\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.Id})")));
  }

  private List<ICoreEntity> ExternalEntitiesToCore(params IEnumerable<object>[] expected) {
    return expected.SelectMany(lst => lst.Select(ToCore)).ToList();

    ICoreEntity ToCore(object e) => e switch { 
      CrmMembershipType type => ctx.CrmMembershipTypeToCoreMembershipType(type), 
      CrmCustomer customer => ctx.CrmCustomerToCoreCustomer(customer), 
      CrmInvoice invoice => ctx.CrmInvoiceToCoreInvoice(invoice), 
      FinAccount account => ctx.FinAccountToCoreCustomer(account), 
      FinInvoice finInvoice => ctx.FinInvoiceToCoreInvoice(finInvoice), 
      _ => throw new NotSupportedException(e.GetType().Name) };
  }

  public void Add(ICoreEntity e) {
    var key = (e.GetType(), e.Id);
    if (added.ContainsKey(key)) throw new Exception($"entity appears to have already been added: {e}");
    if (updated.ContainsKey(key)) throw new Exception($"entity appears to have already been updated: {e}");
    added[key] = e;
  }
  
  public void Update(ICoreEntity e) {
    var key = (e.GetType(), e.Id);
    if (added.ContainsKey(key)) throw new Exception($"entity appears to have been added in this epoch, do not update, makes it hard to test: {e}");
    updated[key] = e;
  }
}

public class TestingInMemoryCoreToSystemMapStore : InMemoryCoreToSystemMapStore {
  public List<CoreToExternalMap> Db => memdb.Values.ToList();
}

public class SimulationCtx {
  
  public readonly bool SILENCE_LOGGING = false;
  public readonly bool SILENCE_SIMULATION = false;
  public readonly bool ALLOW_BIDIRECTIONAL = true;
  
  public readonly Random rng = new(1);
  public readonly IChecksumAlgorithm checksum = new Sha256ChecksumAlgorithm();
  public readonly ICtlRepository ctl = new InMemoryCtlRepository();
  public readonly TestingInMemoryCoreToSystemMapStore entitymap = new();
  
  public EpochTracker Epoch { get; set; }
  public readonly CoreStorage core;
  public readonly IStagedEntityStore stage;

  internal SimulationCtx() {
    core = new(this);
    stage = new InMemoryStagedEntityStore(0, checksum.Checksum);
    Epoch = new(0, this);
  }

  public readonly int TOTAL_EPOCHS = 50;
  public readonly int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  public readonly int CRM_MAX_NEW_CUSTOMERS = 4;
  public readonly int CRM_MAX_EDIT_CUSTOMERS = 4;
  public readonly int CRM_MAX_NEW_INVOICES = 4;
  public readonly int CRM_MAX_EDIT_INVOICES = 4;
  
  // todo: adding this causes issues
  public readonly int FIN_MAX_NEW_ACCOUNTS = 0;
  public readonly int FIN_MAX_EDIT_ACCOUNTS = 0;
  public readonly int FIN_MAX_NEW_INVOICES = 0;
  public readonly int FIN_MAX_EDIT_INVOICES = 0;
 
 public void Debug(string message) {
   if (SILENCE_SIMULATION) return;
   if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message);
   else Helpers.DebugWrite(message);
 }
 
 public string NewName<T>(string prefix, List<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
 public string UpdateName(string name) {
   var (label, count, _) = name.Split(':');
   return $"{label}:{Int32.Parse(count) + 1}";
 }
 
  public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c) {
    var (membership, invoices) = (core.GetMembershipType(c.MembershipTypeId.ToString()), core.GetInvoicesForCustomer(c.Id.ToString()));
    return new CoreCustomer(c.Id.ToString(), SimulationConstants.CRM_SYSTEM, c.Updated, c.Name, membership, invoices);
  }
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a) {
    var (pending, invoices) = (core.GetMembershipType(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString()), core.GetInvoicesForCustomer(a.Id.ToString()));
    return new CoreCustomer(a.Id.ToString(), SimulationConstants.FIN_SYSTEM, a.Updated, a.Name, pending, invoices);
  }
  public CoreInvoice CrmInvoiceToCoreInvoice(CrmInvoice i, string? custid = null) {
    // todo: add in if statement back after validating works
    var newcustid = entitymap.Db.Single(m => m.ExternalSystem == SimulationConstants.CRM_SYSTEM && m.CoreEntity == CoreEntityType.From<CoreCustomer>() && m.ExternalId == i.CustomerId.ToString()).CoreId;
    if (custid != null && newcustid != custid) throw new Exception();
      
    return new CoreInvoice(i.Id.ToString(), SimulationConstants.CRM_SYSTEM, i.Updated, custid ?? newcustid, i.AmountCents, i.DueDate, i.PaidDate);
  }

  public CoreInvoice FinInvoiceToCoreInvoice(FinInvoice i, string? custid = null) {
    // todo: add in if statement back after validating works
    var newcustid = entitymap.Db.Single(m => m.ExternalSystem == SimulationConstants.FIN_SYSTEM && m.CoreEntity == CoreEntityType.From<CoreCustomer>() && m.ExternalId == i.AccountId.ToString()).CoreId;
    if (custid != null && newcustid != custid) throw new Exception();
    return new CoreInvoice(i.Id.ToString(), SimulationConstants.FIN_SYSTEM, i.Updated, custid ?? newcustid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate);
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m) => new(m.Id.ToString(), m.Updated, m.Name);
  
  public List<T> ShuffleAndTake<T>(IEnumerable<T> enumerable, int? take = null) {
    var list = enumerable.ToList();
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = rng.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return take.HasValue ? list.Take(take.Value).ToList() : list;
  }
  
  public T RandomItem<T>(IList<T> lst) => lst[rng.Next(lst.Count)];
}

public class E2EEnvironment : IAsyncDisposable {

  private static readonly SimulationCtx ctx = new();
  
  // Crm
  private readonly CrmSystem crm = new(ctx);
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> crm_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, WriteOperationResult> crm_write_runner;

  // Fin
  private readonly FinSystem fin = new(ctx);
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> fin_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> fin_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, WriteOperationResult> fin_write_runner;

  public E2EEnvironment() {
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new CrmReadFunction(ctx, crm),
        new ReadOperationRunner(ctx.stage),
        ctx.ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new CrmPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.stage, ctx.core, ctx.entitymap),
        ctx.ctl);
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new CrmWriteFunction(ctx, crm, ctx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new FinReadFunction(ctx, fin),
        new ReadOperationRunner(ctx.stage),
        ctx.ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new FinPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.stage, ctx.core, ctx.entitymap),
        ctx.ctl);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new FinWriteFunction(ctx, fin, ctx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
  }

  public async ValueTask DisposeAsync() {
    await ctx.stage.DisposeAsync();
    await ctx.ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    if (ctx.SILENCE_LOGGING)  LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    
    await Enumerable.Range(0, ctx.TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
  }

  private async Task RunEpoch(int epoch) {
    ctx.Debug($"Epoch[{epoch}] Starting {{{UtcDate.UtcNow:o}}}");
    ctx.Epoch = new(epoch, ctx);
    
    RandomTimeStep();
    
    ctx.Debug($"Epoch[{epoch}] Simulation Step Completed - Running Functions {{{UtcDate.UtcNow:o}}}");
    
    crm.Simulation.Step();
    await crm_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    await fin_write_runner.RunFunction();
    fin.Simulation.Step();
    await fin_read_runner.RunFunction(); 
    await fin_promote_runner.RunFunction();
    await crm_write_runner.RunFunction();
    
    ctx.Debug($"Epoch[{epoch}] Functions Completed - Validating {{{UtcDate.UtcNow:o}}}");
    await ValidateEpoch();
  }

  private void RandomTimeStep() {
    TestingUtcDate.DoTick(new TimeSpan(ctx.rng.Next(0, 2), ctx.rng.Next(0, 24), ctx.rng.Next(0, 60), ctx.rng.Next(0, 60)));
  }
  
  private Task ValidateEpoch() {
    CompareMembershipTypes();
    CompareCustomers();
    CompareInvoices();
    return Task.CompletedTask;
  }

  private void CompareMembershipTypes() {
    var core_types = ctx.core.Types.Cast<CoreMembershipType>().Select(m => new { m.Id, m.Name });
    var crm_types = crm.MembershipTypes.Select(m => new { m.Id, m.Name } );
    
    ctx.Epoch.ValidateAdded<CoreMembershipType>(ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []);
    ctx.Epoch.ValidateUpdated<CoreMembershipType>(crm.Simulation.EditedMemberships);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_types, crm_types);
  }
  
  private void CompareCustomers() {
    var core_customers_for_crm = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name, MembershipTypeId = c.Membership.Id });
    var core_customers_for_fin = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name });
    var crm_customers = crm.Customers.Select(c => new { c.Id, c.Name, c.MembershipTypeId });
    var fin_accounts = fin.Accounts.Select(c => new { c.Id, c.Name });
    
    ctx.Epoch.ValidateAdded<CoreCustomer>(crm.Simulation.AddedCustomers, fin.Simulation.AddedAccounts);
    ctx.Epoch.ValidateUpdated<CoreCustomer>(crm.Simulation.EditedCustomers, fin.Simulation.EditedAccounts);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm_customers);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin_accounts);
  }
  
  private void CompareInvoices() {
    var core_invoices = ctx.core.Invoices.Cast<CoreInvoice>().Select(i => new { i.Id, i.PaidDate, i.DueDate, Amount = i.Cents }).ToList();
    var crm_invoices = crm.Invoices.Select(i => new { i.Id, i.PaidDate, i.DueDate, Amount = i.AmountCents });
    var fin_invoices = fin.Invoices.Select(i => new { i.Id, i.PaidDate, DueDate = DateOnly.FromDateTime(i.DueDate), Amount = (int) (i.Amount * 100m) });
    
    ctx.Epoch.ValidateAdded<CoreInvoice>(crm.Simulation.AddedInvoices, fin.Simulation.AddedInvoices);
    ctx.Epoch.ValidateUpdated<CoreInvoice>(crm.Simulation.EditedInvoices, fin.Simulation.EditedInvoices);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_invoices, crm_invoices);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_invoices, fin_invoices);
  }
  
  private readonly JsonSerializerOptions withid = new();
  private readonly JsonSerializerOptions noid = new() {
    TypeInfoResolver = new DefaultJsonTypeInfoResolver {
      Modifiers = {
        ti => {
          if (ti.Kind != JsonTypeInfoKind.Object) return;
          ti.Properties.Remove(ti.Properties.Single(p => p.Name == "Id"));
        }
      }
    }
  };
  
  private void CompareByChecksum(SystemName targetsys, IEnumerable<object> cores, IEnumerable<object> targets) {
    var (coreslst, targetslst) = (cores.ToList(), targets.ToList());
    var (core_compare, core_desc) = (coreslst.Select(e => Json(e, false)), coreslst.Select(e => Json(e, true)));
    var (targets_compare, targets_desc) = (targetslst.Select(e => Json(e, false)), targetslst.Select(e => Json(e, true)));
    Assert.That(targets_compare, Is.EquivalentTo(core_compare), $"CORES:\n\t{String.Join("\n\t", core_desc)}\nTARGET[{targetsys}]:\n\t{String.Join("\n\t", targets_desc)}");
    
    string Json(object obj, bool includeid) => JsonSerializer.Serialize(obj, includeid ? withid : noid);
  }
}