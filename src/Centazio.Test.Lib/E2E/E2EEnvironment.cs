using Centazio.Core.Runner;
using Centazio.Test.Lib.E2E.Sim;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

public class E2EEnvironment(
    IChangesNotifier notifier,
    ISimulationInstructions instructions,
    ISimulationStorage storage) : IAsyncDisposable {
  
  private readonly bool SAVE_SIMULATION_STATE = false;
  
  private readonly SimulationCtx ctx = new(storage, TestingFactories.Settings());
  private readonly CrmDb crmdb = new();
  private readonly FinDb findb = new();
  private CrmApi crm = null!;
  private FinApi fin = null!;
  
  private IFunctionRunner runner = null!;
  private CrmReadFunction crm_read = null!;
  private CrmPromoteFunction crm_promote = null!;
  private CrmWriteFunction crm_write = null!;
  private FinReadFunction fin_read = null!;
  private FinPromoteFunction fin_promote = null!;
  private FinWriteFunction fin_write = null!;

  public async Task Initialise() {
    InitLogger();
    await ctx.Initialise();
    
    (crm, fin) = (new CrmApi(crmdb), new FinApi(findb));
    instructions.Init(ctx, crmdb, findb);
    
    (crm_read, crm_promote, crm_write) = (new CrmReadFunction(ctx, crm), new CrmPromoteFunction(ctx), new CrmWriteFunction(ctx, crm));
    (fin_read, fin_promote, fin_write) = (new FinReadFunction(ctx, fin), new FinPromoteFunction(ctx), new FinWriteFunction(ctx, fin));
    notifier.Init([crm_read, crm_promote, crm_write, fin_read, fin_promote, fin_write]);
    runner = new FunctionRunnerWithNotificationAdapter(new FunctionRunner(ctx.CtlRepo, ctx.Settings), notifier, () => TestingUtcDate.DoTick());
    _ = notifier.Run(runner);
  }

  private static void InitLogger() {
    Log.Logger = LogInitialiser
          .GetConsoleConfig(template: "{Message}{NewLine}", filters: SimulationConstants.LOGGING_FILTERS)
          .CreateLogger();
    if (!Env.IsGitHubActions() && !SimulationConstants.SILENCE_LOGGING) return;

    SimulationConstants.SILENCE_SIMULATION = true;
    LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
  }

  public async Task RunSimulation() {
    await Initialise();

    for (var epoch = 0; epoch < Int32.MaxValue; epoch++) {
      if (!instructions.HasMoreEpochs(epoch)) break;
      await RunEpoch(epoch);
    }
    await SaveCompletedSimluationState();

    async Task SaveCompletedSimluationState() {
      if (!SAVE_SIMULATION_STATE) return;
      var completedstate = new {
        CoreStorage = new {
          MembershipTypes = await ctx.CoreStore.GetMembershipTypes(),
          Customers = await ctx.CoreStore.GetCustomers(),
          Invoices = await ctx.CoreStore.GetInvoices(),
        },
        CrmDb = crmdb,
        FinDb = findb
      };
      await File.WriteAllTextAsync(FsUtils.GetDevPath($"simulation_state_{DateTime.Now:yyyyMMddHHmmss}.json"), Json.Serialize(completedstate));
    }
  }

  public async ValueTask DisposeAsync() => await ctx.DisposeAsync();

  private async Task RunEpoch(int epoch) {
    ctx.Epoch.SetEpoch(epoch);
    RandomTimeStep();
    ctx.Debug($"Epoch: [{epoch}] starting - running simulation step [{UtcDate.UtcNow:yy-MM-dd HH:mm:ss}]");
    
    instructions.Step(epoch);
    
    ctx.Debug($"Epoch: [{epoch}] simulation step completed - running functions");
    
    var trigger = new List<FunctionTrigger> { new TimerChangeTrigger(nameof(E2EEnvironment)) };
    TestingUtcDate.DoTick();
    await runner.RunFunction(crm_read, trigger);
    
    TestingUtcDate.DoTick();
    await runner.RunFunction(fin_read, trigger);
    
    // async notifiers need to wait for their background
    //    threads to finnish triggering other functions
    if (notifier.IsAsync) { await Task.Delay(storage.PostEpochDelayMs); } 
    else {
      await runner.RunFunction(crm_promote, trigger);
      await runner.RunFunction(fin_promote, trigger);
      await runner.RunFunction(crm_write, trigger);
      await runner.RunFunction(fin_write, trigger);
    }
    ctx.Debug($"Epoch: [{epoch}] functions completed - validating");
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
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, ctx.Epoch.Epoch == 0 ? crmdb.MembershipTypes : []));
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, instructions.EditedCrmMemberships));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_types, crmdb.MembershipTypes);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToCrmCustomer(SystemEntityId.DEFAULT_VALUE, c));
    var core_customers_for_fin = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToFinAccount(SystemEntityId.DEFAULT_VALUE, c));
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>((SimulationConstants.CRM_SYSTEM, instructions.AddedCrmCustomers), (SimulationConstants.FIN_SYSTEM, instructions.AddedFinAccounts));
    await ctx.Epoch.ValidateUpdated<CoreCustomer>((SimulationConstants.CRM_SYSTEM, instructions.EditedCrmCustomers), (SimulationConstants.FIN_SYSTEM, instructions.EditedFinAccounts));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crmdb.Customers);
    CompareByChecksumWithoutSysId(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, findb.Accounts);
  }
  
  private async Task CompareInvoices() {
    var cores = await ctx.CoreStore.GetInvoices();
    var crmmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.CRM_SYSTEM,  CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var finmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.FIN_SYSTEM, CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var core_invoices_for_crm = cores.Select(i => ctx.Converter.CoreInvoiceToCrmInvoice(SystemEntityId.DEFAULT_VALUE, i, crmmaps));
    var core_invoices_for_fin = cores.Select(i => ctx.Converter.CoreInvoiceToFinInvoice(SystemEntityId.DEFAULT_VALUE, i, finmaps));
    
    await ctx.Epoch.ValidateAdded<CoreInvoice>((SimulationConstants.CRM_SYSTEM, instructions.AddedCrmInvoices), (SimulationConstants.FIN_SYSTEM, instructions.AddedFinInvoices));
    await ctx.Epoch.ValidateUpdated<CoreInvoice>((SimulationConstants.CRM_SYSTEM, instructions.EditedCrmInvoices), (SimulationConstants.FIN_SYSTEM, instructions.EditedFinInvoices));
    CompareByChecksumWithoutSysId(SimulationConstants.CRM_SYSTEM, core_invoices_for_crm, crmdb.Invoices);
    CompareByChecksumWithoutSysId(SimulationConstants.FIN_SYSTEM, core_invoices_for_fin, findb.Invoices);
  }
  
  [IgnoreNamingConventions] private void CompareByChecksumWithoutSysId(SystemName system, IEnumerable<ISystemEntity> fromcores, IEnumerable<ISystemEntity> targets) {
    var (corecs, targetscs) = (fromcores.Select(Describe).OrderBy(str => str).ToList(), targets.Select(Describe).OrderBy(str => str).ToList());
    if (!corecs.SequenceEqual(targetscs)) throw new E2ETestFailedException($"[{system}] checksum comparison failed" +
        $"\ncore entities ({corecs.Count}){ctx.DetailsToString(corecs)}" +
        $"\ntarget system entities ({targetscs.Count}){ctx.DetailsToString(targetscs)}");
    
    // remove the SystemId from the checksum subset to make this validation simpler.  Otherwise above would need much more complex code to set the correct IDs
    //    from Map objects on every validation
    string Describe(ISystemEntity e) => Json.Serialize(e.CreatedWithId(new (system == SimulationConstants.CRM_SYSTEM ? Guid.Empty.ToString() : "0")).GetChecksumSubset());
  }
}