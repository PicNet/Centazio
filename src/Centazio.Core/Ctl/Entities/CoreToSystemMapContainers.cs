using Centazio.Core.Checksum;
using Centazio.Core.Types;

namespace Centazio.Core.Ctl.Entities;

public record CoreAndPendingCreateMap(ICoreEntity CoreEntity, Map.PendingCreate Map) {
  public CoreSystemAndPendingCreateMap AddSystemEntity(ISystemEntity sysent, IChecksumAlgorithm checksum) => new(CoreEntity, sysent, Map, checksum);
}

public record CoreAndPendingUpdateMap(ICoreEntity CoreEntity,  Map.PendingUpdate Map) {
  public CoreSystemAndPendingUpdateMap AddSystemEntity(ISystemEntity system, IChecksumAlgorithm checksum) => new(CoreEntity, system, Map, checksum);
}

public record CoreSystemAndPendingCreateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity, Map.PendingCreate Map, IChecksumAlgorithm checksum) {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Created SuccessCreate(SystemEntityId newid) => Map.SuccessCreate(newid, checksum.Checksum(SystemEntity.CreatedWithId(newid)));
  
  internal CoreSystemAndPendingCreateMap<C, S> To<C, S>() where C : ICoreEntity where S : ISystemEntity => new(CoreEntity.To<C>(), SystemEntity.To<S>(), Map, checksum);
}

public record CoreSystemAndPendingCreateMap<C, S>(C CoreEntity, S SystemEntity, Map.PendingCreate Map, IChecksumAlgorithm checksum) where C : ICoreEntity where S : ISystemEntity {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Created SuccessCreate(SystemEntityId newid) => Map.SuccessCreate(newid, checksum.Checksum(SystemEntity.CreatedWithId(newid)));
}

public record CoreSystemAndPendingUpdateMap(ICoreEntity CoreEntity, ISystemEntity SystemEntity,  Map.PendingUpdate Map, IChecksumAlgorithm checksum) {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Updated SuccessUpdate() => Map.SuccessUpdate(checksum.Checksum(SystemEntity));
  
  internal CoreSystemAndPendingUpdateMap<C, S> To<C, S>() where C : ICoreEntity where S : ISystemEntity => new(CoreEntity.To<C>(), SystemEntity.To<S>(), Map, checksum);
}

public record CoreSystemAndPendingUpdateMap<C, S>(C CoreEntity, S SystemEntity,  Map.PendingUpdate Map, IChecksumAlgorithm checksum) where C : ICoreEntity where S : ISystemEntity {
  private IChecksumAlgorithm checksum { get; } = checksum;
  
  public Map.Updated SuccessUpdate() => Map.SuccessUpdate(checksum.Checksum(SystemEntity)); 
}

public static class ConvenienceExtensions {
  public static List<CoreSystemAndPendingCreateMap<C, S>> To<C, S>(this List<CoreSystemAndPendingCreateMap> lst) where C : ICoreEntity where S : ISystemEntity => lst.Select(t => t.To<C, S>()).ToList();
  public static List<CoreSystemAndPendingUpdateMap<C, S>> To<C, S>(this List<CoreSystemAndPendingUpdateMap> lst) where C : ICoreEntity where S : ISystemEntity => lst.Select(t => t.To<C, S>()).ToList();
}