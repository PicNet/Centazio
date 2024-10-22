using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib.E2E.Crm;
using Centazio.Test.Lib.E2E.Fin;
using Serilog;
using Serilog.Events;

namespace Centazio.Test.Lib.E2E;

public class E2EEnvironment(ISimulationProvider provider) : IAsyncDisposable {

  private readonly SimulationCtx ctx = new(provider);
  
  private CrmApi crm = null!;
  private FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner = null!;
  private FunctionRunner<PromoteOperationConfig, PromoteOperationResult> crm_promote_runner = null!;
  private FunctionRunner<WriteOperationConfig, WriteOperationResult> crm_write_runner = null!;

  private FinApi fin = null!;
  private FunctionRunner<ReadOperationConfig, ReadOperationResult> fin_read_runner = null!;
  private FunctionRunner<PromoteOperationConfig, PromoteOperationResult> fin_promote_runner = null!;
  private FunctionRunner<WriteOperationConfig, WriteOperationResult> fin_write_runner = null!;

  public async Task Initialise() {
    await ctx.Initialise();
    
    (crm, fin) = (new CrmApi(ctx), new FinApi(ctx));
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new CrmReadFunction(ctx, crm),
        new ReadOperationRunner(ctx.StageRepository),
        ctx.CtlRepo);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new CrmPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.StageRepository, ctx.CoreStore, ctx.CtlRepo),
        ctx.CtlRepo);
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new CrmWriteFunction(ctx, crm),
        new WriteOperationRunner<WriteOperationConfig>(ctx.CtlRepo, ctx.CoreStore), 
        ctx.CtlRepo);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new FinReadFunction(ctx, fin),
        new ReadOperationRunner(ctx.StageRepository),
        ctx.CtlRepo);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new FinPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.StageRepository, ctx.CoreStore, ctx.CtlRepo),
        ctx.CtlRepo);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new FinWriteFunction(ctx, fin),
        new WriteOperationRunner<WriteOperationConfig>(ctx.CtlRepo, ctx.CoreStore), 
        ctx.CtlRepo);
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
    ctx.Epoch = new(epoch, ctx);
    RandomTimeStep();
    ctx.Debug($"epoch[{epoch}] starting - running simulation step [{{@Now}}]", UtcDate.UtcNow);
    
    crm.Simulation.Step();
    fin.Simulation.Step(); 
    
    ctx.Debug($"epoch[{epoch}] simulation step completed - running functions");
    
    await crm_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    await fin_read_runner.RunFunction(); 
    await fin_promote_runner.RunFunction();
    await crm_write_runner.RunFunction();
    await fin_write_runner.RunFunction();
    
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
    var core_types = (await ctx.CoreStore.GetMembershipTypes()).Select(m => ctx.Converter.CoreMembershipTypeToCrmMembershipType(Guid.Empty, m));
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []));
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedMemberships));
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_types, crm.MembershipTypes);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToCrmCustomer(Guid.Empty, c));
    var core_customers_for_fin = (await ctx.CoreStore.GetCustomers()).Select(c => ctx.Converter.CoreCustomerToFinAccount(0, c));
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>((SimulationConstants.CRM_SYSTEM, crm.Simulation.AddedCustomers), (SimulationConstants.FIN_SYSTEM, fin.Simulation.AddedAccounts));
    await ctx.Epoch.ValidateUpdated<CoreCustomer>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedCustomers), (SimulationConstants.FIN_SYSTEM, fin.Simulation.EditedAccounts));
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm.Customers);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin.Accounts);
  }
  
  private async Task CompareInvoices() {
    var cores = await ctx.CoreStore.GetInvoices();
    var crmmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.CRM_SYSTEM,  CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var finmaps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(SimulationConstants.FIN_SYSTEM, CoreEntityTypeName.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var core_invoices_for_crm = cores.Select(i => ctx.Converter.CoreInvoiceToCrmInvoice(Guid.Empty, i, crmmaps));
    var core_invoices_for_fin = cores.Select(i => ctx.Converter.CoreInvoiceToFinInvoice(0, i, finmaps));
    
    await ctx.Epoch.ValidateAdded<CoreInvoice>((SimulationConstants.CRM_SYSTEM, crm.Simulation.AddedInvoices), (SimulationConstants.FIN_SYSTEM, fin.Simulation.AddedInvoices));
    await ctx.Epoch.ValidateUpdated<CoreInvoice>((SimulationConstants.CRM_SYSTEM, crm.Simulation.EditedInvoices), (SimulationConstants.FIN_SYSTEM, fin.Simulation.EditedInvoices));
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_invoices_for_crm, crm.Invoices);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_invoices_for_fin, fin.Invoices);
  }
  
  [IgnoreNamingConventions] private void CompareByChecksum(SystemName system, IEnumerable<ISystemEntity> cores, IEnumerable<ISystemEntity> targets) {
    var (corecs, targetscs) = (cores.Select(c => Json.Serialize(c.GetChecksumSubset())).OrderBy(str => str).ToList(), targets.Select(t => Json.Serialize(t.GetChecksumSubset())).OrderBy(str => str).ToList());
    if (!corecs.SequenceEqual(targetscs)) throw new E2ETestFailedException($"[{system}] checksum comparison failed\ncore entities:\n\t{String.Join("\n\t", corecs)}\ntarget system entities:\n\t{String.Join("\n\t", targetscs)}");
  }
}