using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public interface ICoreToSystemMapStore : IAsyncDisposable {

  Task<List<CoreToSystemMap.Created>> Create(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Created> maps);
  Task<List<CoreToSystemMap.Updated>> Update(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Updated> maps);
  
  Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName system);
  Task<List<CoreToSystemMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName system);
  Task<List<CoreToSystemMap>> GetExistingMappingsFromSystemIds(CoreEntityType coretype, List<string> sysids, SystemName system);

  /// <summary>
  /// Gets a map from SourceId to the correct CoreId for potential duplicate entities.  These potential
  /// duplicates can only happen for Bi-directional entities that can bounce back when written to
  /// a target system.
  /// </summary>
  Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system);

}

public abstract class AbstractCoreToSystemMapStore : ICoreToSystemMapStore {

  public abstract Task<List<CoreToSystemMap.Created>> Create(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Created> creates);
  public abstract Task<List<CoreToSystemMap.Updated>> Update(CoreEntityType coretype, SystemName system, List<CoreToSystemMap.Updated> updates);
  public abstract Task<(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated)> GetNewAndExistingMappingsFromCores(List<ICoreEntity> cores, SystemName system);
  public abstract Task<List<CoreToSystemMap>> GetExistingMappingsFromCoreIds(CoreEntityType coretype, List<string> coreids, SystemName system);
  
  public abstract Task<List<CoreToSystemMap>> GetExistingMappingsFromSystemIds(CoreEntityType coretype, List<string> sysids, SystemName system);
  public abstract Task<Dictionary<string, string>> GetPreExistingSourceIdToCoreIdMap(List<ICoreEntity> potentialDups, SystemName system);
  public abstract ValueTask DisposeAsync();

}