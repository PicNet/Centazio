using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinReadFunction(SimulationCtx ctx, FinApi api) : ReadFunction(SimulationConstants.FIN_SYSTEM, ctx.StageRepository, ctx.CtlRepo) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new(SystemEntityTypeName.From<FinAccount>(), TestingDefaults.CRON_EVERY_SECOND, GetFinAccountUpdates),
    new(SystemEntityTypeName.From<FinInvoice>(), TestingDefaults.CRON_EVERY_SECOND, GetFinInvoiceUpdates)
  ]);
  
  public async Task<ReadOperationResult> GetFinAccountUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetAccounts(config.Checkpoint));
  public async Task<ReadOperationResult> GetFinInvoiceUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetInvoices(config.Checkpoint));
  
  private ReadOperationResult GetUpdatesImpl(OperationStateAndConfig<ReadOperationConfig> config, List<string> updates) {
    ctx.Debug($"FinReadFunction.GetUpdatesAfterCheckpoint[{config.State.Object.Value}] Updates[{updates.Count}]:\n\t{String.Join("\n\t", updates)}");
    return updates.Any() ? ReadOperationResult.Create(updates, UtcDate.UtcNow) : ReadOperationResult.EmptyResult();
  }
}