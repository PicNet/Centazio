﻿using Centazio.Core;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmReadFunction(SimulationCtx ctx, CrmApi api) : ReadFunction(SimulationConstants.CRM_SYSTEM, ctx.StageRepository, ctx.CtlRepo) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new(SystemEntityTypeName.From<CrmMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmMembershipTypeUpdates),
    new(SystemEntityTypeName.From<CrmCustomer>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmCustomerUpdates),
    new(SystemEntityTypeName.From<CrmInvoice>(), TestingDefaults.CRON_EVERY_SECOND, GetCrmInvoiceUpdates)
  ]);

  public async Task<ReadOperationResult> GetCrmMembershipTypeUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetMembershipTypes(config.Checkpoint));
  public async Task<ReadOperationResult> GetCrmCustomerUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetCustomers(config.Checkpoint));
  public async Task<ReadOperationResult> GetCrmInvoiceUpdates(OperationStateAndConfig<ReadOperationConfig> config) => GetUpdatesImpl(config, await api.GetInvoices(config.Checkpoint));

  private ReadOperationResult GetUpdatesImpl(OperationStateAndConfig<ReadOperationConfig> config, List<string> updates) {
    if (updates.Any()) ctx.Debug($"CrmReadFunction.GetUpdatesAfterCheckpoint[{config.State.Object.Value}] Updates[{updates.Count}]:\n\t{String.Join("\n\t", updates)}");
    return ReadOperationResult.Create(updates);
  }
}