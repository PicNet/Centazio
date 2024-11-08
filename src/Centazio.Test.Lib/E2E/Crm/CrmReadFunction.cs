using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmReadFunction(SimulationCtx ctx, CrmApi api) : ReadFunction(SimulationConstants.CRM_SYSTEM, ctx.StageRepository, ctx.CtlRepo) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new(SystemEntityTypeName.From<CrmMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
    new(SystemEntityTypeName.From<CrmCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
    new(SystemEntityTypeName.From<CrmInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
  ]);
  
  public override async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(CrmMembershipType) => await api.GetMembershipTypes(config.Checkpoint), 
      nameof(CrmCustomer) => await api.GetCustomers(config.Checkpoint), 
      nameof(CrmInvoice) => await api.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
    if (updates.Any()) ctx.Debug($"CrmReadFunction.GetUpdatesAfterCheckpoint[{config.State.Object.Value}] Updates[{updates.Count}]:\n\t{String.Join("\n\t", updates)}");
    return ReadOperationResult.Create(updates);
  }
}