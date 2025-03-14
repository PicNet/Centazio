namespace Centazio.Core.Write;

public delegate Task<CovertCoresToSystemsResult> CovertCoreEntitiesToSystemEntitiesHandler(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
public delegate Task<WriteOperationResult> WriteEntitiesToTargetSystemHandler(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);

public record CovertCoresToSystemsResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);

public record WriteOperationConfig(
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    CovertCoreEntitiesToSystemEntitiesHandler CovertCoreEntitiesToSystemEntities,
    WriteEntitiesToTargetSystemHandler WriteEntitiesToTargetSystem) : OperationConfig(CoreEntityTypeName, [ (CoreEntityTypeName, LifecycleStage.Defaults.Promote) ], Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
}
