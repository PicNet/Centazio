using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Write;

namespace Centazio.Core.EntitySysMapping;

public record GetForCoresResult(List<CoreAndPendingCreateMap> Created, List<CoreAndPendingUpdateMap> Updated) {
  public bool Empty => !Created.Any() && !Updated.Any();
}

public interface IEntityIntraSystemMappingStore : IAsyncDisposable {
  
  Task<EntityIntraSysMap.Created> Create(EntityIntraSysMap.Created create);
  Task<List<EntityIntraSysMap.Created>> Create(ICollection<EntityIntraSysMap.Created> maps);
  
  Task<EntityIntraSysMap.Updated> Update(EntityIntraSysMap.Updated map);
  Task<List<EntityIntraSysMap.Updated>> Update(ICollection<EntityIntraSysMap.Updated> maps);
  
  Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key);
  
  // todo: can this be removed and replaced with more explicit methods
  // such as ones below (FindTargetIds)
  Task<GetForCoresResult> GetForCores(ICollection<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName target, ICollection<string> coreids);
  
  /// <summary>
  /// Bounce backs are when an entity is created in System 1 and written to
  /// System 2.  These entities created in System 2 are then read and staged
  /// by Centazio.  During the promotion stage we need to filter these out
  /// as core storage already knows about this specific entity as should not
  /// really be considered as new.  Promoting this entity again would be a duplicate.
  ///
  /// The logic of identifying bound backs is as follows:
  /// - Entity is created in System 1, this is staged in Centazio and written to System 2.
  ///   When writing we create an EntityIntraSystemMapping entry with the following attributes:
  ///     - SourceSystem/ID: System 1 / System 1 ID
  ///     - TargetSystem/ID: System 2 / System 2 ID
  /// - Entity is created in System 2 and then staged in Centazio.  This entity cannot be ignored
  ///   at this stage as staged entities have no details of what is being staged, so we cannot filter
  ///   out by id or dates, etc.
  /// - When promoting this newly staged entity from System 2 we need to filter it out be checking
  ///   the EntityIntraSystemMapping for any entities that:
  ///     - TargetSystem is System 2
  ///     - TargetID is the current entities SourceId
  ///     - LastSuccess is less than X seconds ago.  Anything greater than this and we assume that
  ///       it is a valid edit made in System 2 that has to go back to System 1
  ///
  /// Sample scenario diagram:
  /// https://sequencediagram.org/index.html#initialData=C4S2BsFMAIGECUCy0C0A+aAxEA7AhjgMYh7gDOAXNAEID2ArkTNXoQNbQDKeAtgA5QAUAmTo4SKgEkcAN1ohCMABQiAjACYAzAEpohAE6Q8wSABNhSVBliQcwPAC8QtKbPmLoKpBp3Qy9gHMzYVt7J1p0GztHZ1c5BRg+fVoeWhNzKLDndGx8IhJyOPcYAHd9MBMcTzUtaAAjSEIUyDIsXE11VWhcNrziUjJtAB0cAFE7MABPRDw+PlwAr0QAGmhJH1XczfbO7UFcgn7ySNCYl16Orv88IIzT8JPo8KkAnFpDaCSUtIWLzug8K0wK08NBTPQBApjJAAHQjAAitBwMDqkz0AAtGmxfuNQMBprN5jgAtAAGbvXrLXKXIA
  /// </summary>
  /// <returns>The list of IDs that are not bouce backs and should be promoted</returns>
  Task<List<string>> FilterOutBouncedBackIds(SystemName thissys, CoreEntityType obj, List<string> ids);

  /// <summary>
  /// Gets the CoreId for a specific `CoreEntityType` given its `TargetId` and `TargetSystem`.
  /// </summary>
  Task<string?> GetCoreIdForTargetSys(CoreEntityType obj, string targetid, SystemName targetsys);

}

public abstract class AbstractEntityIntraSystemMappingStore : IEntityIntraSystemMappingStore {
  
  public async Task<EntityIntraSysMap.Created> Create(EntityIntraSysMap.Created create) => (await Create([create])).Single();
  public abstract Task<List<EntityIntraSysMap.Created>> Create(ICollection<EntityIntraSysMap.Created> creates);
  
  public async Task<EntityIntraSysMap.Updated> Update(EntityIntraSysMap.Updated update) => (await Update([update])).Single();
  public abstract Task<List<EntityIntraSysMap.Updated>> Update(ICollection<EntityIntraSysMap.Updated> updates);
  
  public abstract Task<EntityIntraSysMap> GetSingle(EntityIntraSysMap.MappingKey key);
  public abstract Task<GetForCoresResult> GetForCores(ICollection<ICoreEntity> cores, SystemName target, CoreEntityType obj);
  public abstract Task<List<EntityIntraSysMap>> FindTargetIds(CoreEntityType coretype, SystemName target, ICollection<string> coreids);
  public abstract Task<List<EntityIntraSysMap>> GetAll();
  
  public abstract Task<List<string>> FilterOutBouncedBackIds(SystemName thissys, CoreEntityType obj, List<string> ids);
  public abstract Task<string?> GetCoreIdForTargetSys(CoreEntityType obj, string targetid, SystemName targetsys);
  public abstract ValueTask DisposeAsync();

}