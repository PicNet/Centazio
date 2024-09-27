namespace Centazio.Core.Extensions;

public static class EnumerableExtensions {

  public static async Task Synchronous(this IEnumerable<Task> tasks) {
    foreach (var task in tasks) await task;
  }
  
  public static async Task<List<T>> Synchronous<T>(this IEnumerable<Task<T>> tasks, Func<T, bool>? abort = null) {
    var results = new List<T>();
    foreach (var task in tasks) {
      var res = await task; 
      results.Add(res);
      if (abort is not null && abort(res)) { return results; }
    }
    return results;
  }
  
  public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunksz = 25) {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunksz);
    return list
        .Select((item, index) => new { item, index })
        .GroupBy(x => x.index / chunksz)
        .Select(g => g.Select(x => x.item).ToList())
        .ToList();
  }
  
  public static async Task<List<T>> ChunkedSynchronousCall<T>(this IEnumerable<Task<T>> ops, int chunksz = 25) => 
      (await ops.Chunk(chunksz).Select(Task.WhenAll).Synchronous()).SelectMany(lst => lst).ToList();
  
  public static IList<T> Shuffle<T>(this IList<T> list) {
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = Random.Shared.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return list;
  }
  
  public static T RandomItem<T>(this IList<T> lst) => lst[Random.Shared.Next(lst.Count)];
}