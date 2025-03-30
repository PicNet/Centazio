using System.Collections;

namespace Centazio.Core.Misc;

public interface IDto<out T> { T ToBase(); }
public interface ICoreEntityDto<out T> : IDto<T> { string CoreId { get; } }

record PropPair(PropertyInfo BasePi, PropertyInfo DtoPi);

public static class DtoHelpers {
  public static D ToDto<E, D>(E baseobj) where D : class, IDto<E> {
    ArgumentNullException.ThrowIfNull(baseobj);
    return (D) ToDto(baseobj);
  }
  
  public static object ToDto(object baseobj) {
    ArgumentNullException.ThrowIfNull(baseobj);

    var dtot = GetDtoTypeFromTypeHierarchy(baseobj.GetType());
    if (dtot is null) throw new Exception($"baseobj does not have a associated Dto.  Call `DtoHelpers.HasDto` before calling `ToDto`."); 
    var dto = Activator.CreateInstance(dtot) ?? throw new Exception();
    var pairs = GetPropPairs(baseobj.GetType(), dtot);
    pairs.ForEach(p => p.DtoPi.SetValue(dto, GetDtoVal(GetValueSafe(p.BasePi, baseobj))));
    return dto;
    
    object? GetValueSafe(PropertyInfo pi, object obj) {
      try { return pi.GetValue(obj); } 
      catch (TargetInvocationException) { return null; }
    }
  }
  
  public static bool HasDto(object baseobj) => GetDtoTypeFromTypeHierarchy(baseobj.GetType()) is not null;

  private static List<PropPair> GetPropPairs(Type baset, Type dtot) {
    var dtoprops = dtot.GetProperties();
    return baset.GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => !ReflectionUtils.IsJsonIgnore(baset, p.Name))
        .Select(pi => {
          var dtopi = dtoprops.SingleOrDefault(pi2 => pi2.Name == pi.Name) ?? throw new Exception($"could not find property[{pi.Name}] in Dto[{dtot.FullName}]");
          return new PropPair(pi, dtopi);
        }).ToList();
  } 
  
  private static object? GetDtoVal(object? origval) {
    if (origval is null) return origval;
    var origtype = origval.GetType();
    if (origtype.IsEnum) return origval.ToString();
    if (origval is ValidString vs) return vs.Value;
    if (HasDto(origval)) return ToDto(origval);
    if (origtype.IsGenericType && origtype.GetGenericTypeDefinition() == typeof(List<>)) {
      var elemtype = origtype.GetGenericArguments().First();
      var dtoelem = GetDtoTypeFromTypeHierarchy(elemtype);
      if (dtoelem is null) return origval; // non dto lists, like List<string>
      
      var listtype = typeof(List<>).MakeGenericType(dtoelem);
      var dtolst = (IList) (Activator.CreateInstance(listtype) ?? throw new Exception());
      foreach (var o in (IList)origval) {
        dtolst.Add(GetDtoVal(o));
      }
      return dtolst;
    } 
    return origval;
  }

  private static readonly Dictionary<Type, Type?> dto_cache = new();
  public static Type? GetDtoTypeFromTypeHierarchy(Type baset) {
    if (dto_cache.TryGetValue(baset, out var dtot)) return dtot;
    var tmp = baset; 
    while(tmp is not null) {
      var t = tmp.Assembly.GetType(tmp.FullName + "+Dto");
      if (t is not null) return dto_cache[baset] = t;
      tmp = tmp.BaseType;
    }
    return dto_cache[baset] = null;
  }
}