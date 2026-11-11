namespace Centazio.Test.Lib.E2E;

/// <summary>
/// The simulation's only source of randomness.  It is owned by a `SimulationCtx` so every value a simulation draws
/// depends only on its seed, not on which other simulations ran before it in the same process.
/// </summary>
public class SimulationRandom(Random rng) {

  public int Next(int max) => rng.Next(max);
  public int Next(int min, int max) => rng.Next(min, max);
  
  public Guid NewGuid() {  
    var guid = new byte[16];
    rng.NextBytes(guid);
    return new Guid(guid);
  }

  public List<T> ShuffleAndTake<T>(IEnumerable<T> enumerable, int? take = null) {
    var list = enumerable.ToList();
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = rng.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return take.HasValue ? list.Take(take.Value).ToList() : list;
  }
  
  public T RandomItem<T>(IList<T> lst) => lst[rng.Next(lst.Count)];
}
