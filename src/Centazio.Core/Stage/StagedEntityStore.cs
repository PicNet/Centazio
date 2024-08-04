using Centazio.Core.Entities.Ctl;

namespace Centazio.Core.Stage;

public interface IStagedEntityStore {
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj);
}

public abstract class AbstractStagedEntityStore : IStagedEntityStore {

  public abstract Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data);
  public abstract Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas);
  
  // gurantee that staged entities are returned only if > since and
  // sorted correctly even if implementation is wrong
  public async Task<List<StagedEntity>> Get(DateTime since, SystemName source, ObjectName obj) => (await GetImpl(since, source, obj))
      .Where(s => s.DateStaged > since && s.Source == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .ToList();
  
  protected abstract Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj);
}

public class InMemoryStagedEntityStore : AbstractStagedEntityStore {

  private readonly List<StagedEntity> saved = [];
  
  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, string data) {
    saved.Add(new StagedEntity(source, obj, stageddt, data));
    return Task.CompletedTask;
  }

  public override Task Save(DateTime stageddt, SystemName source, ObjectName obj, IEnumerable<string> datas) {
    saved.AddRange(datas.Select(data => new StagedEntity(source, obj, stageddt, data)));
    return Task.CompletedTask;
  }

  protected override Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) => Task.FromResult(saved
          .Where(s => s.DateStaged > since && s.Source == source && s.Object == obj)
          .OrderBy(s => s.DateStaged)
          .AsEnumerable());

}