using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;

namespace Centazio.Core.Write;


// todo: move containers to Containers.ts

public static class MapEnumerableExtentionMethods {
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingCreateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndCreatedMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingUpdateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndUpdatedMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreSysAndPendingCreateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreSystemMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<E> ToCore<E>(this List<CoreAndPendingCreateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndCreatedMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndPendingUpdateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndUpdatedMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreSysAndPendingCreateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreSystemMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  
  public static List<E> ToSysEnt<E>(this List<CoreSysAndPendingCreateMap> maps) where E : ISystemEntity => maps.Select(m => m.SysEnt.To<E>()).ToList();
  public static List<E> ToSysEnt<E>(this List<CoreSystemMap> maps) where E : ISystemEntity => maps.Select(m => m.SysEnt.To<E>()).ToList();
  
  public static List<CoreAndCreatedMap> SuccessCreate<E>(this List<CoreSysAndPendingCreateMap> maps, List<E> created, IChecksumAlgorithm checksum) where E : ISystemEntity => 
      maps.Select((m, idx) => new CoreAndCreatedMap(m.Core, m.Map.SuccessCreate(created[idx].SystemId, checksum.Checksum(created[idx])))).ToList();

  public static List<CoreAndUpdatedMap> SuccessUpdate<E>(this List<CoreSystemMap> maps, List<E> updated, IChecksumAlgorithm checksum) where E : ISystemEntity =>
      maps.Select((m, idx) => m.Updated(checksum.Checksum(updated[idx]))).ToList();
}

public record CoreAndPendingCreateMap(ICoreEntity Core, CoreToSystemMap.PendingCreate Map) {
  public CoreSysAndPendingCreateMap AddSystemEntity(ISystemEntity sysent, SystemEntityChecksum checksum) {
    return new (Core, sysent, Map, checksum);
  }
}

public record CoreSysAndPendingCreateMap(ICoreEntity Core, ISystemEntity SysEnt, CoreToSystemMap.PendingCreate Map, SystemEntityChecksum Checksum) {
  public CoreAndCreatedMap Created(string targetid) => new(Core, Map.SuccessCreate(targetid, Checksum));
}

public record CoreAndCreatedMap {
  public ICoreEntity Core { get; }
  public CoreToSystemMap.Created Map { get; }
  
  internal CoreAndCreatedMap(ICoreEntity core, CoreToSystemMap.Created map) {
    Core = core;
    Map = map;
  }
}

public record CoreAndPendingUpdateMap(ICoreEntity Core, CoreToSystemMap.PendingUpdate Map) {
  public CoreSystemMap SetSystemEntity(ISystemEntity system) => new(Core, system, Map);
}

public record CoreSystemMap(ICoreEntity Core, ISystemEntity SysEnt, CoreToSystemMap.PendingUpdate Map) {
  public CoreAndUpdatedMap Updated(SystemEntityChecksum checksum) => new(Core, Map.SuccessUpdate(checksum));
}

public record CoreAndUpdatedMap {
  public ICoreEntity Core { get; }
  public CoreToSystemMap.Updated Map { get; }
  
  internal CoreAndUpdatedMap(ICoreEntity core, CoreToSystemMap.Updated map) {
    Core = core;
    Map = map;
  }
}

public record WriteOperationConfig(
    CoreEntityType CoreEntityType, 
    ValidCron Cron,
    ITargetSystemWriter TargetSysWriter) : OperationConfig(CoreEntityType, Cron), ILoggable {

  // ReSharper disable once RedundantExplicitPositionalPropertyDeclaration
  public CoreEntityType CoreEntityType { get; init; } = CoreEntityType;
  
  public object LoggableValue => $"{CoreEntityType.Value}";

}

// SingleWriteOperationConfig/IWriteSingleEntityToTargetSystem - used when target system only writes one entity at a time

public interface ITargetSystemWriter {
  Task<(List<CoreSysAndPendingCreateMap>, List<CoreSystemMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate);
  Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSysAndPendingCreateMap> tocreate, List<CoreSystemMap> toupdate);

}
