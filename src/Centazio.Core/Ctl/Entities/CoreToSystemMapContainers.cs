using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

public static class MapEnumerableExtentionMethods {
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingCreateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndCreatedMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingUpdateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndUpdatedMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreSystemAndPendingCreateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreSystemAndPendingUpdateMap> maps) => maps.Select(m => m.Core).ToList();
  public static List<E> ToCore<E>(this List<CoreAndPendingCreateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndCreatedMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndPendingUpdateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreAndUpdatedMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreSystemAndPendingCreateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  public static List<E> ToCore<E>(this List<CoreSystemAndPendingUpdateMap> maps) where E : ICoreEntity => maps.Select(m => m.Core.To<E>()).ToList();
  
  public static List<E> ToSysEnt<E>(this List<CoreSystemAndPendingCreateMap> maps) where E : ISystemEntity => maps.Select(m => m.SysEnt.To<E>()).ToList();
  public static List<E> ToSysEnt<E>(this List<CoreSystemAndPendingUpdateMap> maps) where E : ISystemEntity => maps.Select(m => m.SysEnt.To<E>()).ToList();
  
  public static List<CoreAndCreatedMap> SuccessCreate<E>(this List<CoreSystemAndPendingCreateMap> maps, List<E> created, IChecksumAlgorithm checksum) where E : ISystemEntity => 
      maps.Select((m, idx) => new CoreAndCreatedMap(m.Core, m.Map.SuccessCreate(created[idx].SystemId, checksum.Checksum(created[idx])))).ToList();

  public static List<CoreAndUpdatedMap> SuccessUpdate<E>(this List<CoreSystemAndPendingUpdateMap> maps, List<E> updated, IChecksumAlgorithm checksum) where E : ISystemEntity =>
      maps.Select((m, idx) => m.SuccessUpdate(checksum.Checksum(updated[idx]))).ToList();
}

public record CoreAndPendingCreateMap(ICoreEntity Core, Map.PendingCreate Map) {
  public CoreSystemAndPendingCreateMap AddSystemEntity(ISystemEntity sysent) => new(Core, sysent, Map);
}

public record CoreSystemAndPendingCreateMap(ICoreEntity Core, ISystemEntity SysEnt,  Map.PendingCreate Map) {
  public CoreAndCreatedMap SuccessCreate(string targetid, SystemEntityChecksum checksum) => new(Core, Map.SuccessCreate(targetid, checksum));

}

public record CoreAndPendingUpdateMap(ICoreEntity Core,  Map.PendingUpdate Map) {
  public CoreSystemAndPendingUpdateMap AddSystemEntity(ISystemEntity system) => new(Core, system, Map);
}

public record CoreSystemAndPendingUpdateMap(ICoreEntity Core, ISystemEntity SysEnt,  Map.PendingUpdate Map) {
  public CoreAndUpdatedMap SuccessUpdate(SystemEntityChecksum checksum) => new(Core, Map.SuccessUpdate(checksum));

}

public record CoreAndCreatedMap(ICoreEntity Core, Map.Created Map);
public record CoreAndUpdatedMap(ICoreEntity Core, Map.Updated Map);
