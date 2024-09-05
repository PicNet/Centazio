using Centazio.Core.Ctl;
using Centazio.Core.Runner;

namespace Centazio.Core.Tests.Promote;

public abstract class AbstractPromoteFunction(
        ICtlRepository ctl, 
        FunctionConfig<PromoteOperationConfig> cfg, 
        IOperationRunner<PromoteOperationConfig> runner, 
        IOperationsFilterAndPrioritiser<PromoteOperationConfig>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig>(ctl, cfg, runner, prioritiser);