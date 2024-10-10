using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;


public record WriteOperationConfig(
    CoreEntityType CoreEntityType, 
    ValidCron Cron,
    ITargetSystemWriter TargetSysWriter) : OperationConfig(CoreEntityType, Cron), ILoggable {
  
  public object LoggableValue => $"{CoreEntityType.Value}";
}

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface ITargetSystemWriter {
  Task<(List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
  Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate);

}
