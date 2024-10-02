using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;
using Serilog;
using Serilog.Events;

namespace Centazio.E2E.Tests;

internal static class SimulationCtx {
  
  public static readonly bool SILENCE_LOGGING = true;
  public static readonly bool SILENCE_SIMULATION = true;
  public static readonly bool ALLOW_BIDIRECTIONAL = true;
  
  public static readonly SystemName CRM_SYSTEM;
  public static readonly SystemName FIN_SYSTEM;
  
  public static readonly ICtlRepository ctl = new InMemoryCtlRepository();
  public static readonly ICoreToSystemMapStore entitymap = new InMemoryCoreToSystemMapStore();
  public static readonly IStagedEntityStore stage = new InMemoryStagedEntityStore(0, Helpers.TestingChecksum);
  public static readonly CoreStorage core = new();
      
  static SimulationCtx() {
    CRM_SYSTEM = new(nameof(CrmSystem));
    FIN_SYSTEM = new(nameof(FinSystem));
  }
  
  public const int TOTAL_EPOCHS = 500;
  public const int CRM_MAX_NEW_CUSTOMERS = 2;
  public const int CRM_MAX_EDIT_CUSTOMERS = 4;
  public const int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  public const int CRM_MAX_NEW_INVOICES = 4;
  public const int CRM_MAX_EDIT_INVOICES = 4;
  
  public const int FIN_MAX_NEW_ACCOUNTS = 2;
  public const int FIN_MAX_EDIT_ACCOUNTS = 0;
  public const int FIN_MAX_NEW_INVOICES = 0;
  public const int FIN_MAX_EDIT_INVOICES = 0;
 
 public static void Debug(string message) {
   if (SILENCE_SIMULATION) return;
   if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message);
   else Helpers.DebugWrite(message);
 }
 
 public static string Checksum(CrmMembershipType m) => Helpers.TestingChecksum(new { Id = m.Id.ToString(), m.Name });
 public static string Checksum(CrmCustomer c) => Helpers.TestingChecksum(new { Id = c.Id.ToString(), c.Name, Membership = c.MembershipTypeId.ToString(), Invoices = core.GetInvoicesForCustomer(c.Id.ToString()).Select(e => e.Checksum).ToList() });
 public static string Checksum(FinAccount a) => Helpers.TestingChecksum(new { Id = a.Id.ToString(), a.Name, Invoices = core.GetInvoicesForCustomer(a.Id.ToString()).Select(e => e.Checksum).ToList() });
 public static string Checksum(CrmInvoice i) => Helpers.TestingChecksum(new { Id = i.Id.ToString(), Customer = i.CustomerId.ToString(), i.AmountCents, i.DueDate, i.PaidDate });
 public static string Checksum(FinInvoice i) => Helpers.TestingChecksum(new { Id = i.Id.ToString(), Customer = i.AccountId.ToString(), i.Amount, i.DueDate, i.PaidDate });
 
 public static string NewName<T>(string prefix, List<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
 
 public static string UpdateName(string name) {
   var tokens = name.Split(':');
   return $"{tokens[0]}:{Int32.Parse(tokens[1]) + 1}";
 } 
}

public class E2EEnvironment : IAsyncDisposable {

  // Crm
  private readonly CrmSystem crm = new();
  private readonly FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> crm_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> crm_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult> crm_write_runner;

  // Fin
  private readonly FinSystem fin = new();
  private readonly FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> fin_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> fin_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult> fin_write_runner;

