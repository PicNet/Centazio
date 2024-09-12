using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public interface IEntityIntraSystemMappingStore : IAsyncDisposable {
  
  // todo: Upsert should be split into create/update so we can automatically set DateCreate/DateUpdated, etc.
  Task Upsert(EntityIntraSystemMapping map);
  Task Upsert(IEnumerable<EntityIntraSystemMapping> maps);
  
  Task<List<EntityIntraSystemMapping>> Get();
  
  /// <summary>
  /// Bounce backs are when an entity is created in System 1 and written to
  /// System 2.  These entities created in System 2 are then read and staged
  /// by Centazio.  During the promotion stage we need to filter these out
  /// as core storage already knows about this specific entity as should not
  /// really be considered as new.  Promiting this entity again would be a duplicate.
  ///
  /// The logic of identifying bound backs is as follows:
  /// - Entity is created in System 1, this is staged in Centazio and written to System 2.
  ///   When writing we create an EntityIntraSystemMapping entry with the following attributes:
  ///     - SourceSystem/ID: System 1 / System 1 ID
  ///     - TargetSystem/ID: System 2 / System 2 ID
  /// - Entity is created in System 2 and staged in Centazio.
  /// - When promoting this newly staged entity from System 2 we need to filter it out be checking
  ///   the EntityIntraSystemMapping for any entities that:
  ///     - TargetSystem is System 2
  ///     - TargetID is the current entities SourceId
  ///
  /// Sample scenario diagram:
  /// https://sequencediagram.org/index.html#initialData=C4S2BsFMAIGECUCy0C0A+aAxEA7AhjgMYh7gDOAXNAEID2ArkTNXoQNbQDKeAtgA5QAUAmTo4SKgEkcAN1ohCMABQiAjACYAzAEpohAE6Q8wSABNhSVBliQcwPAC8QtKbPmLoKpBp3Qy9gHMzYVt7J1p0GztHZ1c5BRg+fVoeWhNzKLDndGx8IhJyOPcYAHd9MBMcTzUtaAAjSEIUyDIsXE11VWhcNrziUjJtAB0cAFE7MABPRDw+PlwAr0QAGmhJH1XczfbO7UFcgn7ySNCYl16Orv88IIzT8JPo8KkAnFpDaCSUtIWLzug8K0wK08NBTPQBApjJAAHQjAAitBwMDqkz0AAtGmxfuNQMBprN5jgAtAAGbvXrLXKXIA
  /// </summary>
  /// <returns>The list of IDs that are not bouce backs and should be promoted</returns>
  Task<List<string>> FilterOutBouncedBackIds<C>(SystemName thissys, List<string> ids) where C : ICoreEntity;

}

public abstract class AbstractEntityIntraSystemMappingStore : IEntityIntraSystemMappingStore {
  
  public Task Upsert(EntityIntraSystemMapping map) => Upsert([map]);
  
  public abstract Task Upsert(IEnumerable<EntityIntraSystemMapping> maps);
  public abstract Task<List<EntityIntraSystemMapping>> Get();
  public abstract Task<List<string>> FilterOutBouncedBackIds<C>(SystemName thissys, List<string> ids) where C : ICoreEntity;
  
  public abstract ValueTask DisposeAsync();

}