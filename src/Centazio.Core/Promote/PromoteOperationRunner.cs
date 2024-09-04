using Centazio.Core.Ctl;
using centazio.core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Func;

internal class PromoteOperationRunner(IEntityStager stager, ICtlRepository ctl) : IOperationRunner {
  

  public async Task<OperationResult> RunOperation(DateTime start, OperationStateAndConfig op) {
    var res = await op.Settings.Impl(start, op);
    
    return res;
  }
}