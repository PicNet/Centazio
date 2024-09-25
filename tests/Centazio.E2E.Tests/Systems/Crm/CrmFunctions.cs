using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly ICrmSystemApi api;
  
  public CrmReadFunction(ICrmSystemApi api) {
    this.api = api;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Read, new ([
      new (nameof(CMembershipType), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CCustomer), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CInvoice), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public async Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig> config) => 
    config.State.Object.Value switch { 
      nameof(CMembershipType) => ReadOperationResult.Create(await api.GetMembershipTypes(config.Checkpoint)), 
      nameof(CCustomer) => ReadOperationResult.Create(await api.GetCustomers(config.Checkpoint)), 
      nameof(CInvoice) => ReadOperationResult.Create(await api.GetInvoices(config.Checkpoint)), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
}

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig<CoreCustomer>, PromoteOperationResult<CoreCustomer>>, IEvaluateEntitiesToPromote<CoreCustomer> {

  public override FunctionConfig<PromoteOperationConfig<CoreCustomer>> Config { get; }
  public Task<PromoteOperationResult<CoreCustomer>> Evaluate(OperationStateAndConfig<PromoteOperationConfig<CoreCustomer>> config, IEnumerable<StagedEntity> staged) => throw new NotImplementedException();

  public CrmPromoteFunction() {
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Promote, new ([
      new (nameof(CoreCustomer), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

}