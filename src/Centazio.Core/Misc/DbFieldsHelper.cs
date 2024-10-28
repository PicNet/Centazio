using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Centazio.Core.Misc;

public record DbFieldType(string name, Type type, string length, bool required);

public interface IDbFieldsHelper {
  List<DbFieldType> GetDbFields<T>(bool failOnMissingLength=true);
  List<DbFieldType> GetDbFields(Type t, bool failOnMissingLength=true);
  string GenerateCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null);
  string GenerateIndexScript(string schema, string table, params string[] columns);
  string GenerateDropTableScript(string schema, string table);
  string TableName(string schema, string table);

}

public abstract class AbstractDbFieldsHelper : IDbFieldsHelper {
  
  private const int DEFAULT_MAX_STR_LENGTH = 128;

  public List<DbFieldType> GetDbFields<T>(bool failOnMissingLength=true) => GetDbFields(typeof(T), failOnMissingLength);
  public List<DbFieldType> GetDbFields(Type t, bool failOnMissingLength=true) => t.GetProperties()
      .Where(p => !ReflectionUtils.IsJsonIgnore(t, p.Name))
      .Select(p => GetDbField(t, p, failOnMissingLength)).ToList();

  private static DbFieldType GetDbField(Type t, PropertyInfo p, bool failOnMissingLength) {
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
      if (maxlen is null && failOnMissingLength) throw new Exception($"field[{t.Name}].[{p.Name}] does not have a [MaxLength] attribute");

      var len = maxlen ?? DEFAULT_MAX_STR_LENGTH;
      return new DbFieldType(p.Name, typeof(string), len == Int32.MaxValue ? "max" : len.ToString(), !ReflectionUtils.IsNullable(p));
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