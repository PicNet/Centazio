using Centazio.Core.Ctl;
using Centazio.Core.Runner;

namespace Centazio.Core.Tests.Read;

public abstract class AbstractReadFunction(
        ICtlRepository ctl, 
        FunctionConfig<ReadOperationConfig> cfg, 
        IOperationRunner<ReadOperationConfig> runner, 
        IOperationsFilterAndPrioritiser<ReadOperationConfig>? prioritiser = null) 
    : AbstractFunction<ReadOperationConfig>(ctl, cfg, runner, prioritiser);