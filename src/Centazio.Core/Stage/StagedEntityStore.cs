using centazio.core.Ctl.Entities;

namespace Centazio.Core.Stage;

public interface IStagedEntityStore : IAsyncDisposable {
  Task<StagedEntity> Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task<IEnumerable<StagedEntity>> Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  
  Task Update(StagedEntity staged);
  Task Update(IEnumerable<StagedEntity> staged);
  
  Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj);
  
  Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj);
  Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj);
}

public abstract class AbstractStagedEntityStore(int limit) : IStagedEntityStore {

  protected int Limit => limit > 0 ? limit : Int32.MaxValue;
  
  public Task<StagedEntity> Save(DateTime stageddt, SystemName source, ObjectName obj, string data) => 
      SaveImpl(new StagedEntity(source, obj, stageddt, data));

  public Task<IEnumerable<StagedEntity>> Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) => 
      SaveImpl(datas.Select(data => new StagedEntity(source, obj, stageddt, data)));
  
  protected abstract Task<StagedEntity> SaveImpl(StagedEntity staged);
  protected abstract Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> staged);
  
  public abstract Task Update(StagedEntity staged);
  public abstract Task Update(IEnumerable<StagedEntity> staged);

  // gurantee that staged entities are returned only if > since and
  // sorted correctly even if implementation is wrong
  public async Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj) => (await GetImpl(since, source, obj))
      .Where(s => s.DateStaged > since && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .Take(Limit)
      .ToList();
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, false);
  
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}