  public E2EEnvironment() {
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new CrmReadFunction(crm),
        new ReadOperationRunner(SimulationCtx.stage),
        SimulationCtx.ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(new CrmPromoteFunction(SimulationCtx.core),
        new PromoteOperationRunner(SimulationCtx.stage, SimulationCtx.core, SimulationCtx.entitymap),
        SimulationCtx.ctl);
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult>(new CrmWriteFunction(crm, SimulationCtx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(SimulationCtx.entitymap, SimulationCtx.core), 
        SimulationCtx.ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new FinReadFunction(fin),
        new ReadOperationRunner(SimulationCtx.stage),
        SimulationCtx.ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(new FinPromoteFunction(SimulationCtx.core),
        new PromoteOperationRunner(SimulationCtx.stage, SimulationCtx.core, SimulationCtx.entitymap),
        SimulationCtx.ctl);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult>(new FinWriteFunction(fin, SimulationCtx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(SimulationCtx.entitymap, SimulationCtx.core), 
        SimulationCtx.ctl);
  }

  public async ValueTask DisposeAsync() {
    await SimulationCtx.stage.DisposeAsync();
    await SimulationCtx.ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    if (SimulationCtx.SILENCE_LOGGING)  LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    
    var systems = new List<ISystem> { crm, fin };
    await Enumerable.Range(0, SimulationCtx.TOTAL_EPOCHS).Select(epoch => RunEpoch(epoch, systems)).Synchronous();
  }

  private async Task RunEpoch(int epoch, List<ISystem> systems) {
    SimulationCtx.Debug($"\nEpoch[{epoch}] Starting {{{UtcDate.UtcNow:o}}}\n");
    
    RandomTimeStep();
    systems.ForEach(s => s.Simulation.Step());
    SimulationCtx.Debug($"\nEpoch[{epoch}] Simulation Step Completed - Running Functions {{{UtcDate.UtcNow:o}}}\n");
    
    await crm_read_runner.RunFunction();
    await fin_read_runner.RunFunction(); 
    await crm_promote_runner.RunFunction();
    await fin_promote_runner.RunFunction();
    await crm_write_runner.RunFunction();
    await fin_write_runner.RunFunction();
    
    SimulationCtx.Debug($"\nEpoch[{epoch}] Functions Completed - Validating {{{UtcDate.UtcNow:o}}}\n");
    await ValidateEpoch();
  }

  private void RandomTimeStep() {
    TestingUtcDate.DoTick(new TimeSpan(Random.Shared.Next(0, 2), Random.Shared.Next(0, 24), Random.Shared.Next(0, 60), Random.Shared.Next(0, 60)));
  }
  
  private Task ValidateEpoch() {
    CompareMembershipTypes();
    CompareCustomers();
    CompareInvoices();
    return Task.CompletedTask;
  }

  private void CompareMembershipTypes() {
    var core_types = SimulationCtx.core.Types.Cast<CoreMembershipType>().Select(m => new { m.Id, m.Name });
    var crm_types = crm.MembershipTypes.Select(m => new { m.Id, m.Name } );
    
    CompareByChecksum(SimulationCtx.CRM_SYSTEM, core_types, crm_types);
  }
  
  private void CompareCustomers() {
    var core_customers_for_crm = SimulationCtx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name, MembershipTypeId = c.Membership.Id });
    var core_customers_for_fin = SimulationCtx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name });
    var crm_customers = crm.Customers.Select(c => new { c.Id, c.Name, c.MembershipTypeId });
    var fin_accounts = fin.Accounts.Select(c => new { c.Id, c.Name });
    
    CompareByChecksum(SimulationCtx.CRM_SYSTEM, core_customers_for_crm, crm_customers);
    CompareByChecksum(SimulationCtx.FIN_SYSTEM, core_customers_for_fin, fin_accounts);
  }
  
  private void CompareInvoices() {
    var core_invoices = SimulationCtx.core.Invoices.Cast<CoreInvoice>().Select(i => new { i.Id, i.PaidDate, i.DueDate, Amount = i.Cents }).ToList();
    var crm_invoices = crm.Invoices.Select(i => new { i.Id, i.PaidDate, i.DueDate, Amount = i.AmountCents });
    var fin_invoices = fin.Invoices.Select(i => new { i.Id, i.PaidDate, DueDate = DateOnly.FromDateTime(i.DueDate), Amount = (int) (i.Amount * 100m) });
    
    CompareByChecksum(SimulationCtx.CRM_SYSTEM, core_invoices, crm_invoices);
    CompareByChecksum(SimulationCtx.FIN_SYSTEM, core_invoices, fin_invoices);
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