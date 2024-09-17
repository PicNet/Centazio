using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public abstract class AbstractReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>;
    
public record ReadOperationConfig(ObjectName Object, ValidCron Cron, DateTime FirstTimeCheckpoint, Func<OperationStateAndConfig<ReadOperationConfig>, Task<ReadOperationResult>> GetObjectsToStage) 
        : OperationConfig(Object, Cron, FirstTimeCheckpoint);
        