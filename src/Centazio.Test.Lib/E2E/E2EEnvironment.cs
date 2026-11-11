using Centazio.Core.Runner;
using Centazio.Test.Lib.E2E.Sim;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

public class E2EEnvironment(
    IChangesNotifier notifier,
    ISimulationInstructions instructions,
    ISimulationStorage storage,
    int? seed = null) : IAsyncDisposable {
  
  private readonly bool SAVE_SIMULATION_STATE = false;
  
  private readonly CrmDb crmdb = new();
  private readonly FinDb findb = new();
  
  private SimulationCtx ctx = null!;
  private CrmApi crm = null!;
  private FinApi fin = null!;
  
  private FunctionRunnerWithNotificationAdapter runner = null!;
  private RecordingFunctionRunner recorder = null!;
  private CrmReadFunction crm_read = null!;
  private CrmPromoteFunction crm_promote = null!;
  private CrmWriteFunction crm_write = null!;
  private FinReadFunction fin_read = null!;
  private FinPromoteFunction fin_promote = null!;
  private FinWriteFunction fin_write = null!;

  public async Task Initialise() {
    var silent = InitLogger();
    ctx = new (storage, await F.Settings(), seed ?? SimulationConstants.RANDOM_SEED, silent);
    await ctx.Initialise();
    
    (crm, fin) = (new CrmApi(crmdb, ctx.Clock), new FinApi(findb, ctx.Clock));
    instructions.Init(ctx, crmdb, findb);
    
    (crm_read, crm_promote, crm_write) = (new CrmReadFunction(ctx, crm), new CrmPromoteFunction(ctx), new CrmWriteFunction(ctx, crm));
    (fin_read, fin_promote, fin_write) = (new FinReadFunction(ctx, fin), new FinPromoteFunction(ctx), new FinWriteFunction(ctx, fin));
    notifier.Init([crm_read, crm_promote, crm_write, fin_read, fin_promote, fin_write]);
    recorder = new RecordingFunctionRunner(new FunctionRunner(ctx.CtlRepo, ctx.Settings, ctx.Clock, true));
    runner = new FunctionRunnerWithNotificationAdapter(recorder, notifier);
    runner.OnFunctionRunning += (_, _) => ctx.Clock.Tick();
    // the in-process notifier is drained explicitly after each function run (see `RunFuncAndWait`), its background
    //    `Run` loop would process notifications on another thread
    if (notifier is not InProcessChangesNotifier) await notifier.Run(runner);
  }

  private static bool InitLogger() {
    Log.Logger = LogInitialiser
          .GetConsoleConfig(template: "{Message}{NewLine}", filters: SimulationConstants.LOGGING_FILTERS)
          .CreateLogger();
    if (!Env.IsGitHubActions && !SimulationConstants.SILENCE_LOGGING) return false;

    LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    return true;
  }

  /// <returns>the fingerprint of the simulation: a running hash of its state after each epoch</returns>
  public async Task<List<string>> RunSimulation() {
    await Initialise();

    var fingerprint = new List<string>();
    for (var epoch = 0; epoch < Int32.MaxValue; epoch++) {
      if (!instructions.HasMoreEpochs(epoch)) break;
      await RunEpoch(epoch);
      fingerprint.Add(SimulationFingerprint.Next(fingerprint.LastOrDefault() ?? String.Empty, await CaptureState()));
    }
    await SaveCompletedSimluationState();
    return fingerprint;

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
      await File.WriteAllTextAsync(FsUtils.GetCentazioPath($"simulation_state_{ctx.Clock.UtcNow():yyyyMMddHHmmss}.json"), Json.Serialize(completedstate));
    }
  }

  public async ValueTask DisposeAsync() => await ctx.DisposeAsync();

  private async Task RunEpoch(int epoch) {
    ctx.Epoch.SetEpoch(epoch);
    RandomTimeStep();
    ctx.Debug($"Epoch: [{epoch}] starting - running simulation step [{ctx.Clock.UtcNow():yy-MM-dd HH:mm:ss}]");
    
    instructions.Step(epoch);
    
    ctx.Debug($"Epoch: [{epoch}] simulation step completed - running functions");
    
    var triggers = new List<FunctionTrigger> { new TimerChangeTrigger(nameof(E2EEnvironment)) };
    
    await RunFuncAndWait(crm_read);
    await RunFuncAndWait(fin_read);
    
    if (notifier is NoOpChangeNotifier) {
      // if not using notifier, then manually call promoters and writers
      await runner.RunFunction(crm_promote, triggers);
      await runner.RunFunction(fin_promote, triggers);
      await runner.RunFunction(crm_write, triggers);
      await runner.RunFunction(fin_write, triggers);
    }
    
    ctx.Debug($"Epoch: [{epoch}] functions completed - validating");
    await ValidateEpoch();

    async Task RunFuncAndWait(IRunnableFunction func) {
      ctx.Clock.Tick();
      await runner.RunFunction(func, triggers);
      if (notifier is InProcessChangesNotifier inproc) await inproc.ProcessPending(runner);
    }
  }

  // everything is read through the storage interfaces and sorted, so the result does not depend on store ordering
  private async Task<object> CaptureState() {
    var since = TestingDefaults.DefaultStartDt.AddYears(-1);
    var functions = new List<IRunnableFunction> { crm_read, crm_promote, crm_write, fin_read, fin_promote, fin_write };
    var pairs = new List<(SystemName System, SystemEntityTypeName SystemType, CoreEntityTypeName CoreType, List<SystemEntityId> SystemIds)> {
      (SimulationConstants.CRM_SYSTEM, SystemEntityTypeName.From<CrmMembershipType>(), CoreEntityTypeName.From<CoreMembershipType>(), crmdb.MembershipTypes.Select(e => e.SystemId).ToList()),
      (SimulationConstants.CRM_SYSTEM, SystemEntityTypeName.From<CrmCustomer>(), CoreEntityTypeName.From<CoreCustomer>(), crmdb.Customers.Select(e => e.SystemId).ToList()),
      (SimulationConstants.CRM_SYSTEM, SystemEntityTypeName.From<CrmInvoice>(), CoreEntityTypeName.From<CoreInvoice>(), crmdb.Invoices.Select(e => e.SystemId).ToList()),
      (SimulationConstants.FIN_SYSTEM, SystemEntityTypeName.From<FinAccount>(), CoreEntityTypeName.From<CoreCustomer>(), findb.Accounts.Select(e => e.SystemId).ToList()),
      (SimulationConstants.FIN_SYSTEM, SystemEntityTypeName.From<FinInvoice>(), CoreEntityTypeName.From<CoreInvoice>(), findb.Invoices.Select(e => e.SystemId).ToList())
    };
    var coretypes = pairs.Select(p => p.CoreType).Distinct().ToList();
    
    return new {
      Runs = recorder.TakeRuns(),
      Crm = new { MembershipTypes = Sorted(crmdb.MembershipTypes), Customers = Sorted(crmdb.Customers), Invoices = Sorted(crmdb.Invoices) },
      Fin = new { Accounts = Sorted(findb.Accounts), Invoices = Sorted(findb.Invoices) },
      Core = await coretypes.Select(CoreEntities).Synchronous(),
      Staged = await pairs.Select(async p => (await ctx.StageRepository.GetAll(p.System, p.SystemType, since))
          .Select(s => new { s.Id, s.DateStaged, s.DatePromoted, Checksum = s.StagedEntityChecksum.Value })
          .OrderBy(s => s.Id.ToString("D"), StringComparer.Ordinal)
          .ToList()).Synchronous(),
      States = await functions.Select(FunctionStates).Synchronous(),
      Changes = await coretypes.Select(async coretype => (await ctx.CtlRepo.GetEntityChanges(coretype, since))
          .Select(DtoHelpers.ToDto<EntityChange, EntityChange.Dto>)
          .OrderBy(c => c.ChangeDate).ThenBy(c => Json.Serialize(c), StringComparer.Ordinal)
          .ToList()).Synchronous(),
      Maps = await pairs.Select(async p => (await ctx.CtlRepo.GetMapsFromSystemIds(p.System, p.CoreType, p.SystemIds))
          .Select(DtoHelpers.ToDto<Map.CoreToSysMap, Map.CoreToSysMap.Dto>)
          .OrderBy(m => m.SystemId, StringComparer.Ordinal)
          .ToList()).Synchronous()
    };
    
    List<string> Sorted<T>(List<T> sysents) where T : ISystemEntity => sysents
        .OrderBy(e => e.SystemId.Value, StringComparer.Ordinal)
        .Select(e => Json.Serialize(e))
        .ToList();
    
    async Task<List<string>> CoreEntities(CoreEntityTypeName coretype) {
      var coreids = coretype.Value switch {
        nameof(CoreMembershipType) => (await ctx.CoreStore.GetMembershipTypes()).Select(e => e.CoreId).ToList(),
        nameof(CoreCustomer) => (await ctx.CoreStore.GetCustomers()).Select(e => e.CoreId).ToList(),
        _ => (await ctx.CoreStore.GetInvoices()).Select(e => e.CoreId).ToList()
      };
      return (await ctx.CoreStore.GetExistingEntities(coretype, coreids))
          .Select(e => Json.Serialize(e.ToDtos()))
          .Order(StringComparer.Ordinal)
          .ToList();
    }
    
    async Task<object> FunctionStates(IRunnableFunction func) {
      var system = await ctx.CtlRepo.GetSystemState(func.System, func.Stage) ?? throw new Exception();
      var objects = await func.Config.Operations.Select(async op => await ctx.CtlRepo.GetObjectState(system, op.Object)).Synchronous();
      return new { 
        System = DtoHelpers.ToDto<SystemState, SystemState.Dto>(system), 
        Objects = objects.Select(o => o is null ? null : DtoHelpers.ToDto<ObjectState, ObjectState.Dto>(o)).ToList() 
      };
    }
  }

  private void RandomTimeStep() => ctx.Clock.Tick(new TimeSpan(ctx.Rng.Next(0, 2), ctx.Rng.Next(0, 24), ctx.Rng.Next(0, 60), ctx.Rng.Next(0, 60)));

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