using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public class InMemoryStagedEntityStore(int limit) : AbstractStagedEntityStore(limit) {

  protected readonly List<StagedEntity> saved = [];
  
  public override Task Update(StagedEntity staged) {
    var idx = saved.FindIndex(se => se.SourceSystem == staged.SourceSystem && se.Object == staged.Object && se.DateStaged == staged.DateStaged);
    if (idx < 0) throw new Exception($"could not find [{staged}]");
    saved[idx] = staged;
    return Task.CompletedTask;
  }

  public override Task Update(IEnumerable<StagedEntity> staged) {
    staged.ForEachIdx(se => Update(se));
    return Task.CompletedTask;
  }
  
  protected override Task<StagedEntity> SaveImpl(StagedEntity staged) {
    saved.Add(staged);
    return Task.FromResult(staged);
  }

  protected override Task<IEnumerable<StagedEntity>> SaveImpl(IEnumerable<StagedEntity> staged) {
    var lst = staged.ToList();
    saved.AddRange(lst);
    return Task.FromResult(lst.AsEnumerable());
  }

  protected override Task<IEnumerable<StagedEntity>> GetImpl(DateTime since, SystemName source, ObjectName obj) => 
      Task.FromResult(saved
          .Where(s => s.DateStaged > since && s.SourceSystem == source && s.Object == obj && s.Ignore == null)
          .OrderBy(s => s.DateStaged)
          .Take(Limit)
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