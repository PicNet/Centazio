using Centazio.Core.Entities.Ctl;

namespace Centazio.Core.Stage;

public interface IStagedEntityStore {
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj);
}

public abstract class AbstractStagedEntityStore(IUtcDate dt) : IStagedEntityStore {

  public abstract Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  public abstract Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  
  protected abstract Task<List<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj);
  
  public async Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj) {
    var raw = await GetImpl(since, source, obj);
    // gurantee that staged entities are returned only if > since and
    // sorted correctly even if implementer gets this wrong
    return raw.Where(s => s.DateStaged > since).OrderBy(s => s.DateStaged).ToList();
  }

}

public class InMemoryStagedEntityStore(IUtcDate dt) : AbstractStagedEntityStore(dt) {

  private readonly List<StagedEntity> saved = [];
  
  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data) {
    saved.Add(new StagedEntity(source, obj, stageddt, data));
    return Task.CompletedTask;
  }

  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) {
    saved.AddRange(datas.Select(data => new StagedEntity(source, obj, stageddt, data)));
    return Task.CompletedTask;
  }

  protected override Task<List<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) => 
      Task.FromResult(saved.Where(s => s.DateStaged > since && s.Source == source && s.Object == obj).OrderBy(s => s.DateStaged).ToList());

}