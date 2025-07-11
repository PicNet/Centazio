using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinReadFunction(SimulationCtx ctx, FinApi api) : ReadFunction(SC.Fin.SYSTEM_NAME, ctx.StageRepository, ctx.CtlRepo) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(SystemEntityTypeName.From<FinAccount>(), TestingDefaults.CRON_EVERY_SECOND, GetFinAccountUpdates),
    new ReadOperationConfig(SystemEntityTypeName.From<FinInvoice>(), TestingDefaults.CRON_EVERY_SECOND, GetFinInvoiceUpdates)
  ]);
  
  public async Task<ReadOperationResult> GetFinAccountUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetAccounts(config.Checkpoint));
  public async Task<ReadOperationResult> GetFinInvoiceUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetInvoices(config.Checkpoint));
  
  private ReadOperationResult GetUpdatesImpl(OperationStateAndConfig<ReadOperationConfig> config, List<RawJsonData> updates) {
    ctx.Debug($"FinReadFunction.GetUpdatesAfterCheckpoint[{config.State.Object.Value}] Updates[{updates.Count}]", updates.Select(e => e.Json).ToList());
    return updates.Any() ? ReadOperationResult.Create(updates, UtcDate.UtcNow) : ReadOperationResult.EmptyResult();
  }
}