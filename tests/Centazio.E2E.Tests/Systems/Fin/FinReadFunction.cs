using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinApi api;
  
  public FinReadFunction(SimulationCtx ctx, FinApi api) {
    this.ctx = ctx;
    this.api = api;
    Config = new(nameof(FinApi), LifecycleStage.Defaults.Read, [
      new(SystemEntityType.From<FinAccount>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(SystemEntityType.From<FinInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(FinAccount) => await api.GetAccounts(config.Checkpoint), 
      nameof(FinInvoice) => await api.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    }; 
    ctx.Debug($"FinReadFunction[{config.OpConfig.Object.Value}] Updates[{updates.Count}]");
    return ReadOperationResult.Create(updates);
  }
}