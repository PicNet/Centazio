using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public interface IEntityStager : IAsyncDisposable {
  Task<StagedEntity?> Stage(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task<IEnumerable<StagedEntity>> Stage(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
}
    
public interface IStagedEntityStore : IEntityStager {
  Task Update(StagedEntity staged);
  Task Update(IEnumerable<StagedEntity> staged);
  
  Task<List<StagedEntity>> GetAll(DateTime after, SystemName source, ObjectName obj);
  Task<List<StagedEntity>> GetUnpromoted(DateTime after, SystemName source, ObjectName obj);
  
  Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj);
  Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj);
}

public abstract class AbstractStagedEntityStore(int limit, Func<string, string> checksum) : IStagedEntityStore {
  
  protected int Limit => limit > 0 ? limit : Int32.MaxValue;
  
  public async Task<StagedEntity?> Stage(DateTime stageddt, SystemName source, ObjectName obj, string data) {
    var results = (await Stage(stageddt, source, obj, new[] { data })).ToList();
    return results.Any() ? results.Single() : null; 
  }

  public Task<IEnumerable<StagedEntity>> Stage(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) {
    return StageImpl(datas.Distinct().Select(data => new StagedEntity(Guid.CreateVersion7(), source, obj, stageddt, data, checksum(data))));
  }

  protected abstract Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged);
  
  public Task Update(StagedEntity staged) => Update(new [] { staged });
  public abstract Task Update(IEnumerable<StagedEntity> staged);

  public async Task<List<StagedEntity>> GetAll(DateTime after, SystemName source, ObjectName obj) => (await GetImpl(after, source, obj, true))
      // todo: remove this and add to unit tests to ensure implementations are correct
      .Where(s => s.DateStaged > after && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .Take(Limit)
      .ToList();
  
  // gurantee that staged entities are returned only if > after and
  // sorted correctly even if implementation is wrong
  // todo: remove this and add to unit tests to ensure implementations are correct
  public async Task<List<StagedEntity>> GetUnpromoted(DateTime after, SystemName source, ObjectName obj) => (await GetImpl(after, source, obj, false))
      .Where(s => s.DateStaged > after && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .Take(Limit)
      .ToList();
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, false);
  
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj, bool incpromoted);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}