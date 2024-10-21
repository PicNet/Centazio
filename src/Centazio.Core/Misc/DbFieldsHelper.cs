using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Centazio.Core.Misc;

public record DbFieldType(string name, Type type, string length, bool required);

public class DbFieldsHelper {
  
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

  public string GetSqlServerCreateTableScript(string schema, string table, List<DbFieldType> fields, string[] pkfields, string? additional=null) {
    var sql = new StringBuilder();
    var additionaltxt = String.IsNullOrWhiteSpace(additional) ? String.Empty : ",\n    " + additional;
    if (!String.IsNullOrWhiteSpace(schema)) {
      sql.AppendLine($@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = N'{schema}')
  EXEC('CREATE SCHEMA [{schema}] AUTHORIZATION [dbo]');");
    }
    sql.AppendLine($@"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='{table}' AND xtype='U')
BEGIN
  CREATE TABLE [{schema}].[{table}] (
    {String.Join(",\n    ", fields.Select(ToSqlSrv))},
    PRIMARY KEY ({String.Join(", ", pkfields)}){additionaltxt}
  )
END
");
    return sql.ToString().Trim();
    
    string ToSqlSrv(DbFieldType f) {
      var typestr = 
          f.type == typeof(int) ? "int" : 
          f.type == typeof(decimal) ? "decimal" : 
          f.type == typeof(DateTime) ? "datetime2" : 
          f.type == typeof(DateOnly) ? "date" : 
          f.type == typeof(Boolean) ? "bit" : 
          f.type == typeof(Guid) ? "uniqueidentifier" : 
          f.type == typeof(string) ? "nvarchar" : 
          throw new NotImplementedException(f.type.Name);
      if (!String.IsNullOrWhiteSpace(f.length)) typestr += $"({f.length})";
      var nullstr = f.required ? "not null" : "null";
      return $"[{f.name}] {typestr} {nullstr}";
    }
  }
  
  public string GetSqliteCreateTableScript(string table, List<DbFieldType> fields, string[] pkfields, string? additional = null) {
    var additionaltxt = String.IsNullOrWhiteSpace(additional) ? String.Empty : ",\n  " + additional;
    return $@"CREATE TABLE IF NOT EXISTS [{table}] (
  {String.Join(",\n    ", fields.Select(ToSqlite))},
  PRIMARY KEY ({String.Join(", ", pkfields)}){additionaltxt})";
    
    string ToSqlite(DbFieldType f) {
      var typestr = 
          f.type == typeof(int) ? "int" : 
          f.type == typeof(decimal) ? "decimal" : 
          f.type == typeof(DateTime) ? "datetime" : 
          f.type == typeof(DateOnly) ? "date" : 
          f.type == typeof(Boolean) ? "bit" : 
          f.type == typeof(Guid) ? "uniqueidentifier" : 
          f.type == typeof(string) && f.length == "max" ? "text" :
          f.type == typeof(string) ? "nvarchar" :
          throw new NotImplementedException(f.type.Name);
      if (!String.IsNullOrWhiteSpace(f.length) && typestr != "text") typestr += $"({f.length})";
      var nullstr = f.required ? "not null" : "null";
      return $"[{f.name}] {typestr} {nullstr}".Trim();
    }
  }
}
