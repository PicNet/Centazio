namespace Centazio.Core;

public static class GlobalExtensionMethods { 
  public static void ForEachIdx<T>(this IEnumerable<T> e, Action<T, int> action) {
    var lst = e.ToList();
    for (var i = 0; i < lst.Count; i++) action(lst[i], i);
  }
}