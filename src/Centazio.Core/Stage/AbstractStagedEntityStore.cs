using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public interface IEntityStager : IAsyncDisposable {
  Task<StagedEntity?> Stage(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task<IEnumerable<StagedEntity>> Stage(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
}
    
public interface IStagedEntityStore : IEntityStager {
  Task Update(StagedEntity staged);
  Task Update(IEnumerable<StagedEntity> staged);
  
  Task<List<StagedEntity>> Get(DateTime after, SystemName source, ObjectName obj);
  
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
    return StageImpl(datas.Distinct().Select(data => new StagedEntity(source, obj, stageddt, data, checksum(data))));
  }

  protected abstract Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged);
  
  public Task Update(StagedEntity staged) => Update(new [] { staged });
  public abstract Task Update(IEnumerable<StagedEntity> staged);
  
  // gurantee that staged entities are returned only if > after and
  // sorted correctly even if implementation is wrong
  public async Task<List<StagedEntity>> Get(DateTime after, SystemName source, ObjectName obj) => (await GetImpl(after, source, obj))
      .Where(s => s.DateStaged > after && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .Take(Limit)
      .ToList();
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, false);
  
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}