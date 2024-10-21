using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;


public record WriteOperationConfig(
    CoreEntityTypeName CoreEntityTypeName, 
    ValidCron Cron,
    ITargetSystemWriter TargetSysWriter) : OperationConfig(CoreEntityTypeName, Cron), ILoggable {
  
  public string LoggableValue => $"{CoreEntityTypeName.Value}";
}

public record CovertCoreEntitiesToSystemEntitiesResult(List<CoreSystemAndPendingCreateMap> ToCreate, List<CoreSystemAndPendingUpdateMap> ToUpdate);

public interface ITargetSystemWriter {
  Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
  Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);

}
