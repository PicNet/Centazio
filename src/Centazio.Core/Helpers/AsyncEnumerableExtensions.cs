namespace Centazio.Core.Helpers;

public static class AsyncEnumerableExtensions {

  public static async Task<IEnumerable<T>> Synchronous<T>(this IEnumerable<Task<T>> tasks) {
    var results = new List<T>();
    foreach (var task in tasks) results.Add(await task);
    return results;
  }
  
  public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int chunksz = 25) {
        if (chunksz <= 0) throw new ArgumentOutOfRangeException(nameof(chunksz), "Chunk size must be greater than zero.");

        return list
            .Select((item, index) => new { item, index })
            .GroupBy(x => x.index / chunksz)
            .Select(g => g.Select(x => x.item).ToList())
            .ToList();
  }

}