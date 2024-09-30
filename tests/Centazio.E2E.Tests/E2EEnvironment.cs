using Centazio.Core;
using Centazio.Core.CoreRepo;
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
  
  public static readonly SystemName CRM_SYSTEM;
  public static readonly SystemName FIN_SYSTEM;
      
  static SimulationCtx() {
    CRM_SYSTEM = new(nameof(CrmSystem));
    FIN_SYSTEM = new(nameof(FinSystem));
  }
  
  public const int CRM_MAX_NEW_CUSTOMERS = 2;
  public const int CRM_MAX_EDIT_CUSTOMERS = 2;
  public const int CRM_MAX_EDIT_MEMBERSHIPS = 0;
  public const int CRM_MAX_NEW_INVOICES = 0;
  public const int CRM_MAX_EDIT_INVOICES = 0;
  
  public const int FIN_MAX_NEW_ACCOUNTS = 0;
  public const int FIN_MAX_EDIT_ACCOUNTS = 0;
  public const int FIN_MAX_NEW_INVOICES = 0;
  public const int FIN_MAX_EDIT_INVOICES = 0;
 
 public static void Debug(string message) {
   if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message);
   else Helpers.DebugWrite(message);
 }
 
 public static string NewName<T>(string prefix, ICollection<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
 
 public static string UpdateName(string name) {
   var tokens = name.Split(':');
   return $"{tokens[0]}:{Int32.Parse(tokens[1]) + 1}";
 } 
}

public class E2EEnvironment : IAsyncDisposable {

  private const int TOTAL_EPOCHS = 500;
  private readonly CoreStorage core = new();

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

  // Infra
  private readonly ICtlRepository ctl = new InMemoryCtlRepository();
  private readonly IEntityIntraSystemMappingStore entitymap = new InMemoryEntityIntraSystemMappingStore();
  private readonly IStagedEntityStore stage = new InMemoryStagedEntityStore(0, s => s.GetHashCode().ToString());
  private readonly List<ISystem> Systems;

  public E2EEnvironment() {
    Systems = [crm, fin];
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new CrmReadFunction(crm),
        new ReadOperationRunner(stage),
        ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(new CrmPromoteFunction(core),
        new PromoteOperationRunner(stage, entitymap, core),
        ctl);
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult>(new CrmWriteFunction(crm, entitymap),
        new WriteOperationRunner<WriteOperationConfig>(entitymap, core), 
        ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult>(new FinReadFunction(fin),
        new ReadOperationRunner(stage),
        ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>(new FinPromoteFunction(core),
        new PromoteOperationRunner(stage, entitymap, core),
        ctl);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, CoreEntityType, WriteOperationResult>(new FinWriteFunction(fin, entitymap),
        new WriteOperationRunner<WriteOperationConfig>(entitymap, core), 
        ctl);
  }

  public async ValueTask DisposeAsync() {
    await stage.DisposeAsync();
    await ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    await Enumerable.Range(0, TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
  }

  private async Task RunEpoch(int epoch) {
    SimulationCtx.Debug($"Epoch[{epoch}] Starting");
    
    TestingUtcDate.DoTick(new TimeSpan(1, Random.Shared.Next(0, 24), Random.Shared.Next(0, 60), Random.Shared.Next(0, 60)));
    Systems.ForEach(s => s.Simulation.Step());
    SimulationCtx.Debug($"Epoch[{epoch}] Simulation Step Completed - Running Functions");
    
    var functions = new List<Task> { 
      crm_read_runner.RunFunction(),
      fin_read_runner.RunFunction(),
      crm_promote_runner.RunFunction(),
      fin_promote_runner.RunFunction(),
      crm_write_runner.RunFunction(),
      fin_write_runner.RunFunction()
    };
    // functions.AddRange(functions); // do twice to test ping backs
    /*
    await crm_read_runner.RunFunction();
    await fin_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    await fin_promote_runner.RunFunction();
    await crm_write_runner.RunFunction();
    await fin_write_runner.RunFunction();
    */
    await functions.Synchronous();
    SimulationCtx.Debug($"Epoch[{epoch}] Functions Completed - Validating");
    await ValidateEpoch();
  }

  
  private Task ValidateEpoch() {
    var core_types = core.Types.Cast<CoreMembershipType>().ToList();
    var core_customers = core.Customers.Cast<CoreCustomer>().ToList();
    var core_invoices = core.Invoices.Cast<CoreInvoice>().ToList();
    
    var crm_types = crm.MembershipTypes.ToList();
    var crm_customers = crm.Customers.ToList();
    var crm_invoices = crm.Invoices.ToList();
    
    var fin_accounts = fin.Accounts.ToList();
    var fin_invoices = fin.Invoices.ToList();
    
    CompareMembershipTypes(core_types, crm_types);
    CompareCustomers(core_customers, crm_customers, fin_accounts);
    CompareInvoices(core_invoices, crm_invoices, fin_invoices);
    return Task.CompletedTask;
  }

  private void CompareMembershipTypes(List<CoreMembershipType> core_types, List<CrmMembershipType> crm_types) {
    var expected = crm_types.Select(c => CoreMembershipType.FromCrmMembershipType(c, core));
    
    CompareByChecksun(expected, core_types);
  }
  
  private void CompareCustomers(List<CoreCustomer> core_customers, List<CrmCustomer> crm_customers, List<FinAccount> fin_accounts) {
    var expected1 = crm_customers.Select(c => CoreCustomer.FromCrmCustomer(c, core));
    var expected2 = fin_accounts.Select(a => CoreCustomer.FromFinAccount(a, core));
    
    CompareByChecksun(expected1, core_customers);
    CompareByChecksun(expected2, core_customers);
  }
  
  private void CompareInvoices(List<CoreInvoice> core_invoices, List<CrmInvoice> crm_invoices, List<FinInvoice> fin_invoices) {
    var expected1 = crm_invoices.Select(i => CoreInvoice.FromCrmInvoice(i, core));
    var expected2 = fin_invoices.Select(i => CoreInvoice.FromFinInvoice(i, core));
    
    CompareByChecksun(expected1, core_invoices);
    CompareByChecksun(expected2, core_invoices);
  }
  
  private void CompareByChecksun(IEnumerable<ICoreEntity> expected, IEnumerable<ICoreEntity> actual) {
    expected = expected.OrderBy(e => e.Checksum).ToList();
    actual = actual.OrderBy(e => e.Checksum).ToList();
    var expsums = expected.Select(e => e.Checksum).ToList();
    var actsums = actual.Select(e => e.Checksum).ToList();
    Assert.That(actsums, Is.EquivalentTo(expsums), $"EXPECTED:\n\t{String.Join("\n\t", expected)}\nACTUAL:\n\t{String.Join("\n\t", actual)}");
    
  }
}