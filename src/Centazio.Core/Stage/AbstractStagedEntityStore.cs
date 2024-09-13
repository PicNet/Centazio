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

  public async Task<IEnumerable<StagedEntity>> Stage(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) {
    var ses = datas.Distinct().Select(data => new StagedEntity(Guid.CreateVersion7(), source, obj, stageddt, data, checksum(data))).ToList();
    if (!ses.Any()) return ses;
    return await StageImpl(ses);
  }

  /// <summary>
  /// Implementing provider can assume that `staged` has already been de-duped and has at least 1 entity.
  /// </summary>
  protected abstract Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged);
  
  public Task Update(StagedEntity staged) => Update(new [] { staged });
  public abstract Task Update(IEnumerable<StagedEntity> staged);

  // todo: this should return IEnumerable to allow providers to stream data
  public async Task<List<StagedEntity>> GetAll(DateTime after, SystemName source, ObjectName obj) => (await GetImpl(after, source, obj, true)).ToList();
  
  // todo: this should return IEnumerable to allow providers to stream data
  public async Task<List<StagedEntity>> GetUnpromoted(DateTime after, SystemName source, ObjectName obj) => (await GetImpl(after, source, obj, false)).ToList();

  /// <summary>
  /// Implementing providers must ensure the following:
  /// - Only data where DateStaged > after is returned
  /// - Data is returned sorted in ascending order by DateStaged (oldest to newest)
  /// - If Limit is specified, at most that number of records is returned.  Note: This is a performance
  ///   feature and the provider should ensure they only query the underlying data source for maximum this
  ///   amount of records. 
  /// </summary>
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj, bool incpromoted);
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ObjectName obj) => await DeleteBeforeImpl(before, source, obj, false);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}