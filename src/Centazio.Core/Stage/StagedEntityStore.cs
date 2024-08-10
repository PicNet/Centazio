using Centazio.Core.Entities.Ctl;

namespace Centazio.Core.Stage;

public interface IStagedEntityStore : IAsyncDisposable {
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  
  Task Update(StagedEntity staged);
  Task Update(IEnumerable<StagedEntity> se);
  
  Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj);
  Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj);
  Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj);
}

public abstract class AbstractStagedEntityStore : IStagedEntityStore {

  public Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data) => SaveImpl(new StagedEntity(source, obj, stageddt, data));
  public Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) => SaveImpl(datas.Select(data => new StagedEntity(source, obj, stageddt, data)));
  
  protected abstract Task SaveImpl(StagedEntity se);
  protected abstract Task SaveImpl(IEnumerable<StagedEntity> ses);
  
  public abstract Task Update(StagedEntity staged);
  public abstract Task Update(IEnumerable<StagedEntity> se);

  // gurantee that staged entities are returned only if > since and
  // sorted correctly even if implementation is wrong
  public async Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj) => (await GetImpl(since, source, obj))
      .Where(s => s.DateStaged > since && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .ToList();
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, false);

  
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}