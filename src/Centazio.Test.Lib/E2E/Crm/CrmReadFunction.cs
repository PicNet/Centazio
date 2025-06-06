﻿using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmReadFunction(SimulationCtx ctx, CrmApi api) : ReadFunction(SimulationConstants.CRM_SYSTEM, ctx.StageRepository, ctx.CtlRepo) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(SystemEntityTypeName.From<CrmMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmMembershipTypeUpdates),
    new ReadOperationConfig(SystemEntityTypeName.From<CrmCustomer>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmCustomerUpdates),
    new ReadOperationConfig(SystemEntityTypeName.From<CrmInvoice>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmInvoiceUpdates)
  ]);

  public async Task<ReadOperationResult> GetCrmMembershipTypeUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetMembershipTypes(config.Checkpoint));
  public async Task<ReadOperationResult> GetCrmCustomerUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetCustomers(config.Checkpoint));
  public async Task<ReadOperationResult> GetCrmInvoiceUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetInvoices(config.Checkpoint));

  private ReadOperationResult GetUpdatesImpl(OperationStateAndConfig<ReadOperationConfig> config, List<string> updates) {
    ctx.Debug($"CrmReadFunction.GetUpdatesAfterCheckpoint[{config.OpConfig.Object.Value}] Updates[{updates.Count}]", updates);
    return updates.Any() ? ReadOperationResult.Create(updates, UtcDate.UtcNow) : ReadOperationResult.EmptyResult();
  }
}