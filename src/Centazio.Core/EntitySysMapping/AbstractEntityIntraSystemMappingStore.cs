﻿using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.EntitySysMapping;

public abstract record NewEntityIntraSystemMapping(ICoreEntity CoreEntity, SystemName TargetSystem, ValidString TargetId, EEntityMappingStatus Status) {
  public EntityIntraSystemMapping CreateEntityIntraSystemMapping() => new(
      CoreEntity.GetType().Name, 
      CoreEntity.Id, 
      CoreEntity.SourceSystem, 
      CoreEntity.SourceId, 
      TargetSystem, 
      TargetId, 
      Status, 
      UtcDate.UtcNow,
      null,
      Status == EEntityMappingStatus.Success ? UtcDate.UtcNow : null);
}
public record NewSuccessIntraSystemMapping(ICoreEntity CoreEntity, SystemName TargetSystem, ValidString TargetId) : 
    NewEntityIntraSystemMapping(CoreEntity, TargetSystem, TargetId, EEntityMappingStatus.Success);

public record UpdateEntityIntraSystemMapping(EntityIntraSystemMapping.MappingKey Key, EEntityMappingStatus Status, string? Error = null);

public interface IEntityIntraSystemMappingStore : IAsyncDisposable {
  
  Task<EntityIntraSystemMapping> Create(NewEntityIntraSystemMapping create);
  Task<IEnumerable<EntityIntraSystemMapping>> Create(IEnumerable<NewEntityIntraSystemMapping> maps);
  
  Task<EntityIntraSystemMapping> Update(UpdateEntityIntraSystemMapping map);
  Task<IEnumerable<EntityIntraSystemMapping>> Update(IEnumerable<UpdateEntityIntraSystemMapping> maps);
  
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
  
  public async Task<EntityIntraSystemMapping> Create(NewEntityIntraSystemMapping create) => (await Create([create])).Single();
  public abstract Task<IEnumerable<EntityIntraSystemMapping>> Create(IEnumerable<NewEntityIntraSystemMapping> creates);
  
  public async Task<EntityIntraSystemMapping> Update(UpdateEntityIntraSystemMapping update) => (await Update([update])).Single();
  public abstract Task<IEnumerable<EntityIntraSystemMapping>> Update(IEnumerable<UpdateEntityIntraSystemMapping> updates);
  
  public abstract Task<List<EntityIntraSystemMapping>> Get();
  public abstract Task<List<string>> FilterOutBouncedBackIds<C>(SystemName thissys, List<string> ids) where C : ICoreEntity;
  
  public abstract ValueTask DisposeAsync();

}