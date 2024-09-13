namespace Centazio.Core;

public static class GlobalEnumerableExtensionMethods {

  public static void ForEach<T>(this IEnumerable<T> e, Action<T> action) {
    foreach (var t in e) action(t);
  }
  
  public static void ForEach<T>(this IEnumerable<T> e, Action<T, int> action) {
    var lst = e.ToList();
    for (var i = 0; i < lst.Count; i++) action(lst[i], i);
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
}