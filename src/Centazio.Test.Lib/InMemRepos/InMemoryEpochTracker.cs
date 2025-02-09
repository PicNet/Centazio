using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Misc;
using Centazio.Test.Lib.E2E;

namespace Centazio.Test.Lib.InMemRepos;

[IgnoreNamingConventions] public class InMemoryEpochTracker : IEpochTracker {

  private List<CoreEntityAndMeta> added = new();
  private List<CoreEntityAndMeta> updated = new();
  
  public int Epoch { get; private set; }

  public void SetEpoch(int epoch) => (Epoch, added, updated) = (epoch, new(), new());
  
  public void Add(CoreEntityAndMeta coreent) { added.Add(coreent); }
  public void Update(CoreEntityAndMeta coreent) { updated.Add(coreent); }
  
  public IReadOnlyList<ICoreEntity> Added => added.Select(ceam => ceam.CoreEntity).ToList().AsReadOnly();
  public IReadOnlyList<ICoreEntity> Updated => updated.Select(ceam => ceam.CoreEntity).ToList().AsReadOnly();

}