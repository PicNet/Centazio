namespace Centazio.Core;

public static class GlobalEnumerableExtensionMethods {

  public static void ForEachIdx<T>(this IEnumerable<T> e, Action<T> action) {
    foreach (var t in e) action(t);
  }
  
  public static void ForEachIdx<T>(this IEnumerable<T> e, Action<T, int> action) {
    var lst = e.ToList();
    for (var i = 0; i < lst.Count; i++) action(lst[i], i);
  }

  public static void Deconstruct<T>(this IList<T> list, out T first, out IList<T> rest) {
    first = list.Count > 0 ? list[0] : throw new Exception();
    rest = list.Skip(1).ToList();
  }

  public static void Deconstruct<T>(this IList<T> list, out T first, out T second, out IList<T> rest) {
    first = list.Count > 0 ? list[0] : throw new Exception();
    second = list.Count > 1 ? list[1] : throw new Exception();
    rest = list.Skip(2).ToList();
  }

}