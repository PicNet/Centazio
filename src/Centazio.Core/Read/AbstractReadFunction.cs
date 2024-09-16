using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract class AbstractReadFunction(IOperationsFilterAndPrioritiser<ReadOperationConfig>? prioritiser = null) 
    : AbstractFunction<ReadOperationConfig, ReadOperationResult>(prioritiser);
    
public record ReadOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> GetObjectsToStage) 
        : OperationConfig(Object, Cron, FirstTimeCheckpoint);
        