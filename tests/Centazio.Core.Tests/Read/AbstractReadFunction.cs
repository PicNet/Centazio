using Centazio.Core.Runner;

namespace Centazio.Core.Tests.Read;

public abstract class AbstractReadFunction(IOperationsFilterAndPrioritiser<ReadOperationConfig>? prioritiser = null) 
    : AbstractFunction<ReadOperationConfig>(prioritiser);