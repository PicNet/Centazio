using System.Reflection;
using Centazio.Core.Misc;

namespace Centazio.Core;

public interface IDto<out T> { T ToBase(); }
public interface ICoreEntityDto<out T> : IDto<T> { string? CoreId { get; init; } }

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
    pairs.ForEach(p => p.DtoPi.SetValue(dto, GetDtoVal(baseobj, p)));
    return dto;
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
  
  private static object? GetDtoVal(object baseobj, PropPair p) {
    var origval = p.BasePi.GetValue(baseobj);
    if (origval is null) return origval;
    if (p.BasePi.PropertyType.IsEnum) return p.BasePi.GetValue(baseobj)?.ToString();
    if (p.BasePi.PropertyType.IsAssignableTo(typeof(ValidString))) return ((ValidString)origval).Value;   
    return Convert.ChangeType(origval, Nullable.GetUnderlyingType(p.DtoPi.PropertyType) ?? p.DtoPi.PropertyType) ?? throw new Exception();
  }

  public static Type? GetDtoTypeFromTypeHierarchy(Type? baset) {
    while(baset is not null) {
      var t = baset.Assembly.GetType(baset.FullName + "+Dto");
      if (t is not null) return t;
      baset = baset.BaseType;
    }
    return null;
  }
}