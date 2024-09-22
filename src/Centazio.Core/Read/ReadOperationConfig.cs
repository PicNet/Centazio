using Centazio.Core.Runner;

namespace Centazio.Core.Read;

public interface IGetObjectsToStage {
  Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig> config);
}

public record ReadOperationConfig(ObjectName Object, ValidCron Cron, IGetObjectsToStage GetObjectsToStage) 
        : OperationConfig(Object, Cron);
        