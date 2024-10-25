using Centazio.Core.CoreRepo;
using Centazio.Test.Lib.E2E;

namespace Centazio.Test.Lib.InMemRepos;

public class InMemoryEpochTracker : IEpochTracker {

  private List<ICoreEntity> added = new();
  private List<ICoreEntity> updated = new();
  
  public int Epoch { get; private set; }

  public void SetEpoch(int epoch) => (Epoch, added, updated) = (epoch, new(), new());
  
  public void Add(ICoreEntity coreent) { added.Add(coreent); }
  public void Update(ICoreEntity coreent) { updated.Add(coreent); }
  
  public IReadOnlyList<ICoreEntity> Added => added.AsReadOnly();
  public IReadOnlyList<ICoreEntity> Updated => updated.AsReadOnly();

}