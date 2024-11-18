using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;

public delegate Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreEntitiesToSystemEntitiesHandler(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
public delegate Task<WriteOperationResult> WriteEntitiesToTargetSystemHandler(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);

public record CovertCoreEntitiesToSystemEntitiesResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);

public record WriteOperationConfig(
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    CovertCoreEntitiesToSystemEntitiesHandler CovertCoreEntitiesToSystemEntities,
    WriteEntitiesToTargetSystemHandler WriteEntitiesToTargetSystem) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
}
