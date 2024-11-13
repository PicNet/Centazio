using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public interface IEntityStager : IAsyncDisposable {
  Task<StagedEntity?> Stage(SystemName system, SystemEntityTypeName systype, string data);
  Task<List<StagedEntity>> Stage(SystemName system, SystemEntityTypeName systype, List<string> datas);
}
    
public interface IStagedEntityRepository : IEntityStager {
  public Task<IStagedEntityRepository> Initialise();
  
  int Limit { get; set; }
  Task Update(StagedEntity staged);
  Task UpdateImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged);
  
  Task<List<StagedEntity>> GetAll(SystemName system, SystemEntityTypeName systype, DateTime after);
  Task<List<StagedEntity>> GetUnpromoted(SystemName system, SystemEntityTypeName systype, DateTime after);
  
  Task DeletePromotedBefore(SystemName system, SystemEntityTypeName systype, DateTime before);
  Task DeleteStagedBefore(SystemName system, SystemEntityTypeName systype, DateTime before);
}

public abstract class AbstractStagedEntityRepository(int limit, Func<string, StagedEntityChecksum> checksum) : IStagedEntityRepository {

  private int lim = limit;
  
  public int Limit {
    get => lim > 0 ? lim : Int32.MaxValue;
    set => lim = value;
  }

  public async Task<StagedEntity?> Stage(SystemName system, SystemEntityTypeName systype, string data) {
    var results = (await Stage(system, systype, [data])).ToList();
    return results.Any() ? results.Single() : null; 
  }

  public async Task<List<StagedEntity>> Stage(SystemName system, SystemEntityTypeName systype, List<string> datas) {
    var now = UtcDate.UtcNow; // ensure all staged entities in this batch have the same `DateStaged`
    var staged = datas.Distinct().Select(data => StagedEntity.Create(system, systype, now, new(data), checksum(data))).ToList();
    if (!staged.Any()) return staged;
    if (staged.Any(e => e.System != system || e.SystemEntityTypeName != systype)) throw new Exception();
    return await StageImpl(system, systype, staged);
  }

  protected abstract Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged);
  
  public Task Update(StagedEntity staged) => Update(staged.System, staged.SystemEntityTypeName, [staged]);
  public Task Update(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    if (!staged.Any()) return Task.CompletedTask; 
    if (staged.Any(e => e.System != system || e.SystemEntityTypeName != systype)) throw new Exception();
    return UpdateImpl(system, systype, staged);
  }

  public abstract Task UpdateImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged);

  public Task<List<StagedEntity>> GetAll(SystemName system, SystemEntityTypeName systype, DateTime after) => GetImpl(system, systype, after, true);
  public Task<List<StagedEntity>> GetUnpromoted(SystemName system, SystemEntityTypeName systype, DateTime after) => GetImpl(system, systype, after, false);

  /// <summary>
  /// Implementing providers must ensure the following:
  /// - Only data where DateStaged > after is returned
  /// - Data is returned sorted in ascending order by DateStaged (oldest to newest)
  /// - If Limit is specified, at most that number of records is returned.  Note: This is a performance
  ///   feature and the provider should ensure they only query the underlying data source for maximum this
  ///   amount of records. 
  /// </summary>
  protected abstract Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted);
  
  public async Task DeletePromotedBefore(SystemName system, SystemEntityTypeName systype, DateTime before) => await DeleteBeforeImpl(system, systype, before, true);
  public async Task DeleteStagedBefore(SystemName system, SystemEntityTypeName systype, DateTime before) => await DeleteBeforeImpl(system, systype, before, false);
  protected abstract Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted);
  
  public abstract Task<IStagedEntityRepository> Initialise();
  public abstract ValueTask DisposeAsync();

}