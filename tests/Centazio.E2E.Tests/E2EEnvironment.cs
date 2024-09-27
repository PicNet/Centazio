using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Extensions;
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

public class E2EEnvironment : IAsyncDisposable {

  private const int TOTAL_EPOCHS = 500;
  private readonly CoreStorage core = new();

  // Crm
  private readonly CrmSystem crm = new();
  private readonly CrmPromoteFunction crm_promote;
  private readonly CrmReadFunction crm_read;
  private readonly CrmWriteFunction crm_write;
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> crm_promote_runner;
  // todo: function runner should allow operations of SingleWrite and Batch write combined.  Currently new
  //    functions are required if we need to combine these
  private readonly FunctionRunner<BatchWriteOperationConfig, WriteOperationResult> crm_write_runner;

  // Fin
  private readonly FinSystem fin = new();
  private readonly FinPromoteFunction fin_promote;
  private readonly FinReadFunction fin_read;
  private readonly FinWriteFunction fin_write;
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> fin_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> fin_promote_runner;
  // todo: function runner should allow operations of SingleWrite and Batch write combined.  Currently new
  //    functions are required if we need to combine these
  private readonly FunctionRunner<BatchWriteOperationConfig, WriteOperationResult> fin_write_runner;

  // Infra
  private readonly ICtlRepository ctl = new InMemoryCtlRepository();
  private readonly IEntityIntraSystemMappingStore entitymap = new InMemoryEntityIntraSystemMappingStore();
  private readonly IStagedEntityStore stage = new InMemoryStagedEntityStore(0, s => s.GetHashCode().ToString());
  private readonly List<ISystem> Systems;

  public E2EEnvironment() {
    LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal; // disable logging (todo: not working)
    Systems = [crm, fin];
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(crm_read = new CrmReadFunction(crm),
        new ReadOperationRunner(stage),
        ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(crm_promote = new CrmPromoteFunction(core),
        new PromoteOperationRunner(stage, entitymap, core),
        ctl);
    
    crm_write_runner = new FunctionRunner<BatchWriteOperationConfig, WriteOperationResult>(crm_write = new CrmWriteFunction(crm),
        new WriteOperationRunner<BatchWriteOperationConfig>(entitymap, core), 
        ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(fin_read = new FinReadFunction(fin),
        new ReadOperationRunner(stage),
        ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(fin_promote = new FinPromoteFunction(core),
        new PromoteOperationRunner(stage, entitymap, core),
        ctl);
    
    fin_write_runner = new FunctionRunner<BatchWriteOperationConfig, WriteOperationResult>(fin_write = new FinWriteFunction(fin),
        new WriteOperationRunner<BatchWriteOperationConfig>(entitymap, core), 
        ctl);
  }

  public async ValueTask DisposeAsync() {
    await stage.DisposeAsync();
    await ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() => await Enumerable.Range(0, TOTAL_EPOCHS).Select(RunEpoch).Synchronous();

  private async Task RunEpoch(int epoch) {
    Log.Information($"Starting Epoch[{epoch}]");
    
    TestingUtcDate.DoTick(new TimeSpan(1, Random.Shared.Next(0, 24), Random.Shared.Next(0, 60), Random.Shared.Next(0, 60)));
    Systems.ForEach(s => s.Simulation.Step());
    Log.Information($"Epoch[{epoch}] Simulation Step Completed - Running Functions");
    
    await crm_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    await crm_write_runner.RunFunction();
    
    await fin_read_runner.RunFunction();
    await fin_promote_runner.RunFunction();
    await fin_write_runner.RunFunction();
    
    Log.Information($"Epoch[{epoch}] Functions Completed - Validating");
    await ValidateEpoch();
  }

  
  private async Task ValidateEpoch() {
    var staged_types = (await stage.GetAll(DateTime.MinValue, nameof(CrmSystem), nameof(CrmMembershipType))).ToList();
    var core_types = core.Types.Cast<CoreMembershipType>().ToList();
    throw new Exception($"staged_types[{staged_types.Count}] core_types[{core_types.Count}]");
    
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
  }

  private void CompareMembershipTypes(List<CoreMembershipType> core_types, List<CrmMembershipType> crm_types) {
    var expected = crm_types.Select(c => CoreMembershipType.FromCrmMembershipType(c, core)).OrderBy(c => c.Name).ToList();
    var actual = core_types.OrderBy(c => c.Name).ToList();
    
    Assert.That(actual, Is.EquivalentTo(expected));
  }
  
  private void CompareCustomers(List<CoreCustomer> core_customers, List<CrmCustomer> crm_customers, List<FinAccount> fin_accounts) {
    var actual = core_customers.OrderBy(c => c.Name).ToList();
    var expected1 = crm_customers.Select(c => CoreCustomer.FromCrmCustomer(c, core)).OrderBy(c => c.Name).ToList();
    var expected2 = fin_accounts.Select(a => CoreCustomer.FromFinAccount(a, core)).OrderBy(a => a.Name).ToList();
    
    Assert.That(actual, Is.EquivalentTo(expected1));
    Assert.That(actual, Is.EquivalentTo(expected2));
  }
  
  private void CompareInvoices(List<CoreInvoice> core_invoices, List<CrmInvoice> crm_invoices, List<FinInvoice> fin_invoices) {
    var actual = core_invoices.OrderBy(i => i.CustomerId).ThenBy(i => i.DueDate).ToList();
    var expected1 = crm_invoices.Select(i => CoreInvoice.FromCrmInvoice(i, core)).OrderBy(i => i.CustomerId).ThenBy(i => i.DueDate).ToList();
    var expected2 = fin_invoices.Select(i => CoreInvoice.FromFinInvoice(i, core)).OrderBy(i => i.CustomerId).ThenBy(i => i.DueDate).ToList();
    
    Assert.That(actual, Is.EquivalentTo(expected1));
    Assert.That(actual, Is.EquivalentTo(expected2));
  }
}