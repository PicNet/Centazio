using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public record WriteOperationConfig(
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    Func<WriteOperationConfig, List<CoreAndPendingCreateMap>, List<CoreAndPendingUpdateMap>, Task<CovertCoreEntitiesToSystemEntitiesResult>> CovertCoreEntitiesToSystemEntities,
    Func<WriteOperationConfig, List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>, Task<WriteOperationResult>> WriteEntitiesToTargetSystem) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
}

public record CovertCoreEntitiesToSystemEntitiesResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);
