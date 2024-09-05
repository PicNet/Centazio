using Centazio.Core.Runner;

namespace Centazio.Core.Tests.Promote;

public abstract class AbstractPromoteFunction(IOperationsFilterAndPrioritiser<PromoteOperationConfig>? prioritiser = null) 
    : AbstractFunction<PromoteOperationConfig>(prioritiser);