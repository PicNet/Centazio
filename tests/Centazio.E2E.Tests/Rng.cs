namespace Centazio.E2E.Tests;

public static class Rng {

  private static Random rng { get; } = new(SimulationConstants.RANDOM_SEED);
  
  public static int Next(int max) => rng.Next(max);
  public static int Next(int min, int max) => rng.Next(min, max);
  
  public static Guid NewGuid() {  
    var guid = new byte[16];
    rng.NextBytes(guid);
    return new Guid(guid);
  }

  public static List<T> ShuffleAndTake<T>(IEnumerable<T> enumerable, int? take = null) {
    var list = enumerable.ToList();
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = rng.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return take.HasValue ? list.Take(take.Value).ToList() : list;
  }
  
  public static T RandomItem<T>(IList<T> lst) => lst[rng.Next(lst.Count)];
}