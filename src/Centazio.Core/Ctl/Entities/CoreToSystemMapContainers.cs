using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;

namespace Centazio.Core.Ctl.Entities;

// todo: remove types not required
public static class MapEnumerableExtentionMethods {
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingCreateMap> maps) => maps.Select(m => m.CoreEntity).ToList();
  public static List<ICoreEntity> ToCore(this List<CoreAndPendingUpdateMap> maps) => maps.Select(m => m.CoreEntity).ToList();
  
  public static List<E> ToSysEnt<E>(this List<CoreSystemAndPendingCreateMap> maps) where E : ISystemEntity => maps.Select(m => m.SystemEntity.To<E>()).ToList();
  public static List<E> ToSysEnt<E>(this List<CoreSystemAndPendingUpdateMap> maps) where E : ISystemEntity => maps.Select(m => m.SystemEntity.To<E>()).ToList();
  
  public static List<CoreAndCreatedMap> SuccessCreate<E>(this List<CoreSystemAndPendingCreateMap> maps, List<E> created, IChecksumAlgorithm checksum) where E : ISystemEntity => 
      maps.Select((m, idx) => new CoreAndCreatedMap(m.CoreEntity, m.Map.SuccessCreate(created[idx].SystemId, checksum.Checksum(created[idx])))).ToList();

  public static List<CoreAndUpdatedMap> SuccessUpdate<E>(this List<CoreSystemAndPendingUpdateMap> maps, List<E> updated, IChecksumAlgorithm checksum) where E : ISystemEntity =>
      maps.Select((m, idx) => m.SuccessUpdate(checksum.Checksum(updated[idx]))).ToList();
}

public record CoreAndPendingCreateMap(ICoreEntity CoreEntity, Map.PendingCreate Map) {
  public CoreSystemAndPendingCreateMap AddSystemEntity(ISystemEntity sysent) => new(CoreEntity, sysent, Map);
}

public record CoreSystemAndPendingCreateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingCreate Map) {
  public CoreAndCreatedMap SuccessCreate(SystemEntityId systemid, SystemEntityChecksum checksum) => new(CoreEntity, Map.SuccessCreate(systemid, checksum));

}

public record CoreAndPendingUpdateMap(ICoreEntity CoreEntity,  Map.PendingUpdate Map) {
  public CoreSystemAndPendingUpdateMap AddSystemEntity(ISystemEntity system) => new(CoreEntity, system, Map);
}

public record CoreSystemAndPendingUpdateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingUpdate Map) {
  public CoreAndUpdatedMap SuccessUpdate(SystemEntityChecksum checksum) => new(CoreEntity, Map.SuccessUpdate(checksum));

}

public record CoreAndCreatedMap(ICoreEntity CoreEntity, Map.Created Map);
public record CoreAndUpdatedMap(ICoreEntity CoreEntity, Map.Updated Map);
