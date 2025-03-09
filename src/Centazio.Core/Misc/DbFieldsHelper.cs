using System.ComponentModel.DataAnnotations;

namespace Centazio.Core.Misc;

public record DbFieldType(string Name, Type FieldType, string Length, bool Required);

public interface IDbFieldsHelper {
  List<DbFieldType> GetDbFields<T>();
  List<DbFieldType> GetDbFields(Type t);
  string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null);
  string GenerateIndexScript(string schema, string table, params string[] columns);
  string GenerateDropTableScript(string schema, string table);
  string TableName(string schema, string table);

}

public abstract class AbstractDbFieldsHelper : IDbFieldsHelper {
  
  private const int DEFAULT_MAX_STR_LENGTH = 128;

  public List<DbFieldType> GetDbFields<T>() => GetDbFields(typeof(T));
  public List<DbFieldType> GetDbFields(Type t) => t.GetProperties()
      .Where(p => !ReflectionUtils.IsJsonIgnore(t, p.Name))
      .Select(p => GetDbField(t, p)).ToList();

  private static DbFieldType GetDbField(Type t, PropertyInfo p) {
    int? GetMaxLen(string prop) => 
        ((MaxLengthAttribute?)p.GetCustomAttribute(typeof(MaxLengthAttribute)))?.Length 
        ?? ReflectionUtils.GetPropAttribute<MaxLength2Attribute>(t, prop)?.Length 
        ?? (p.PropertyType.IsEnum ? DEFAULT_MAX_STR_LENGTH : null);

    var pt = p.PropertyType;
    var realpt = Nullable.GetUnderlyingType(pt) ?? pt;
    var maxlen = GetMaxLen(p.Name);
    var isstring = realpt.IsEnum || maxlen is not null ||
        typeof(ValidString).IsAssignableFrom(realpt) ||
        realpt == typeof(string);
    if (isstring) {
      if (maxlen is null) throw new Exception($"field[{t.Name}].[{p.Name}] does not have a [MaxLength] attribute");
      return new DbFieldType(p.Name, typeof(string), maxlen == Int32.MaxValue ? "max" : maxlen.ToString() ?? String.Empty, !ReflectionUtils.IsNullable(p));
    }

    if (realpt == typeof(int)) return new DbFieldType(p.Name, realpt, String.Empty, !ReflectionUtils.IsNullable(p));
    if (realpt == typeof(decimal)) return new DbFieldType(p.Name, realpt, "(14,2)", !ReflectionUtils.IsNullable(p));
    if (realpt == typeof(DateTime)) return new DbFieldType(p.Name, realpt, String.Empty, !ReflectionUtils.IsNullable(p));
    if (realpt == typeof(DateOnly)) return new DbFieldType(p.Name, realpt, String.Empty, !ReflectionUtils.IsNullable(p));
    if (realpt == typeof(Boolean)) return new DbFieldType(p.Name, realpt, String.Empty, !ReflectionUtils.IsNullable(p));
    if (realpt == typeof(Guid)) return new DbFieldType(p.Name, realpt, String.Empty, !ReflectionUtils.IsNullable(p));

    throw new NotSupportedException($"Property[{p.Name}] Defined Type[{pt.Name}] Real Type[{realpt.Name}]");
  }
  
  
  public abstract string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null);
  public abstract string GenerateIndexScript(string schema, string table, params string[] columns);
  public abstract string GenerateDropTableScript(string schema, string table);
  public abstract string TableName(string schema, string table);

}