namespace Centazio.Core.Misc;

public static class GlobalMiscExtensionMethods {
  public static long ToMillis(this DateTime dt) => new DateTimeOffset(dt).ToUnixTimeMilliseconds();
}

public static class GlobalEnumerableExtensionMethods {

  public static T AddAndReturn<T>(this ICollection<T> lst, T item) {
    lst.Add(item);
    return item;
  }
  
  public static List<T> AddRangeAndReturn<T>(this List<T> lst, List<T> items) {
    lst.AddRange(items);
    return items;
  }

  public static void ForEach<T>(this IEnumerable<T> e, Action<T> action) {
    foreach (var t in e) action(t);
  }
  
  public static void ForEach<T>(this IEnumerable<T> e, Action<T, int> action) {
    var lst = e.ToList();
    for (var i = 0; i < lst.Count; i++) action(lst[i], i);
  }
  
  public static async Task ForEachSequentialAsync<T>(this IEnumerable<T> e, Func<T, Task> action) {
    foreach (var t in e) { await action(t); }
  }
  
  public static async Task ForEachSequentialAsync<T>(this IEnumerable<T> e, Func<T, int, Task> action) {
    var lst = e.ToList();
    for (var i = 0; i < lst.Count; i++) { await action(lst[i], i); }
  }
  
  public static List<T> ForEachAndReturn<T>(this IEnumerable<T> e, Action<T> action) {
    var lst = e.ToList();
    lst.ForEach(action);
    return lst;
  }
  
  public static List<T> ForEachAndReturn<T>(this IEnumerable<T> e, Action<T, int> action) {
    var lst = e.ToList();
    lst.ForEach(action);
    return lst;
  }

  public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest) {
    if (!list.Any()) throw new ArgumentException("list is empty");
    (first, rest) = (list[0], list.Skip(1).ToList());
  }

  public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest) {
    if (list.Count < 2) throw new ArgumentException("not enough items in the list to deconstruct");
    (first, second, rest) = (list[0], list[1], list.Skip(2).ToList());
  }

  public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out T third, out IList<T> rest) {
    if (list.Count < 3) throw new ArgumentException("not enough items in the list to deconstruct");
    (first, second, third, rest) = (list[0], list[1], list[2], list.Skip(3).ToList());
  }
  
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
}