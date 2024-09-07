using Centazio.Core.Runner;

namespace Centazio.Core.Promote;

public abstract class AbstractPromoteFunction(IOperationsFilterAndPrioritiser<PromoteOperationConfig>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>(prioritiser);