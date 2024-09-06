using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract class AbstractReadFunction(IOperationsFilterAndPrioritiser<ReadOperationConfig>? prioritiser = null) 
    : AbstractFunction<ReadOperationConfig>(prioritiser);