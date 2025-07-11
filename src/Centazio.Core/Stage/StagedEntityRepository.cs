using Centazio.Core.Checksum;

namespace Centazio.Core.Stage;

public interface IEntityStager : IAsyncDisposable {
  Task<StagedEntity?> StageSingleItem(SystemName system, SystemEntityTypeName systype, RawJsonDataWithCorrelationId data);
  Task<List<StagedEntity>> StageItems(SystemName system, SystemEntityTypeName systype, List<RawJsonDataWithCorrelationId> datas);
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

  public int Limit { get => field > 0 ? field : Int32.MaxValue; set; } = limit;

  public async Task<StagedEntity?> StageSingleItem(SystemName system, SystemEntityTypeName systype, RawJsonDataWithCorrelationId data) {
    var results = (await StageItems(system, systype, [data])).ToList();
    return results.Any() ? results.Single() : null; 
  }

  public async Task<List<StagedEntity>> StageItems(SystemName system, SystemEntityTypeName systype, List<RawJsonDataWithCorrelationId> datas) {
    var now = UtcDate.UtcNow; // ensure all staged entities in this batch have the same `DateStaged`
    var tostage = datas.Distinct().Select(data => StagedEntity.Create(system, systype, now, new(data.Json), data.CorrelationId, checksum(data.Json))).ToList();
    if (!tostage.Any()) return tostage;
    if (tostage.Any(e => e.System != system || e.SystemEntityTypeName != systype)) throw new Exception();
    var checksums = tostage.Select(s => s.StagedEntityChecksum).ToList();
    var duplicates = await GetDuplicateChecksums(system, systype, checksums);
    var nonduplicates = tostage.Where(s => !duplicates.Contains(s.StagedEntityChecksum)).ToList();
    if (!nonduplicates.Any()) return nonduplicates;
    
    DataFlowLogger.Log(system, systype, "Staging", [$"{nonduplicates.Count} Entity(s)"]);
    return await StageImpl(system, systype, nonduplicates);
  }

  protected abstract Task<List<StagedEntityChecksum>> GetDuplicateChecksums(SystemName system, SystemEntityTypeName systype, List<StagedEntityChecksum> newchecksums);
  protected abstract Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> tostage);
  
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

public record RawJsonData(string Json, string? Id, DateTime? LastUpdatedUtc) {
  public T Deserialise<T>() where T : ISystemEntity => Centazio.Core.Misc.Json.Deserialize<T>(Json);
  
  internal RawJsonDataWithCorrelationId AddCorrelation(CorrelationId corrid) => new(Json, corrid, Id, LastUpdatedUtc);
}
public record RawJsonDataWithCorrelationId(string Json, CorrelationId CorrelationId, string? Id, DateTime? LastUpdatedUtc) : RawJsonData(Json, Id, LastUpdatedUtc);
