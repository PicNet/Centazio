using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public record ConvertCoreEntitiesToSystemEntitiesArgs(WriteOperationConfig Config, List<CoreAndPendingCreateMap> ToCreate, List<CoreAndPendingUpdateMap> ToUpdate);
public record WriteEntitiesToTargetSystemArgs(WriteOperationConfig Config, List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);

public record WriteOperationConfig(
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    Func<ConvertCoreEntitiesToSystemEntitiesArgs, Task<CovertCoreEntitiesToSystemEntitiesResult>> CovertCoreEntitiesToSystemEntities,
    Func<WriteEntitiesToTargetSystemArgs, Task<WriteOperationResult>> WriteEntitiesToTargetSystem) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
}

public record CovertCoreEntitiesToSystemEntitiesResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);
