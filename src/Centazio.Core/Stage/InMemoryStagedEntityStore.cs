using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;

namespace Centazio.Core.Stage;

public class InMemoryStagedEntityStore(int limit, Func<string, StagedEntityChecksum> checksum) 
    : AbstractStagedEntityStore(limit, checksum) {

  private readonly Dictionary<string, bool> checksums = new();
  protected readonly List<StagedEntity> saved = [];

  public override Task Update(List<StagedEntity> staged) {
    staged.ForEach(s => {
      var idx = saved.FindIndex(e => e.SourceSystem == s.SourceSystem && e.Object == s.Object && e.Id == s.Id);
      if (idx < 0) throw new Exception($"could not find StagedEntity[{s.Id}]");
      saved[idx] = s;
    });
    return Task.CompletedTask;
  }
  

  protected override Task<List<StagedEntity>> StageImpl(List<StagedEntity> staged) {
    var newchecksums = new Dictionary<string, bool>();
    var lst = staged.Where(e => !checksums.ContainsKey(e.StagedEntityChecksum) && newchecksums.TryAdd(e.StagedEntityChecksum, true)).ToList();
    
    saved.AddRange(lst);
    newchecksums.Keys.ForEach(cs => checksums.Add(cs, true));
    
    return Task.FromResult(lst);
  }

  protected override Task<List<StagedEntity>> GetImpl(DateTime after, SystemName source, ExternalEntityType obj, bool incpromoted) => 
      Task.FromResult(saved
          .Where(s => s.DateStaged > after && s.SourceSystem == source && s.Object == obj && s.IgnoreReason is null && (incpromoted || !s.DatePromoted.HasValue))
          .OrderBy(s => s.DateStaged)
          .Take(Limit)
          .ToList());

  protected override Task DeleteBeforeImpl(DateTime before, SystemName source, ExternalEntityType obj, bool promoted) {
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