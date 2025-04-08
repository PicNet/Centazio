namespace Centazio.Core.Write;

public delegate Task<ConvertCoresToSystemsResult> CovertCoreEntitiesToSystemEntitiesHandler(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
public delegate Task<WriteOperationResult> WriteEntitiesToTargetSystemHandler(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);

public record ConvertCoresToSystemsResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);

public record WriteOperationConfig(
    SystemName System, 
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    CovertCoreEntitiesToSystemEntitiesHandler CovertCoreEntitiesToSystemEntities,
    WriteEntitiesToTargetSystemHandler WriteEntitiesToTargetSystem) : OperationConfig(CoreEntityTypeName, [ new(new NotSystem(System), LifecycleStage.Defaults.Promote, CoreEntityTypeName) ], Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
  
  // Triggered by any PromoteFunction for same Object but from other System
  public override bool ShouldRunBasedOnTriggers(List<ObjectChangeTrigger> triggeredby) => 
      triggeredby.Any(t => t.Stage == LifecycleStage.Defaults.Promote && t.Object == Object && t.System != System);

}
