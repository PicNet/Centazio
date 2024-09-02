using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public class InMemoryStagedEntityStore(int limit, Func<string, string> checksum) : AbstractStagedEntityStore(limit, checksum) {

  private readonly Dictionary<string, bool> checksums = new();
  protected readonly List<StagedEntity> saved = [];

  public override Task Update(IEnumerable<StagedEntity> staged) {
    staged.ForEachIdx(se2 => {
      var idx = saved.FindIndex(se => se.SourceSystem == se2.SourceSystem && se.Object == se2.Object && se.DateStaged == se2.DateStaged);
      if (idx < 0) throw new Exception($"could not find [{se2}]");
      saved[idx] = se2;
    });
    return Task.CompletedTask;
  }
  

  protected override Task<IEnumerable<StagedEntity>> StageImpl(IEnumerable<StagedEntity> staged) {
    var newchecksums = new Dictionary<string, bool>();
    var lst = staged.Where(e => !checksums.ContainsKey(e.Checksum) && newchecksums.TryAdd(e.Checksum, true)).ToList();
    
    saved.AddRange(lst);
    newchecksums.Keys.ForEachIdx(cs => checksums.Add(cs, true));
    
    return Task.FromResult(lst.AsEnumerable());
  }

  protected override Task<IEnumerable<StagedEntity>> GetImpl(DateTime after, SystemName source, ObjectName obj) => 
      Task.FromResult(saved
          .Where(s => s.DateStaged > after && s.SourceSystem == source && s.Object == obj && s.Ignore is null)
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