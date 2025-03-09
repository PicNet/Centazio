using Centazio.Core.Settings;
using Centazio.Test.Lib.E2E.Crm;
using Centazio.Test.Lib.E2E.Fin;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

public class E2EEnvironment(ISimulationProvider provider, CentazioSettings settings) : IAsyncDisposable {

  private readonly SimulationCtx ctx = new(provider, settings);
  
  private CrmApi crm = null!;
  private FinApi fin = null!;
  
  private CrmReadFunction crm_read = null!;
  private CrmPromoteFunction crm_promote = null!;
  private CrmWriteFunction crm_write = null!;
  private FinReadFunction fin_read = null!;
  private FinPromoteFunction fin_promote = null!;
  private FinWriteFunction fin_write = null!;
  
  public async Task Initialise() {
    await ctx.Initialise();
    
    (crm, fin) = (new CrmApi(ctx), new FinApi(ctx));
    
    (crm_read, crm_promote, crm_write) = (new CrmReadFunction(ctx, crm), new CrmPromoteFunction(ctx), new CrmWriteFunction(ctx, crm));
    (fin_read, fin_promote, fin_write) = (new FinReadFunction(ctx, fin), new FinPromoteFunction(ctx), new FinWriteFunction(ctx, fin));
  }
  
  public async Task RunSimulation() {
    await Initialise();
    if (SimulationConstants.SILENCE_LOGGING) LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    if (SimulationConstants.LOGGING_FILTERS.Any()) {
      Log.Logger = LogInitialiser.GetConsoleConfig(filters: SimulationConstants.LOGGING_FILTERS).CreateLogger();
      Log.Information($"logging filter enabled[{String.Join(',', SimulationConstants.LOGGING_FILTERS)}]");
    }
    
    await Enumerable.Range(0, SimulationConstants.TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
  }
  
  public async ValueTask DisposeAsync() => await ctx.DisposeAsync();

  private async Task RunEpoch(int epoch) {
    ctx.Epoch.SetEpoch(epoch);
    RandomTimeStep();
    ctx.Debug($"epoch[{epoch}] starting - running simulation step [{{@Now}}]", UtcDate.UtcNow);
    
    crm.Simulation.Step();
    fin.Simulation.Step(); 
    
    ctx.Debug($"epoch[{epoch}] simulation step completed - running functions");
    
    await crm_read.RunFunction();
    await crm_promote.RunFunction();
    await fin_read.RunFunction(); 
    await fin_promote.RunFunction();
    await crm_write.RunFunction();
    await fin_write.RunFunction();
    
    ctx.Debug($"epoch[{epoch}] functions completed - validating");
    await ValidateEpoch();
  }

  private void RandomTimeStep() => TestingUtcDate.DoTick(new TimeSpan(Rng.Next(0, 2), Rng.Next(0, 24), Rng.Next(0, 60), Rng.Next(0, 60)));

  private async Task ValidateEpoch() {
    await CompareMembershipTypes();
    await CompareCustomers();
    await CompareInvoices();
  }

  private async Task CompareMembershipTypes() {
    var core_types = (await ctx.CoreStore.GetMembershipTypes()).Select(m => ctx.Converter.CoreMembershipTypeToCrmMembershipType(SystemEntityId.DEFAULT_VALUE, m));
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []));
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedMemberships));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_types, crm.MembershipTypes);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToCrmCustomer(SystemEntityId.DEFAULT_VALUE, c));
    var core_customers_for_fin = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToFinAccount(SystemEntityId.DEFAULT_VALUE, c));
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>((SimulationConstants.CRM_SYSTEM, crm.Simulation.AddedCustomers), (SimulationConstants.FIN_SYSTEM, fin.Simulation.AddedAccounts));
    await ctx.Epoch.ValidateUpdated<CoreCustomer>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedCustomers), (SimulationConstants.FIN_SYSTEM, fin.Simulation.EditedAccounts));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm.Customers);
    CompareByChecksumWithoutSysId(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin.Accounts);
  }
  
  private async Task CompareInvoices() {
    var cores = await ctx.CoreStore.GetInvoices();
    var crmmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.CRM_SYSTEM,  CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var finmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.FIN_SYSTEM, CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var core_invoices_for_crm = cores.Select(i => ctx.Converter.CoreInvoiceToCrmInvoice(SystemEntityId.DEFAULT_VALUE, i, crmmaps));
    var core_invoices_for_fin = cores.Select(i => ctx.Converter.CoreInvoiceToFinInvoice(SystemEntityId.DEFAULT_VALUE, i, finmaps));
    
    await ctx.Epoch.ValidateAdded<CoreInvoice>((SimulationConstants.CRM_SYSTEM, crm.Simulation.AddedInvoices), (SimulationConstants.FIN_SYSTEM, fin.Simulation.AddedInvoices));
    await ctx.Epoch.ValidateUpdated<CoreInvoice>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedInvoices), (SimulationConstants.FIN_SYSTEM, fin.Simulation.EditedInvoices));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_invoices_for_crm, crm.Invoices);
    CompareByChecksumWithoutSysId(SimulationConstants.FIN_SYSTEM, core_invoices_for_fin, fin.Invoices);
  }
  
  [IgnoreNamingConventions] private void CompareByChecksumWithoutSysId(SystemName system, IEnumerable<ISystemEntity> fromcores, IEnumerable<ISystemEntity> targets) {
    var (corecs, targetscs) = (fromcores.Select(Describe).OrderBy(str => str).ToList(), targets.Select(Describe).OrderBy(str => str).ToList());
    if (!corecs.SequenceEqual(targetscs)) throw new E2ETestFailedException($"[{system}] checksum comparison failed\ncore entities:\n\t{String.Join("\n\t", corecs)}\ntarget system entities:\n\t{String.Join("\n\t", targetscs)}");
    
    // remove the SystemId from the checksum subset to make this validation simpler.  Otherwise above would need much more complex code to set the correct IDs
    //    from Map objects on every validation
    object Describe(ISystemEntity e) => Json.Serialize(e.CreatedWithId(new (system == SimulationConstants.CRM_SYSTEM ? Guid.Empty.ToString() : "0")).GetChecksumSubset());
  }
}