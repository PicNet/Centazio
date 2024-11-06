﻿using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinReadFunction : ReadFunction {

  protected override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinApi api;
  
  public FinReadFunction(SimulationCtx ctx, FinApi api) : base (ctx.StageRepository, ctx.CtlRepo) {
    this.ctx = ctx;
    this.api = api;
    Config = new(new(nameof(FinApi)), LifecycleStage.Defaults.Read, [
      new(SystemEntityTypeName.From<FinAccount>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(SystemEntityTypeName.From<FinInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }
  
  public override async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(FinAccount) => await api.GetAccounts(config.Checkpoint), 
      nameof(FinInvoice) => await api.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    }; 
    if (updates.Any()) ctx.Debug($"FinReadFunction.GetUpdatesAfterCheckpoint[{config.OpConfig.Object.Value}] Updates[{updates.Count}]:\n\t{String.Join("\n\t", updates)}");
    return ReadOperationResult.Create(updates);
  }
}