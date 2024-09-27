using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public interface IEntityStager : IAsyncDisposable {
  Task<StagedEntity?> Stage(SystemName source, ExternalEntityType obj, string data);
  Task<IEnumerable<StagedEntity>> Stage(SystemName source, ExternalEntityType obj, IEnumerable<string> datas);
}
    
public interface IStagedEntityStore : IEntityStager {
  int Limit { get; set; }
  Task Update(StagedEntity staged);
  Task Update(IEnumerable<StagedEntity> staged);
  
  Task<IEnumerable<StagedEntity>> GetAll(DateTime after, SystemName source, ExternalEntityType obj);
  Task<IEnumerable<StagedEntity>> GetUnpromoted(DateTime after, SystemName source, ExternalEntityType obj);
  
  Task DeletePromotedBefore(DateTime before, SystemName source, ExternalEntityType obj);
  Task DeleteStagedBefore(DateTime before, SystemName source, ExternalEntityType obj);
}

public abstract class AbstractStagedEntityStore(int limit, Func<string, string> checksum) : IStagedEntityStore {

  private int lim = limit;
  
  public int Limit {
    get => lim > 0 ? lim : Int32.MaxValue;
    set => lim = value;
  }

  public async Task<StagedEntity?> Stage(SystemName source, ExternalEntityType obj, string data) {
    var results = (await Stage(source, obj, [data])).ToList();
    return results.Any() ? results.Single() : null; 
  }

  public async Task<IEnumerable<StagedEntity>> Stage(SystemName source, ExternalEntityType obj, IEnumerable<string> datas) {
    var now = UtcDate.UtcNow; // ensure all staged entities in this batch have the same `DateStaged`
    var ses = datas.Distinct().Select(data => StagedEntity.Create(source, obj, now, data, checksum(data))).ToList();
    if (!ses.Any()) return ses;
    return await StageImpl(ses);
  }

  /// <summary>
  /// Implementing provider can assume that `staged` has already been de-duped and has at least 1 entity.
  /// </summary>
  protected abstract Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged);
  
  public Task Update(StagedEntity staged) => Update([staged]);
  public abstract Task Update(IEnumerable<StagedEntity> staged);

  public Task<IEnumerable<StagedEntity>> GetAll(DateTime after, SystemName source, ExternalEntityType obj) => GetImpl(after, source, obj, true);
  public Task<IEnumerable<StagedEntity>> GetUnpromoted(DateTime after, SystemName source, ExternalEntityType obj) => GetImpl(after, source, obj, false);

  /// <summary>
  /// Implementing providers must ensure the following:
  /// - Only data where DateStaged > after is returned
  /// - Data is returned sorted in ascending order by DateStaged (oldest to newest)
  /// - If Limit is specified, at most that number of records is returned.  Note: This is a performance
  ///   feature and the provider should ensure they only query the underlying data source for maximum this
  ///   amount of records. 
  /// </summary>
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ExternalEntityType obj, bool incpromoted);
  
  public async Task DeletePromotedBefore(DateTime before, SystemName source, ExternalEntityType obj) => await DeleteBeforeImpl(before, source, obj, true);
  public async Task DeleteStagedBefore(DateTime before, SystemName source, ExternalEntityType obj) => await DeleteBeforeImpl(before, source, obj, false);
  protected abstract Task DeleteBeforeImpl(DateTime before, SystemName source, ExternalEntityType obj, bool promoted);
  
  public abstract ValueTask DisposeAsync();

}