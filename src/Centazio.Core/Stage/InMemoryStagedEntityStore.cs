using Centazio.Core.Entities.Ctl;

namespace Centazio.Core.Stage;

public class InMemoryStagedEntityStore : AbstractStagedEntityStore {

  private readonly List<StagedEntity> saved = [];
  
  public override Task Update(StagedEntity staged) {
    var idx = saved.FindIndex(se => se.SourceSystem == staged.SourceSystem && se.Object == staged.Object && se.DateStaged == staged.DateStaged);
    if (idx < 0) throw new Exception($"could not find [{staged}]");
    saved[idx] = staged;
    return Task.CompletedTask;
  }

  public override Task Update(IEnumerable<StagedEntity> staged) {
    staged.ForEachIdx(se => {
      var idx = saved.FindIndex(s => s.SourceSystem == se.SourceSystem && s.Object == se.Object && s.DateStaged == se.DateStaged);
      if (idx < 0) throw new Exception($"could not find [{se}]");
      saved[idx] = se;
    });
    return Task.CompletedTask;
  }
  
  protected override Task SaveImpl(StagedEntity se) {
    saved.Add(se);
    return Task.CompletedTask;
  }

  protected override Task SaveImpl(IEnumerable<StagedEntity> ses) {
    saved.AddRange(ses);
    return Task.CompletedTask;
  }

  protected override Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) => Task.FromResult(saved
      .Where(s => s.DateStaged > since && s.SourceSystem == source && s.Object == obj)
      .OrderBy(s => s.DateStaged)
      .AsEnumerable());

  protected override Task DeleteBeforeImpl(DateTime before, SystemName source, ObjectName obj, bool promoted) {
    var toremove = saved
        .Where(se => se.SourceSystem == source && se.Object == obj && 
            ((promoted && se.DatePromoted < before) || 
                (!promoted && se.DateStaged < before)))
        .ToList();
    saved.RemoveAll(se => toremove.Contains(se));
    return Task.CompletedTask;
  }

  public override ValueTask DisposeAsync() { 
    saved.Clear();
    return ValueTask.CompletedTask;
  }
}