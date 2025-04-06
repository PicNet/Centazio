using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Test.Lib.E2E.Crm;
using Centazio.Test.Lib.E2E.Fin;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

// todo: would be good to have multiple kinds of simulations, such as a simple one-way, one-epoch integration,
//    this class could just be the engine to run any sort of simulation
public class E2EEnvironment(IChangesNotifier notifier, ISimulationProvider provider, CentazioSettings settings) : IAsyncDisposable {
  
  private readonly bool SAVE_SIMULATION_STATE = false;
  
  private readonly SimulationCtx ctx = new(provider, settings);
  private CrmApi crm = null!;
  private FinApi fin = null!;
  
  private IFunctionRunner runner = null!;
  private CrmReadFunction crm_read = null!;
  private CrmPromoteFunction crm_promote = null!;
  private CrmWriteFunction crm_write = null!;
  private FinReadFunction fin_read = null!;
  private FinPromoteFunction fin_promote = null!;
  private FinWriteFunction fin_write = null!;
  private IChangesNotifier notifier = notifier;

  public async Task Initialise() {
    // todo: remove this line once SqliteE2E is fixed, this line effectively removes the real notifier when running in CI
    if (Env.IsGitHubActions()) { notifier = new NoOpChangeNotifier(); } 
    
    Log.Logger = LogInitialiser.GetConsoleConfig(template: "{Message}{NewLine}").CreateLogger();
    await ctx.Initialise();
    
    (crm, fin) = (new CrmApi(ctx), new FinApi(ctx));
    
    (crm_read, crm_promote, crm_write) = (new CrmReadFunction(ctx, crm), new CrmPromoteFunction(ctx), new CrmWriteFunction(ctx, crm));
    (fin_read, fin_promote, fin_write) = (new FinReadFunction(ctx, fin), new FinPromoteFunction(ctx), new FinWriteFunction(ctx, fin));
    notifier.Init([crm_read, crm_promote, crm_write, fin_read, fin_promote, fin_write]);
    runner = new FunctionRunnerWithNotificationAdapter(new FunctionRunner(ctx.CtlRepo, ctx.Settings), notifier);
    _ = notifier.Run(runner);
  }
  
  public async Task RunSimulation() {
    await Initialise();
    if (Env.IsGitHubActions() || SimulationConstants.SILENCE_LOGGING) {
      SimulationConstants.SILENCE_SIMULATION = true;
      LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    } else if (SimulationConstants.LOGGING_FILTERS.Any()) {
      Log.Logger = LogInitialiser.GetConsoleConfig(filters: SimulationConstants.LOGGING_FILTERS).CreateLogger();
      Log.Information($"logging filter enabled[{String.Join(',', SimulationConstants.LOGGING_FILTERS)}]");
    }
    
    await Enumerable.Range(0, SimulationConstants.TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
    await SaveCompletedSimluationState();

    async Task SaveCompletedSimluationState() {
      if (!SAVE_SIMULATION_STATE) return;
      var completedstate = new {
        CoreStorage = new {
          MembershipTypes = await ctx.CoreStore.GetMembershipTypes(),
          Customers = await ctx.CoreStore.GetCustomers(),
          Invoices = await ctx.CoreStore.GetInvoices(),
        },
        CrmApi = new {
          crm.MembershipTypes,
          crm.Customers,
          crm.Invoices
        },
        FinApi = new {
          fin.Accounts,
          fin.Invoices
        }
      };
      await File.WriteAllTextAsync(FsUtils.GetDevPath($"simulation_state_{DateTime.Now:yyyyMMddHHmmss}.json"), Json.Serialize(completedstate));
    }
  }
  
  public async ValueTask DisposeAsync() => await ctx.DisposeAsync();

  private async Task RunEpoch(int epoch) {
    ctx.Epoch.SetEpoch(epoch);
    RandomTimeStep();
    ctx.Debug($"epoch[{epoch}] starting - running simulation step [{UtcDate.UtcNow:yy-MM-dd HH:mm:ss}]");
    
    crm.Simulation.Step();
    fin.Simulation.Step(); 
    
    ctx.Debug($"epoch[{epoch}] simulation step completed - running functions");
    
    var trigger = new List<FunctionTrigger> { new TimerChangeTrigger(nameof(E2EEnvironment)) };
    await runner.RunFunction(crm_read, trigger);
    await runner.RunFunction(fin_read, trigger);
    if (notifier is not NoOpChangeNotifier ipcn) {
      // allow the notifier to run and all writes flushed to db
      // while (!ipcn.IsEmpty || runner.Running) { await Task.Delay(15); }
      await Task.Delay(250);
    } else {
      await runner.RunFunction(crm_promote, trigger);
      await runner.RunFunction(fin_promote, trigger);
      await runner.RunFunction(crm_write, trigger);
      await runner.RunFunction(fin_write, trigger);
    }
    await Task.Delay(250);
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
    if (!corecs.SequenceEqual(targetscs)) throw new E2ETestFailedException($"[{system}] checksum comparison failed" +
        $"\ncore entities ({corecs.Count}):{ctx.DetailsToString(corecs)}" +
        $"\ntarget system entities ({targetscs.Count}):{ctx.DetailsToString(targetscs)}");
    
    // remove the SystemId from the checksum subset to make this validation simpler.  Otherwise above would need much more complex code to set the correct IDs
    //    from Map objects on every validation
    string Describe(ISystemEntity e) => Json.Serialize(e.CreatedWithId(new (system == SimulationConstants.CRM_SYSTEM ? Guid.Empty.ToString() : "0")).GetChecksumSubset());
  }
}