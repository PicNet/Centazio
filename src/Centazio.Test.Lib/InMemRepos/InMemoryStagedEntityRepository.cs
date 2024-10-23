using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;

namespace Centazio.Test.Lib.InMemRepos;

public class InMemoryStagedEntityRepository(int limit, Func<string, StagedEntityChecksum> checksum) : AbstractStagedEntityRepository(limit, checksum) {

  private readonly Dictionary<string, bool> checksums = new();
  protected readonly List<StagedEntity> saved = [];

  public override Task Update(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    staged.ForEach(s => {
      var idx = saved.FindIndex(e => e.System == s.System && e.SystemEntityTypeName == s.SystemEntityTypeName && e.Id == s.Id);
      if (idx < 0) throw new Exception($"could not find StagedEntity[{s.Id}]");
      saved[idx] = s;
    });
    return Task.CompletedTask;
  }
  

  protected override Task<List<StagedEntity>> StageImpl(SystemName system, SystemEntityTypeName systype, List<StagedEntity> staged) {
    var newchecksums = new Dictionary<string, bool>();
    var lst = staged.Where(e => !checksums.ContainsKey(e.StagedEntityChecksum) && newchecksums.TryAdd(e.StagedEntityChecksum, true)).ToList();
    
    saved.AddRange(lst);
    newchecksums.Keys.ForEach(cs => checksums.Add(cs, true));
    
    return Task.FromResult(lst);
  }

  protected override Task<List<StagedEntity>> GetImpl(SystemName system, SystemEntityTypeName systype, DateTime after, bool incpromoted) => 
      Task.FromResult(saved
          .Where(s => s.DateStaged > after && s.System == system && s.SystemEntityTypeName == systype && s.IgnoreReason is null && (incpromoted || !s.DatePromoted.HasValue))
          .OrderBy(s => s.DateStaged)
          .Take(Limit)
          .ToList());

  protected override Task DeleteBeforeImpl(SystemName system, SystemEntityTypeName systype, DateTime before, bool promoted) {
    var toremove = saved
        .Where(se => se.System == system && se.SystemEntityTypeName == systype && 
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